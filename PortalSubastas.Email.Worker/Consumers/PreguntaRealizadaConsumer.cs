using MassTransit;
using PortalSubastas.Contracts.Events;
using PortalSubastas.Email.Worker.Services;

namespace PortalSubastas.Email.Worker.Consumers;

public class PreguntaRealizadaConsumer : IConsumer<PreguntaRealizadaEvent>
{
    private readonly IEmailService _emailService;
    private readonly ILogger<PreguntaRealizadaConsumer> _logger;

    public PreguntaRealizadaConsumer(IEmailService emailService, ILogger<PreguntaRealizadaConsumer> logger)
    {
        _emailService = emailService;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<PreguntaRealizadaEvent> context)
    {
        var msg = context.Message;

        if (string.IsNullOrWhiteSpace(msg.EmailOrganismo))
        {
            _logger.LogWarning("⚠️ PreguntaRealizadaEvent ignorado: organismo sin email para cotización {IdCotizacion}.", msg.IdCotizacion);
            return;
        }

        try
        {
            var subject = $"Nueva consulta en subasta #{msg.NroCotizacion}";
            var body = $"""
            <div style="font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;">
              <h2 style="color: #2563eb;">Portal de Subastas</h2>
              <p>Hola <strong>{msg.NombreOrganismo}</strong>,</p>
              <p>Se ha recibido una nueva consulta del proveedor <strong>{msg.UsuarioProveedor}</strong> en la subasta:</p>
              <table style="width: 100%; border-collapse: collapse;">
                <tr><td style="padding: 8px; border: 1px solid #ddd;"><strong>N° Subasta</strong></td><td style="padding: 8px; border: 1px solid #ddd;">{msg.NroCotizacion}</td></tr>
                <tr><td style="padding: 8px; border: 1px solid #ddd;"><strong>Título</strong></td><td style="padding: 8px; border: 1px solid #ddd;">{msg.Titulo}</td></tr>
              </table>
              <h3 style="margin-top: 20px;">Consulta recibida:</h3>
              <blockquote style="border-left: 4px solid #2563eb; padding-left: 16px; color: #374151; font-style: italic;">
                {msg.ContenidoPregunta}
              </blockquote>
              <p>Accede al portal para responder esta consulta.</p>
              <hr style="border: none; border-top: 1px solid #e5e7eb;" />
              <p style="color: #6b7280; font-size: 12px;">Portal de Subastas</p>
            </div>
            """;

            await _emailService.SendEmailAsync(msg.EmailOrganismo, subject, body);
            _logger.LogInformation("✅ Email de nueva consulta enviado a {Email} para subasta #{Nro}", msg.EmailOrganismo, msg.NroCotizacion);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error al enviar email de nueva consulta a {Email} para subasta #{Nro}", msg.EmailOrganismo, msg.NroCotizacion);
            throw;
        }
    }
}
