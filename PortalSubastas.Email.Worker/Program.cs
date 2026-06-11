using MassTransit;
using PortalSubastas.Email.Worker;
using PortalSubastas.Email.Worker.Consumers;
using PortalSubastas.Email.Worker.Services;
using Resend;

var builder = Host.CreateApplicationBuilder(args);

// Resend
builder.Services.AddResend(options =>
{
    options.ApiToken = builder.Configuration["Resend:ApiKey"]
        ?? throw new InvalidOperationException("Resend:ApiKey is not configured. The worker cannot start without an API key.");
});

// Email service
builder.Services.AddScoped<IEmailService, EmailService>();

// MassTransit with RabbitMQ
builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<ProveedorInvitadoConsumer>();
    x.AddConsumer<SubastaPublicadaConsumer>();

    x.UsingRabbitMq((context, cfg) =>
    {
        var rabbitConfig = builder.Configuration.GetSection("RabbitMq");

        cfg.Host(rabbitConfig["Host"], "/", h =>
        {
            h.Username(rabbitConfig["Username"]);
            h.Password(rabbitConfig["Password"]);
        });

        cfg.ReceiveEndpoint("email-proveedor-invitado-queue", e =>
        {
            e.ConfigureConsumer<ProveedorInvitadoConsumer>(context);
        });

        cfg.ReceiveEndpoint("email-subasta-publicada-queue", e =>
        {
            e.ConfigureConsumer<SubastaPublicadaConsumer>(context);
        });
    });
});

builder.Services.AddHostedService<Worker>();

var host = builder.Build();

// Validate Resend config at startup
var config = host.Services.GetRequiredService<IConfiguration>();
var apiKey = config["Resend:ApiKey"];
var fromEmail = config["Resend:From"];
if (string.IsNullOrWhiteSpace(apiKey))
{
    var logger = host.Services.GetRequiredService<ILogger<Program>>();
    logger.LogCritical("❌ Resend:ApiKey is not configured. Worker cannot start.");
    throw new InvalidOperationException("Resend:ApiKey is required.");
}
if (string.IsNullOrWhiteSpace(fromEmail))
{
    var logger = host.Services.GetRequiredService<ILogger<Program>>();
    logger.LogCritical("❌ Resend:From is not configured. Worker cannot start.");
    throw new InvalidOperationException("Resend:From is required.");
}

host.Run();
