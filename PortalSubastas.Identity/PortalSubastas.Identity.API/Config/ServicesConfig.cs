using FluentValidation;
using FluentValidation.AspNetCore;
using PortalSubastas.Identity.Application.RequestDto.Login;
using Resend;

namespace PortalSubastas.Identity.API.Config;

public static class ServicesConfig
{
    public static void AddConfig(this IServiceCollection services, IConfiguration configuration)
    {
        ValidateProductionSecurityConfiguration(configuration);

        services.AddHttpContextAccessor();
        services.AddMemoryCache();

        var allowedOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>();
        var isDevelopment = IsDevelopment();

        services.AddCors(options =>
        {
            options.AddDefaultPolicy(builder =>
            {
                if (allowedOrigins != null && allowedOrigins.Any())
                {
                    builder.WithOrigins(allowedOrigins)
                           .AllowAnyMethod()
                           .AllowAnyHeader()
                           .AllowCredentials();
                }
                else if (isDevelopment)
                {
                    builder.SetIsOriginAllowed(_ => true)
                           .AllowAnyMethod()
                           .AllowAnyHeader()
                           .AllowCredentials();
                }
                else
                {
                    builder.SetIsOriginAllowed(_ => false);
                }
            });
        });

        services.AddSwagger();
        services.AddJwt(configuration);

        services.BindAppSettings(configuration);
        services.AddAutoMapper(cfg =>
        {
            cfg.AddMaps(typeof(PortalSubastas.Identity.Application.AutoMapper.UserProfile).Assembly);
        });

        services.AddScoped<PortalSubastas.Identity.Domain.Interceptors.AuditInterceptor>();

        services.AddDbContext<PortalSubastasContext>((sp, options) =>
        {
            var interceptor = sp.GetRequiredService<PortalSubastas.Identity.Domain.Interceptors.AuditInterceptor>();
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection"), o => o.UseVector())
                   .AddInterceptors(interceptor); //intercepto solicitudes para auditar
        });

        services.AddInternalServices();

        services.AddResend(options =>
            options.ApiToken = configuration["Resend:ApiKey"]!);

        services.AddControllers();

        services.AddAuthorizationBuilder()
            .SetFallbackPolicy(new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build());
    }

    private static void AddJwt(this IServiceCollection services, IConfiguration configuration)
    {
        var secretKey = GetJwtSecret(
            configuration,
            "YourSecretKeyHere12345",
            "DevelopmentOnlyJwtSecretChangeMe_32Chars_Minimum!");

        _ = services.AddAuthentication(x =>
        {
            x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        }).AddJwtBearer(options =>
        {
            options.RequireHttpsMetadata = !IsDevelopment();
            options.SaveToken = true;
            options.TokenValidationParameters = new TokenValidationParameters()
            {
                RequireExpirationTime = true,
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = configuration["Jwt:Issuer"] ?? "PortalSubastas.Identity",
                ValidAudience = configuration["Jwt:Audience"] ?? "PortalSubastas.Identity",
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(secretKey)),
                ClockSkew = TimeSpan.Zero,
            };

            options.Events = new JwtBearerEvents()
            {
                OnChallenge = context =>
                {
                    context.HandleResponse();
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    context.Response.ContentType = "application/json";

                    return context.Response.WriteAsync(JsonSerializer.Serialize(
                        OperationResponse<object>.CreateBuilder().WithCode(401)
                            .WithMessage("No estás autenticado.").Build()));
                },
                OnForbidden = context =>
                {
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    context.Response.ContentType = "application/json";
                    return context.Response.WriteAsync(JsonSerializer.Serialize(
                        OperationResponse<object>.CreateBuilder().WithCode(403)
                            .WithMessage("No tienes permisos para realizar esta acción.").Build()));
                }
            };
        }); 
    }

    private static void AddSwagger(this IServiceCollection services)
    {
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "PortalSubastas.Identity API",
                Version = "v1"
            });

            c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Description = "Ingrese el token de autenticación en el siguiente formato: Bearer {token}",
                Name = "Authorization",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.ApiKey,
                BearerFormat = "JWT",
                Scheme = "Bearer"
            });

            c.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    Array.Empty<string>()
                }
            });
        });
    }

    private static void AddInternalServices(this IServiceCollection services)
    {
        services.AddScoped<IEmailService, EmailService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IOrganizationService, OrganizationService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IRoleService, RoleService>();
        services.AddScoped<IVigenciaService, VigenciaService>();
        services.AddScoped<IUnidadAdministrativaService, UnidadAdministrativaService>();
        services.AddScoped<IObjetoGastoService, ObjetoGastoService>();
        services.AddScoped<ICatalogoBienService, CatalogoBienService>();
        services.AddScoped<ICategoriaProgramaticaService, CategoriaProgramaticaService>();
        services.AddScoped<IMonedaService, MonedaService>();
        services.AddScoped<ISubResponsableService, SubResponsableService>();

        services.AddFluentValidationAutoValidation();
        services.AddValidatorsFromAssembly(typeof(LoginRequestDto).Assembly);
    }

    private static void BindAppSettings(this IServiceCollection services, IConfiguration configuration)
    {
    }

    private static void ValidateProductionSecurityConfiguration(IConfiguration configuration)
    {
        if (IsDevelopment()) return;

        var allowedOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>();
        if (allowedOrigins is null || allowedOrigins.Length == 0)
        {
            throw new InvalidOperationException("Security misconfiguration: Cors:AllowedOrigins is required outside Development.");
        }

        if (string.IsNullOrWhiteSpace(configuration.GetConnectionString("DefaultConnection")))
        {
            throw new InvalidOperationException("Security misconfiguration: ConnectionStrings:DefaultConnection is required outside Development.");
        }
    }

    private static string GetJwtSecret(IConfiguration configuration, params string[] weakDefaults)
    {
        var secret = configuration["Jwt:SecretKey"];

        if (!IsDevelopment())
        {
            if (string.IsNullOrWhiteSpace(secret) ||
                secret.Length < 32 ||
                weakDefaults.Contains(secret, StringComparer.Ordinal))
            {
                throw new InvalidOperationException("Security misconfiguration: Jwt:SecretKey must be strong and environment-provided outside Development.");
            }
        }

        return secret ?? weakDefaults.First();
    }

    private static bool IsDevelopment()
    {
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
                          ?? Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT");

        return string.Equals(environment, "Development", StringComparison.OrdinalIgnoreCase);
    }
}
