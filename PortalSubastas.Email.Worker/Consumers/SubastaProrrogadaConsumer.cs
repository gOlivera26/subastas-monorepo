using MassTransit;
using PortalSubastas.Contracts.Events;
using PortalSubastas.Email.Worker.Services;

namespace PortalSubastas.Email.Worker.Consumers;

public class SubastaProrrogadaConsumer : IConsumer<SubastaProrrogadaEvent>
{
    private readonly IEmailService _emailService;
    private readonly ILogger<SubastaProrrogadaConsumer> _logger;

    public SubastaProrrogadaConsumer(IEmailService emailService, ILogger<SubastaProrrogadaConsumer> logger)
    {
        _emailService = emailService;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<SubastaProrrogadaEvent> context)
    {
        var msg = context.Message;

        if (msg.Proveedores == null || msg.Proveedores.Count == 0)
        {
            _logger.LogWarning("⚠️ SubastaProrrogadaEvent ignorado: no hay proveedores para la subasta #{Nro}", msg.NroCotizacion);
            return;
        }

        var subject = $"La subasta #{msg.NroCotizacion} fue prorrogada";
        var bodyTemplate = BuildProrrogaHtml(msg);

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
                var personalizedBody = bodyTemplate.Replace("{NombreProveedor}", proveedor.NombreProveedor);
                await _emailService.SendEmailAsync(proveedor.EmailProveedor, subject, personalizedBody);
                successCount++;
                _logger.LogInformation("✅ Email de prórroga enviado a {Email} para subasta #{Nro}",
                    proveedor.EmailProveedor, msg.NroCotizacion);
            }
            catch (Exception ex)
            {
                failCount++;
                _logger.LogError(ex, "❌ Error al enviar email de prórroga a {Email} (Proveedor {Id}) para subasta #{Nro}",
                    proveedor.EmailProveedor, proveedor.IdProveedor, msg.NroCotizacion);
            }
        }

        _logger.LogInformation("📊 SubastaProrrogadaEvent procesado: {Success} enviados, {Failed} fallos para subasta #{Nro}",
            successCount, failCount, msg.NroCotizacion);
    }

    private static string BuildProrrogaHtml(SubastaProrrogadaEvent msg)
    {
        var fechaOriginal = msg.FechaFinOriginal?.ToString("dd/MM/yyyy HH:mm") ?? "—";
        var fechaNueva = msg.FechaFinNueva?.ToString("dd/MM/yyyy HH:mm") ?? "—";

        return @$"<div style=""font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;"">
  <h2 style=""color: #2563eb;"">Portal de Subastas</h2>
  <p>Hola <strong>{{NombreProveedor}}</strong>,</p>
  <p>La subasta a la que fuiste invitado fue <strong>prorrogada</strong>.</p>
  <table style=""width: 100%; border-collapse: collapse;"">
    <tr><td style=""padding: 8px; border: 1px solid #ddd;""><strong>N° Subasta</strong></td><td style=""padding: 8px; border: 1px solid #ddd;"">{msg.NroCotizacion}</td></tr>
    <tr><td style=""padding: 8px; border: 1px solid #ddd;""><strong>Título</strong></td><td style=""padding: 8px; border: 1px solid #ddd;"">{msg.Titulo}</td></tr>
    <tr><td style=""padding: 8px; border: 1px solid #ddd;""><strong>Tipo</strong></td><td style=""padding: 8px; border: 1px solid #ddd;"">{msg.TipoContratacion}</td></tr>
    <tr><td style=""padding: 8px; border: 1px solid #ddd;""><strong>Fecha fin original</strong></td><td style=""padding: 8px; border: 1px solid #ddd;"">{fechaOriginal}</td></tr>
    <tr><td style=""padding: 8px; border: 1px solid #ddd;""><strong>Nueva fecha fin</strong></td><td style=""padding: 8px; border: 1px solid #ddd;"">{fechaNueva}</td></tr>
    <tr><td style=""padding: 8px; border: 1px solid #ddd;""><strong>Minutos agregados</strong></td><td style=""padding: 8px; border: 1px solid #ddd;"">{msg.MinutosAgregados} min</td></tr>
  </table>
  <p style=""margin-top: 20px;"">Accede al portal para más información.</p>
  <hr style=""border: none; border-top: 1px solid #e5e7eb;"" />
  <p style=""color: #6b7280; font-size: 12px;"">Portal de Subastas</p>
</div>";
    }
}
