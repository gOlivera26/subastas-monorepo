using MassTransit;
using PortalSubastas.Audit.Worker;
using PortalSubastas.Audit.Worker.Consumers;
using PortalSubastas.Audit.Worker.Config;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddOpenTelemetryTracing(builder.Configuration);

builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<SystemLogEventConsumer>();
    x.AddConsumer<DataChangedEventConsumer>();

    x.UsingRabbitMq((context, cfg) =>
    {
        var rabbitConfig = builder.Configuration.GetSection("RabbitMq");

        cfg.Host(rabbitConfig["Host"], "/", h =>
        {
            h.Username(rabbitConfig["Username"]);
            h.Password(rabbitConfig["Password"]);
        });

        cfg.ReceiveEndpoint("audit-system-logs-queue", e =>
        {
            e.ConfigureConsumer<SystemLogEventConsumer>(context);
        });

        cfg.ReceiveEndpoint("audit-data-changes-queue", e =>
        {
            e.ConfigureConsumer<DataChangedEventConsumer>(context);
        });
    });
});

builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();