using System.Net;
using System.Diagnostics.Metrics;
using System.Security.Claims;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using PortalSubastas.Gateway.Security;

namespace PortalSubastas.Gateway.Config;

public static class GatewaySecurityConfig
{
    public const string CorrelationHeaderName = "X-Correlation-ID";
    private static readonly Meter SecurityMeter = new("PortalSubastas.Security");
    private static readonly Counter<long> RateLimitRejectedCounter = SecurityMeter.CreateCounter<long>("security.rate_limit.rejected");
    private static readonly Counter<long> KillSwitchRejectedCounter = SecurityMeter.CreateCounter<long>("security.kill_switch.rejected");

    public static IServiceCollection AddGatewaySecurity(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        ValidateProductionConfiguration(configuration, environment);

        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;

            var knownProxies = configuration.GetSection("Security:TrustedProxies:KnownProxies").Get<string[]>()
                               ?? Array.Empty<string>();

            if (knownProxies.Length > 0)
            {
                options.KnownProxies.Clear();
                foreach (var proxy in knownProxies)
                {
                    if (IPAddress.TryParse(proxy, out var ipAddress))
                    {
                        options.KnownProxies.Add(ipAddress);
                    }
                }
            }

            if (environment.IsDevelopment() && knownProxies.Length == 0)
            {
                options.KnownNetworks.Clear();
                options.KnownProxies.Clear();
            }
        });

        services.AddHsts(options =>
        {
            options.MaxAge = TimeSpan.FromDays(configuration.GetValue("Security:Headers:HstsMaxAgeDays", 180));
            options.IncludeSubDomains = configuration.GetValue("Security:Headers:HstsIncludeSubDomains", true);
            options.Preload = configuration.GetValue("Security:Headers:HstsPreload", false);
        });

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.OnRejected = async (context, cancellationToken) =>
            {
                var httpContext = context.HttpContext;
                var retryAfter = GetRetryAfterSeconds(context.Lease);
                var correlationId = EnsureCorrelationId(httpContext);
                var partitionKey = GetPartitionKey(httpContext, "UserIpAndPath");
                RateLimitRejectedCounter.Add(1,
                    new KeyValuePair<string, object?>("method", httpContext.Request.Method),
                    new KeyValuePair<string, object?>("path", NormalizeMetricPath(httpContext.Request.Path)));

                if (retryAfter > 0)
                {
                    httpContext.Response.Headers.RetryAfter = retryAfter.ToString();
                }

                httpContext.Response.Headers[CorrelationHeaderName] = correlationId;
                httpContext.Response.ContentType = "application/json";

                var logger = httpContext.RequestServices
                    .GetRequiredService<ILoggerFactory>()
                    .CreateLogger("Security.RateLimiting");

                logger.LogWarning(
                    "Rate limit rejected {Method} {Path} for {Partition} with correlation {CorrelationId}",
                    httpContext.Request.Method,
                    httpContext.Request.Path.Value,
                    partitionKey,
                    correlationId);

                var auditPublisher = httpContext.RequestServices.GetService<IRateLimitAuditPublisher>();
                if (auditPublisher is not null)
                {
                    await auditPublisher.PublishRejectedAsync(httpContext, correlationId, retryAfter, partitionKey, cancellationToken);
                }

                await httpContext.Response.WriteAsJsonAsync(new
                {
                    success = false,
                    code = StatusCodes.Status429TooManyRequests,
                    message = "Demasiadas solicitudes. Intentá nuevamente en unos segundos.",
                    correlationId
                }, cancellationToken);
            };

            options.GlobalLimiter = PartitionedRateLimiter.Create(CreatePolicy(configuration, "Global", 600, TimeSpan.FromMinutes(1), 0, "UserOrIp"));
            options.AddPolicy("auth-login", CreatePolicy(configuration, "AuthLogin", 8, TimeSpan.FromMinutes(1), 0, "IpAndPath"));
            options.AddPolicy("auth-register", CreatePolicy(configuration, "AuthRegister", 5, TimeSpan.FromMinutes(5), 0, "IpAndPath"));
            options.AddPolicy("auth-reset", CreatePolicy(configuration, "AuthReset", 5, TimeSpan.FromMinutes(5), 0, "IpAndPath"));
            options.AddPolicy("auth-strict", CreatePolicy(configuration, "AuthStrict", 10, TimeSpan.FromMinutes(1), 0, "IpAndPath"));
            options.AddPolicy("public-read", CreatePolicy(configuration, "PublicRead", 240, TimeSpan.FromMinutes(1), 0, "IpAndPath"));
            options.AddPolicy("read-api", CreatePolicy(configuration, "ReadApi", 240, TimeSpan.FromMinutes(1), 0, "UserOrIp"));
            options.AddPolicy("reports-heavy", CreatePolicy(configuration, "ReportsHeavy", 6, TimeSpan.FromMinutes(5), 0, "UserIpAndPath"));
            options.AddPolicy("uploads", CreatePolicy(configuration, "Uploads", 12, TimeSpan.FromMinutes(10), 0, "UserIpAndPath"));
            options.AddPolicy("offers", CreatePolicy(configuration, "Offers", 60, TimeSpan.FromMinutes(1), 0, "UserIpAndPath"));
            options.AddPolicy("write-api", CreatePolicy(configuration, "WriteApi", 90, TimeSpan.FromMinutes(1), 0, "UserOrIp"));
            options.AddPolicy("admin-api", CreatePolicy(configuration, "AdminApi", 90, TimeSpan.FromMinutes(1), 0, "UserOrIp"));
            options.AddPolicy("security-admin", CreatePolicy(configuration, "SecurityAdmin", 60, TimeSpan.FromMinutes(1), 0, "UserIpAndPath"));
            options.AddPolicy("signalr", CreatePolicy(configuration, "SignalR", 30, TimeSpan.FromMinutes(1), 0, "UserIpAndPath"));
            options.AddPolicy("fallback-api", CreatePolicy(configuration, "FallbackApi", 120, TimeSpan.FromMinutes(1), 0, "UserOrIp"));
        });

        return services;
    }

    public static IApplicationBuilder UseGatewaySecurity(this IApplicationBuilder app, IHostEnvironment environment)
    {
        app.UseForwardedHeaders();

        if (!environment.IsDevelopment())
        {
            app.UseHsts();
        }

        app.Use(async (context, next) =>
        {
            var correlationId = EnsureCorrelationId(context);
            context.Response.Headers[CorrelationHeaderName] = correlationId;
            context.Response.Headers["X-Content-Type-Options"] = "nosniff";
            context.Response.Headers["Referrer-Policy"] = "no-referrer";
            context.Response.Headers["X-Frame-Options"] = "DENY";
            context.Response.Headers["Content-Security-Policy"] = "frame-ancestors 'none'";
            context.Response.Headers["Permissions-Policy"] = "geolocation=(), microphone=(), camera=()";

            var configuration = context.RequestServices.GetRequiredService<IConfiguration>();
            var killSwitchMessage = GetKillSwitchMessage(context, configuration);
            if (killSwitchMessage is not null)
            {
                KillSwitchRejectedCounter.Add(1,
                    new KeyValuePair<string, object?>("method", context.Request.Method),
                    new KeyValuePair<string, object?>("path", NormalizeMetricPath(context.Request.Path)));

                context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsJsonAsync(new
                {
                    success = false,
                    code = StatusCodes.Status503ServiceUnavailable,
                    message = killSwitchMessage,
                    correlationId
                });
                return;
            }

            await next();
        });

        app.UseRateLimiter();
        return app;
    }

    private static Func<HttpContext, RateLimitPartition<string>> CreatePolicy(
        IConfiguration configuration,
        string policyName,
        int defaultPermitLimit,
        TimeSpan defaultWindow,
        int defaultQueueLimit,
        string defaultPartitionStrategy)
    {
        var section = configuration.GetSection($"Security:RateLimiting:{policyName}");
        var permitLimit = section.GetValue("PermitLimit", defaultPermitLimit);
        var windowSeconds = section.GetValue("WindowSeconds", (int)defaultWindow.TotalSeconds);
        var queueLimit = section.GetValue("QueueLimit", defaultQueueLimit);
        var partitionStrategy = section.GetValue("PartitionStrategy", defaultPartitionStrategy);

        return httpContext => RateLimitPartition.GetSlidingWindowLimiter(
            $"{policyName}:{GetPartitionKey(httpContext, partitionStrategy)}",
            _ => new SlidingWindowRateLimiterOptions
            {
                PermitLimit = permitLimit,
                Window = TimeSpan.FromSeconds(windowSeconds),
                SegmentsPerWindow = Math.Clamp(section.GetValue("SegmentsPerWindow", 6), 1, 12),
                QueueLimit = queueLimit,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                AutoReplenishment = true
            });
    }

    private static string GetPartitionKey(HttpContext context, string? strategy = null)
    {
        var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier)
                     ?? context.User.FindFirstValue("sub")
                     ?? context.User.Identity?.Name;
        var ip = GetClientIp(context);
        var userOrIp = !string.IsNullOrWhiteSpace(userId)
            ? $"user:{userId}"
            : $"ip:{ip}";
        var rawPath = context.Request.Path.Value ?? "/";
        var normalizedPath = NormalizeMetricPath(context.Request.Path);
        var method = context.Request.Method.ToUpperInvariant();

        return (strategy ?? "UserOrIp").Trim().ToLowerInvariant() switch
        {
            "ip" => $"ip:{ip}",
            "ipandpath" => $"ip:{ip}:method:{method}:path:{normalizedPath}",
            "userandip" => !string.IsNullOrWhiteSpace(userId) ? $"user:{userId}:ip:{ip}" : $"ip:{ip}",
            "userandpath" => $"{userOrIp}:method:{method}:path:{normalizedPath}",
            "useripandpath" => !string.IsNullOrWhiteSpace(userId)
                ? $"user:{userId}:ip:{ip}:method:{method}:path:{rawPath}"
                : $"ip:{ip}:method:{method}:path:{rawPath}",
            _ => userOrIp
        };
    }

    private static string NormalizeMetricPath(PathString path)
    {
        var value = path.Value;
        if (string.IsNullOrWhiteSpace(value) || value == "/")
        {
            return "/";
        }

        var segments = value
            .Split('/', StringSplitOptions.RemoveEmptyEntries)
            .Select(segment => int.TryParse(segment, out _) || Guid.TryParse(segment, out _) ? "{id}" : segment);

        return "/" + string.Join("/", segments);
    }

    private static string GetClientIp(HttpContext context)
    {
        var cfConnectingIp = context.Request.Headers["CF-Connecting-IP"].FirstOrDefault();
        if (IPAddress.TryParse(cfConnectingIp, out _))
        {
            return cfConnectingIp!;
        }

        return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }

    private static string EnsureCorrelationId(HttpContext context)
    {
        var correlationId = context.Request.Headers[CorrelationHeaderName].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(correlationId))
        {
            correlationId = context.TraceIdentifier;
            context.Request.Headers[CorrelationHeaderName] = correlationId;
        }

        return correlationId;
    }

    private static int GetRetryAfterSeconds(RateLimitLease lease)
    {
        return lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter)
            ? Math.Max(1, (int)Math.Ceiling(retryAfter.TotalSeconds))
            : 60;
    }

    private static string? GetKillSwitchMessage(HttpContext context, IConfiguration configuration)
    {
        var path = context.Request.Path.Value ?? string.Empty;
        var method = context.Request.Method;

        if (configuration.GetValue("Security:KillSwitches:DisablePublicRegistration", false) &&
            HttpMethods.IsPost(method) &&
            path.Equals("/api/Auth/register", StringComparison.OrdinalIgnoreCase))
        {
            return "El registro público está temporalmente deshabilitado.";
        }

        if (configuration.GetValue("Security:KillSwitches:DisablePdfReports", false) &&
            path.StartsWith("/api/Reporte", StringComparison.OrdinalIgnoreCase) &&
            path.Contains("/pdf", StringComparison.OrdinalIgnoreCase))
        {
            return "La generación de reportes PDF está temporalmente deshabilitada.";
        }

        if (configuration.GetValue("Security:KillSwitches:AuctionEmergencyMode", false) &&
            !IsAuctionEmergencyAllowed(path, method))
        {
            return "El sistema está operando en modo emergencia de subasta. Sólo se permiten ofertas y consultas esenciales.";
        }

        if (configuration.GetValue("Security:KillSwitches:ReadOnlyMode", false) &&
            !IsSafeMethod(method) &&
            !IsReadOnlyAllowedWrite(path, method))
        {
            return "El sistema está temporalmente en modo sólo lectura.";
        }

        return null;
    }

    private static bool IsAuctionEmergencyAllowed(string path, string method)
    {
        if (IsSafeMethod(method))
        {
            return path.StartsWith("/api/CotizacionPublica", StringComparison.OrdinalIgnoreCase) ||
                   path.StartsWith("/api/Cotizacion", StringComparison.OrdinalIgnoreCase) ||
                   path.StartsWith("/api/OfertaSubasta", StringComparison.OrdinalIgnoreCase) ||
                   path.StartsWith("/signalr/subastas", StringComparison.OrdinalIgnoreCase);
        }

        return path.StartsWith("/api/OfertaSubasta", StringComparison.OrdinalIgnoreCase) ||
               path.Equals("/api/Auth/login", StringComparison.OrdinalIgnoreCase) ||
               path.Equals("/api/Auth/switch-context", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsReadOnlyAllowedWrite(string path, string method)
    {
        return HttpMethods.IsPost(method) &&
               (path.Equals("/api/Auth/login", StringComparison.OrdinalIgnoreCase) ||
                path.Equals("/api/Auth/switch-context", StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsSafeMethod(string method)
    {
        return HttpMethods.IsGet(method) ||
               HttpMethods.IsHead(method) ||
               HttpMethods.IsOptions(method);
    }

    private static void ValidateProductionConfiguration(IConfiguration configuration, IHostEnvironment environment)
    {
        if (environment.IsDevelopment())
        {
            return;
        }

        var allowedOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>();
        if (allowedOrigins is null || allowedOrigins.Length == 0)
        {
            throw new InvalidOperationException("Security misconfiguration: Cors:AllowedOrigins is required outside Development.");
        }

        var trustedProxies = configuration.GetSection("Security:TrustedProxies:KnownProxies").Get<string[]>();
        if (trustedProxies is null || trustedProxies.Length == 0)
        {
            throw new InvalidOperationException("Security misconfiguration: Security:TrustedProxies:KnownProxies is required outside Development.");
        }
    }
}
