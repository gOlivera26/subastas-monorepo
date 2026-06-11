using MassTransit;
using PortalSubastas.Contracts.Events;
using PortalSubastas.Email.Worker.Services;

namespace PortalSubastas.Email.Worker.Consumers;

public class SubastaPublicadaConsumer : IConsumer<SubastaPublicadaEvent>
{
    private readonly IEmailService _emailService;
    private readonly ILogger<SubastaPublicadaConsumer> _logger;

    public SubastaPublicadaConsumer(IEmailService emailService, ILogger<SubastaPublicadaConsumer> logger)
    {
        _emailService = emailService;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<SubastaPublicadaEvent> context)
    {
        var msg = context.Message;

        if (msg.Proveedores == null || msg.Proveedores.Count == 0)
        {
            _logger.LogWarning("⚠️ SubastaPublicadaEvent ignorado: no hay proveedores para la subasta #{Nro}", msg.NroCotizacion);
            return;
        }

        var subject = $"La subasta #{msg.NroCotizacion} ya está publicada";
        var bodyTemplate = BuildPublicationHtml(msg);

        int successCount = 0;
        int failCount = 0;

        foreach (var proveedor in msg.Proveedores)
        {
            if (string.IsNullOrWhiteSpace(proveedor.EmailProveedor))
            {
                _logger.LogWarning("⚠️ Proveedor {IdProveedor} sin email, se salta el envío.", proveedor.IdProveedor);
                continue;
            }

            try
            {
                // Personalizar el body con el nombre del proveedor
                var personalizedBody = bodyTemplate.Replace("{NombreProveedor}", proveedor.NombreProveedor);
                await _emailService.SendEmailAsync(proveedor.EmailProveedor, subject, personalizedBody);
                successCount++;
                _logger.LogInformation("✅ Email de publicación enviado a {Email} para subasta #{Nro}",
                    proveedor.EmailProveedor, msg.NroCotizacion);
            }
            catch (Exception ex)
            {
                failCount++;
                _logger.LogError(ex, "❌ Error al enviar email de publicación a {Email} (Proveedor {Id}) para subasta #{Nro}",
                    proveedor.EmailProveedor, proveedor.IdProveedor, msg.NroCotizacion);
                // Continuamos con los demás — un fallo no debe bloquear el batch
            }
        }

        _logger.LogInformation("📊 SubastaPublicadaEvent procesado: {Success} enviados, {Failed} fallos para subasta #{Nro}",
            successCount, failCount, msg.NroCotizacion);
    }

    private static string BuildPublicationHtml(SubastaPublicadaEvent msg)
    {
        var fechaInicio = msg.FechaInicio?.ToString("dd/MM/yyyy HH:mm") ?? "Pendiente";
        var fechaFin = msg.FechaFin?.ToString("dd/MM/yyyy HH:mm") ?? "Pendiente";

        // Usamos un placeholder {NombreProveedor} que se reemplaza por cada proveedor
        return @$"<div style=""font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;"">
  <h2 style=""color: #2563eb;"">Portal de Subastas</h2>
  <p>Hola <strong>{{NombreProveedor}}</strong>,</p>
  <p>La subasta a la que fuiste invitado ya está publicada. Ya puedes participar ingresando al portal.</p>
  <table style=""width: 100%; border-collapse: collapse;"">
    <tr><td style=""padding: 8px; border: 1px solid #ddd;""><strong>N° Subasta</strong></td><td style=""padding: 8px; border: 1px solid #ddd;"">{msg.NroCotizacion}</td></tr>
    <tr><td style=""padding: 8px; border: 1px solid #ddd;""><strong>Título</strong></td><td style=""padding: 8px; border: 1px solid #ddd;"">{msg.Titulo}</td></tr>
    <tr><td style=""padding: 8px; border: 1px solid #ddd;""><strong>Tipo</strong></td><td style=""padding: 8px; border: 1px solid #ddd;"">{msg.TipoContratacion}</td></tr>
    <tr><td style=""padding: 8px; border: 1px solid #ddd;""><strong>Inicio</strong></td><td style=""padding: 8px; border: 1px solid #ddd;"">{fechaInicio}</td></tr>
    <tr><td style=""padding: 8px; border: 1px solid #ddd;""><strong>Fin</strong></td><td style=""padding: 8px; border: 1px solid #ddd;"">{fechaFin}</td></tr>
  </table>
  <p style=""margin-top: 20px;"">Accede al portal para participar.</p>
  <hr style=""border: none; border-top: 1px solid #e5e7eb;"" />
  <p style=""color: #6b7280; font-size: 12px;"">Portal de Subastas</p>
</div>";
    }
}
