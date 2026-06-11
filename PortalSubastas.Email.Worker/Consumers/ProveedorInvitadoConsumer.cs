using MassTransit;
using PortalSubastas.Contracts.Events;
using PortalSubastas.Email.Worker.Services;

namespace PortalSubastas.Email.Worker.Consumers;

public class ProveedorInvitadoConsumer : IConsumer<ProveedorInvitadoEvent>
{
    private readonly IEmailService _emailService;
    private readonly ILogger<ProveedorInvitadoConsumer> _logger;

    public ProveedorInvitadoConsumer(IEmailService emailService, ILogger<ProveedorInvitadoConsumer> logger)
    {
        _emailService = emailService;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<ProveedorInvitadoEvent> context)
    {
        var msg = context.Message;

        if (string.IsNullOrWhiteSpace(msg.EmailProveedor))
        {
            _logger.LogWarning("⚠️ ProveedorInvitadoEvent ignorado: el proveedor {IdProveedor} no tiene email.", msg.IdProveedor);
            return;
        }

        try
        {
            var subject = $"Has sido invitado a la subasta #{msg.NroCotizacion}";
            var body = BuildInviteHtml(msg);

            await _emailService.SendEmailAsync(msg.EmailProveedor, subject, body);

            _logger.LogInformation("✅ Email de invitación enviado a {Email} para subasta #{Nro}",
                msg.EmailProveedor, msg.NroCotizacion);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error al enviar email de invitación a {Email} para subasta #{Nro}",
                msg.EmailProveedor, msg.NroCotizacion);
            throw; // MassTransit maneja el retry
        }
    }

    private static string BuildInviteHtml(ProveedorInvitadoEvent msg)
    {
        return $"""
        <div style="font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;">
          <h2 style="color: #2563eb;">Portal de Subastas</h2>
          <p>Hola <strong>{msg.NombreProveedor}</strong>,</p>
          <p>Has sido invitado a participar en la siguiente subasta:</p>
          <table style="width: 100%; border-collapse: collapse;">
            <tr><td style="padding: 8px; border: 1px solid #ddd;"><strong>N° Subasta</strong></td><td style="padding: 8px; border: 1px solid #ddd;">{msg.NroCotizacion}</td></tr>
            <tr><td style="padding: 8px; border: 1px solid #ddd;"><strong>Título</strong></td><td style="padding: 8px; border: 1px solid #ddd;">{msg.Titulo}</td></tr>
            <tr><td style="padding: 8px; border: 1px solid #ddd;"><strong>Tipo</strong></td><td style="padding: 8px; border: 1px solid #ddd;">{msg.TipoContratacion}</td></tr>
            <tr><td style="padding: 8px; border: 1px solid #ddd;"><strong>Inicio</strong></td><td style="padding: 8px; border: 1px solid #ddd;">{msg.FechaInicio?.ToString("dd/MM/yyyy HH:mm") ?? "Pendiente"}</td></tr>
            <tr><td style="padding: 8px; border: 1px solid #ddd;"><strong>Fin</strong></td><td style="padding: 8px; border: 1px solid #ddd;">{msg.FechaFin?.ToString("dd/MM/yyyy HH:mm") ?? "Pendiente"}</td></tr>
          </table>
          <p style="margin-top: 20px;">Accede al portal para más información.</p>
          <hr style="border: none; border-top: 1px solid #e5e7eb;" />
          <p style="color: #6b7280; font-size: 12px;">Portal de Subastas</p>
        </div>
        """;
    }
}
