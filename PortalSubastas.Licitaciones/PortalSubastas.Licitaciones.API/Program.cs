using PortalSubastas.Licitaciones.API.Config;
using PortalSubastas.Licitaciones.API.Hubs;
using PortalSubastas.Licitaciones.API.Middlewares;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;

TimeZoneInfo.ClearCachedData();
Environment.SetEnvironmentVariable("TZ", "America/Argentina/Buenos_Aires");

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMassTransit(x =>
{
    x.UsingRabbitMq((context, cfg) =>
    {
        var rabbitConfig = builder.Configuration.GetSection("RabbitMq");
        cfg.Host(rabbitConfig["Host"], "/", h =>
        {
            h.Username(rabbitConfig["Username"]);
            h.Password(rabbitConfig["Password"]);
        });
    });
});

builder.Services.AddConfig(builder.Configuration);
builder.Services.AddOpenTelemetryTracing(builder.Configuration);

builder.Services.AddSignalR();
builder.Services.AddHealthChecks();

var app = builder.Build();

app.UseMiddleware<GlobalExceptionHandlingMiddleware>();

app.UseOpenTelemetry();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<SubastaHub>("/signalr/subastas");

app.MapHealthChecks("/health");

app.Run();

public partial class Program { }
