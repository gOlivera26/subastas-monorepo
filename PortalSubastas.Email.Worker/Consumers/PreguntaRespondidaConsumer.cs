using MassTransit;
using PortalSubastas.Contracts.Events;
using PortalSubastas.Email.Worker.Services;

namespace PortalSubastas.Email.Worker.Consumers;

public class PreguntaRespondidaConsumer : IConsumer<PreguntaRespondidaEvent>
{
    private readonly IEmailService _emailService;
    private readonly ILogger<PreguntaRespondidaConsumer> _logger;

    public PreguntaRespondidaConsumer(IEmailService emailService, ILogger<PreguntaRespondidaConsumer> logger)
    {
        _emailService = emailService;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<PreguntaRespondidaEvent> context)
    {
        var msg = context.Message;

        if (string.IsNullOrWhiteSpace(msg.EmailProveedor))
        {
            _logger.LogWarning("⚠️ PreguntaRespondidaEvent ignorado: proveedor sin email.");
            return;
        }

        try
        {
            var subject = $"Tu consulta en subasta #{msg.NroCotizacion} fue respondida";
            var body = $"""
            <div style="font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;">
              <h2 style="color: #2563eb;">Portal de Subastas</h2>
              <p>Hola <strong>{msg.NombreProveedor}</strong>,</p>
              <p>Tu consulta en la subasta <strong>#{msg.NroCotizacion}</strong> fue respondida:</p>
              <table style="width: 100%; border-collapse: collapse;">
                <tr><td style="padding: 8px; border: 1px solid #ddd;"><strong>N° Subasta</strong></td><td style="padding: 8px; border: 1px solid #ddd;">{msg.NroCotizacion}</td></tr>
                <tr><td style="padding: 8px; border: 1px solid #ddd;"><strong>Título</strong></td><td style="padding: 8px; border: 1px solid #ddd;">{msg.Titulo}</td></tr>
              </table>
              <h3 style="margin-top: 20px;">Tu consulta:</h3>
              <blockquote style="border-left: 4px solid #f59e0b; padding-left: 16px; color: #374151; font-style: italic;">
                {msg.ContenidoPregunta}
              </blockquote>
              <h3>Respuesta:</h3>
              <blockquote style="border-left: 4px solid #10b981; padding-left: 16px; color: #374151;">
                {msg.ContenidoRespuesta}
              </blockquote>
              <hr style="border: none; border-top: 1px solid #e5e7eb;" />
              <p style="color: #6b7280; font-size: 12px;">Portal de Subastas</p>
            </div>
            """;

            await _emailService.SendEmailAsync(msg.EmailProveedor, subject, body);
            _logger.LogInformation("✅ Email de respuesta enviado a {Email} para subasta #{Nro}", msg.EmailProveedor, msg.NroCotizacion);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error al enviar email de respuesta a {Email}", msg.EmailProveedor);
            throw;
        }
    }
}
