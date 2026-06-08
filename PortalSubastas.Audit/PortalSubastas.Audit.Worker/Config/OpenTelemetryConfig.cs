using Npgsql;
using OpenTelemetry.Exporter;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace PortalSubastas.Audit.Worker.Config;

public static class OpenTelemetryConfig
{
    public static IServiceCollection AddOpenTelemetryTracing(this IServiceCollection services, IConfiguration configuration)
    {
        var serviceName = configuration["OTEL_SERVICE_NAME"] ?? "PortalSubastas.Audit-Worker";
        var otlpEndpoint = configuration["OTEL_EXPORTER_OTLP_ENDPOINT"] ?? "http://localhost:4317";
        var environment = configuration["DOTNET_ENVIRONMENT"] ?? "Development";

        services.AddOpenTelemetry()
            .ConfigureResource(resource => resource
                .AddService(serviceName: serviceName)
                .AddAttributes(new Dictionary<string, object>
                {
                    ["deployment.environment"] = environment,
                    ["service.name"] = serviceName
                }))
            .WithTracing(tracing => tracing
                .AddHttpClientInstrumentation()
                .AddNpgsql()
                .AddSource(serviceName)
                .AddSource("MassTransit")
                .AddOtlpExporter(options =>
                {
                    options.Endpoint = new Uri(otlpEndpoint);
                    options.Protocol = OtlpExportProtocol.Grpc;
                }))
            .WithMetrics(metrics => metrics
                .AddRuntimeInstrumentation()
                .AddMeter("MassTransit")
                .AddOtlpExporter(options =>
                {
                    options.Endpoint = new Uri(otlpEndpoint);
                    options.Protocol = OtlpExportProtocol.Grpc;
                }));

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
}