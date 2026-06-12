using MassTransit;
using PortalSubastas.Contracts.Events;
using PortalSubastas.Email.Worker.Services;

namespace PortalSubastas.Email.Worker.Consumers;

public class GanadorRegistradoConsumer : IConsumer<GanadorRegistradoEvent>
{
    private readonly IEmailService _emailService;
    private readonly ILogger<GanadorRegistradoConsumer> _logger;

    public GanadorRegistradoConsumer(IEmailService emailService, ILogger<GanadorRegistradoConsumer> logger)
    {
        _emailService = emailService;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<GanadorRegistradoEvent> context)
    {
        var msg = context.Message;

        if (string.IsNullOrWhiteSpace(msg.EmailProveedor))
        {
            _logger.LogWarning("⚠️ GanadorRegistradoEvent ignorado: el proveedor ganador {IdProveedor} no tiene email.", msg.IdProveedor);
            return;
        }

        try
        {
            var subject = $"Has resultado ganador en la subasta #{msg.NroCotizacion}";
            var body = BuildGanadorHtml(msg);

            await _emailService.SendEmailAsync(msg.EmailProveedor, subject, body);

            _logger.LogInformation("✅ Email de ganador enviado a {Email} para subasta #{Nro}",
                msg.EmailProveedor, msg.NroCotizacion);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error al enviar email de ganador a {Email} para subasta #{Nro}",
                msg.EmailProveedor, msg.NroCotizacion);
            throw;
        }
    }

    private static string BuildGanadorHtml(GanadorRegistradoEvent msg)
    {
        return $"""
        <div style="font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;">
          <h2 style="color: #2563eb;">Portal de Subastas</h2>
          <p>Hola <strong>{msg.NombreProveedor}</strong>,</p>
          <p>¡Felicitaciones! Has resultado <strong>ganador</strong> en la subasta <strong>#{msg.NroCotizacion}</strong>.</p>
          <table style="width: 100%; border-collapse: collapse;">
            <tr><td style="padding: 8px; border: 1px solid #ddd;"><strong>N° Subasta</strong></td><td style="padding: 8px; border: 1px solid #ddd;">{msg.NroCotizacion}</td></tr>
            <tr><td style="padding: 8px; border: 1px solid #ddd;"><strong>Título</strong></td><td style="padding: 8px; border: 1px solid #ddd;">{msg.Titulo}</td></tr>
            <tr><td style="padding: 8px; border: 1px solid #ddd;"><strong>Tipo</strong></td><td style="padding: 8px; border: 1px solid #ddd;">{msg.TipoContratacion}</td></tr>
            <tr><td style="padding: 8px; border: 1px solid #ddd;"><strong>Monto ganador</strong></td><td style="padding: 8px; border: 1px solid #ddd;">${msg.MontoGanador:N2}</td></tr>
          </table>
          <h3 style="margin-top: 20px;">Próximos pasos</h3>
          <p>Deberás presentar la documentación requerida ante el organismo contratante dentro del plazo establecido en los pliegos. Accede al portal para ver los detalles y descargar la documentación necesaria.</p>
          <hr style="border: none; border-top: 1px solid #e5e7eb;" />
          <p style="color: #6b7280; font-size: 12px;">Portal de Subastas</p>
        </div>
        """;
    }
}
