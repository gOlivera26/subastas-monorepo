namespace PortalSubastas.Licitaciones.API.Config;

public static class OpenTelemetryConfig
{
    public static IServiceCollection AddOpenTelemetryTracing(this IServiceCollection services, IConfiguration configuration)
    {
        var serviceName = configuration["OTEL_SERVICE_NAME"] ?? "PortalSubastas.Licitaciones-API";
        var otlpEndpoint = configuration["OTEL_EXPORTER_OTLP_ENDPOINT"] ?? "http://localhost:4317";
        var environment = configuration["ASPNETCORE_ENVIRONMENT"] ?? "Development";

        services.AddOpenTelemetry()
            .ConfigureResource(resource => resource
                .AddService(serviceName: serviceName)
                .AddAttributes(new Dictionary<string, object>
                {
                    ["deployment.environment"] = environment,
                    ["service.name"] = serviceName
                }))
            .WithTracing(tracing => tracing
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddSource(serviceName)
                .AddOtlpExporter(options =>
                {
                    options.Endpoint = new Uri(otlpEndpoint);
                    options.Protocol = OtlpExportProtocol.Grpc;
                }))
            .WithMetrics(metrics => metrics
                .AddAspNetCoreInstrumentation()
                .AddRuntimeInstrumentation()
                .AddPrometheusExporter());

        services.AddLogging(logging => logging
            .AddOpenTelemetry(options =>
            {
                options.SetResourceBuilder(ResourceBuilder.CreateDefault()
                    .AddService(serviceName)
                    .AddAttributes(new Dictionary<string, object>
                    {
                        ["deployment.environment"] = environment,
                        ["service.name"] = serviceName
                    }));
                
                var lokiEndpoint = configuration["OTEL_EXPORTER_OTLP_LOGS_ENDPOINT"];
                if (!string.IsNullOrEmpty(lokiEndpoint))
                {
                    options.AddOtlpExporter(otlpOptions =>
                    {
                        otlpOptions.Endpoint = new Uri(lokiEndpoint);
                    });
                }
            }));

        return services;
    }

    public static WebApplication UseOpenTelemetry(this WebApplication app)
    {
        app.UseOpenTelemetryPrometheusScrapingEndpoint();

        return app;
    }
}
