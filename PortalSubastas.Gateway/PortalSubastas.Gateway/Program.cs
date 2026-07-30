using MassTransit;
using PortalSubastas.Gateway.Config;
using PortalSubastas.Gateway.Security;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenTelemetryTracing(builder.Configuration);
builder.Services.AddGatewaySecurity(builder.Configuration, builder.Environment);

var rabbitConfig = builder.Configuration.GetSection("RabbitMq");
var rabbitConfigured = !string.IsNullOrWhiteSpace(rabbitConfig["Host"]) &&
                       !string.IsNullOrWhiteSpace(rabbitConfig["Username"]) &&
                       !string.IsNullOrWhiteSpace(rabbitConfig["Password"]);

if (rabbitConfigured)
{
    builder.Services.AddMassTransit(x =>
    {
        x.UsingRabbitMq((context, cfg) =>
        {
            cfg.Host(rabbitConfig["Host"], "/", h =>
            {
                h.Username(rabbitConfig["Username"]!);
                h.Password(rabbitConfig["Password"]!);
            });
        });
    });

    builder.Services.AddScoped<IRateLimitAuditPublisher, RabbitMqRateLimitAuditPublisher>();
}
else
{
    builder.Services.AddSingleton<IRateLimitAuditPublisher, NoopRateLimitAuditPublisher>();
}

builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontendApp",
        policy => policy.WithOrigins(allowedOrigins)
                        .AllowAnyMethod()
                        .AllowAnyHeader()
                        .AllowCredentials());
});

var app = builder.Build();

app.UseGatewaySecurity(app.Environment);

app.UseOpenTelemetry();

app.UseCors("AllowFrontendApp");

app.MapReverseProxy();

app.Run();
