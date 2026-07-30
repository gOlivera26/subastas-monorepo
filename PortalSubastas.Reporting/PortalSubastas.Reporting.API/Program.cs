using PortalSubastas.Reporting.API.Config;
using PortalSubastas.Reporting.API.Middlewares;

TimeZoneInfo.ClearCachedData();
Environment.SetEnvironmentVariable("TZ", "America/Argentina/Buenos_Aires");

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddConfig(builder.Configuration);

var app = builder.Build();

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<GlobalExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapHealthChecks("/health").AllowAnonymous();
app.MapGet("/health/ready", (IConfiguration configuration) =>
{
    var licitacionesBaseUrl = configuration["Services:Licitaciones:BaseUrl"];
    var puppeteerEnabled = !configuration.GetValue("Security:KillSwitches:DisablePdfReports", false);

    var ready = !string.IsNullOrWhiteSpace(licitacionesBaseUrl);
    var payload = new
    {
        status = ready ? "Healthy" : "Degraded",
        checks = new
        {
            licitaciones = ready ? "Configured" : "Missing configuration",
            pdfRenderer = puppeteerEnabled ? "Enabled" : "Disabled by kill switch"
        },
        timestamp = DateTimeOffset.UtcNow
    };

    return Results.Json(payload, statusCode: ready ? StatusCodes.Status200OK : StatusCodes.Status503ServiceUnavailable);
}).AllowAnonymous();

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();

public partial class Program { }
