using PortalSubastas.Gateway.Config;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenTelemetryTracing(builder.Configuration);

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

app.UseOpenTelemetry();

app.UseCors("AllowFrontendApp");

app.MapReverseProxy();

app.Run();