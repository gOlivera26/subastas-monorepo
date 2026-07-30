using PortalSubastas.Providers.API.Config;
using PortalSubastas.Providers.API.Middlewares;

TimeZoneInfo.ClearCachedData();
Environment.SetEnvironmentVariable("TZ", "America/Argentina/Buenos_Aires");

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddConfig(builder.Configuration);

builder.Services.AddOpenTelemetryTracing(builder.Configuration);

var app = builder.Build();

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<GlobalExceptionHandlingMiddleware>();

app.UseOpenTelemetry();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapHealthChecks("/health").AllowAnonymous();
app.MapGet("/health/ready", async (ProvidersContext db, IConfiguration configuration, CancellationToken cancellationToken) =>
{
    var dbOk = await db.Database.CanConnectAsync(cancellationToken);
    var rabbitOk = !string.IsNullOrWhiteSpace(configuration["RabbitMq:Host"]) &&
                   !string.IsNullOrWhiteSpace(configuration["RabbitMq:Username"]) &&
                   !string.IsNullOrWhiteSpace(configuration["RabbitMq:Password"]);
    var storageOk = !string.IsNullOrWhiteSpace(configuration["CloudflareR2:BucketName"]);

    var payload = new
    {
        status = dbOk && rabbitOk && storageOk ? "Healthy" : "Degraded",
        checks = new
        {
            database = dbOk ? "Healthy" : "Unhealthy",
            rabbitMq = rabbitOk ? "Configured" : "Missing configuration",
            storage = storageOk ? "Configured" : "Missing configuration"
        },
        timestamp = DateTimeOffset.UtcNow
    };

    return Results.Json(payload, statusCode: dbOk && rabbitOk && storageOk ? StatusCodes.Status200OK : StatusCodes.Status503ServiceUnavailable);
}).AllowAnonymous();

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();

public partial class Program { }
