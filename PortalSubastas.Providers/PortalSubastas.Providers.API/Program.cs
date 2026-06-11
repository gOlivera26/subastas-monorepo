using PortalSubastas.Providers.API.Config;
using PortalSubastas.Providers.API.Middlewares;

TimeZoneInfo.ClearCachedData();
Environment.SetEnvironmentVariable("TZ", "America/Argentina/Buenos_Aires");

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddConfig(builder.Configuration);

builder.Services.AddOpenTelemetryTracing(builder.Configuration);

var app = builder.Build();

app.UseMiddleware<GlobalExceptionHandlingMiddleware>();

app.UseOpenTelemetry();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapHealthChecks("/health").AllowAnonymous();

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();

public partial class Program { }