using MassTransit;
using PortalSubastas.Contracts.Events;
using PortalSubastas.Email.Worker.Services;

namespace PortalSubastas.Email.Worker.Consumers;

public class SubastaDesistidaConsumer : IConsumer<SubastaDesistidaEvent>
{
    private readonly IEmailService _emailService;
    private readonly ILogger<SubastaDesistidaConsumer> _logger;

    public SubastaDesistidaConsumer(IEmailService emailService, ILogger<SubastaDesistidaConsumer> logger)
    {
        _emailService = emailService;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<SubastaDesistidaEvent> context)
    {
        var msg = context.Message;

        if (msg.Proveedores == null || msg.Proveedores.Count == 0)
        {
            _logger.LogWarning("⚠️ SubastaDesistidaEvent ignorado: no hay proveedores para la subasta #{Nro}", msg.NroCotizacion);
            return;
        }

        var subject = $"Subasta #{msg.NroCotizacion} desistida";
        var bodyTemplate = BuildDesistimientoHtml(msg);

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
                _logger.LogInformation("✅ Email de desistimiento enviado a {Email} para subasta #{Nro}",
                    proveedor.EmailProveedor, msg.NroCotizacion);
            }
            catch (Exception ex)
            {
                failCount++;
                _logger.LogError(ex, "❌ Error al enviar email de desistimiento a {Email} (Proveedor {Id}) para subasta #{Nro}",
                    proveedor.EmailProveedor, proveedor.IdProveedor, msg.NroCotizacion);
            }
        }

        _logger.LogInformation("📊 SubastaDesistidaEvent procesado: {Success} enviados, {Failed} fallos para subasta #{Nro}",
            successCount, failCount, msg.NroCotizacion);
    }

    private static string BuildDesistimientoHtml(SubastaDesistidaEvent msg)
    {
        return @$"<div style=""font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;"">
  <h2 style=""color: #2563eb;"">Portal de Subastas</h2>
  <p>Hola <strong>{{NombreProveedor}}</strong>,</p>
  <p>La subasta <strong>#{msg.NroCotizacion}</strong> en la que participabas ha sido <strong>desistida</strong>.</p>
  <table style=""width: 100%; border-collapse: collapse;"">
    <tr><td style=""padding: 8px; border: 1px solid #ddd;""><strong>N° Subasta</strong></td><td style=""padding: 8px; border: 1px solid #ddd;"">{msg.NroCotizacion}</td></tr>
    <tr><td style=""padding: 8px; border: 1px solid #ddd;""><strong>Título</strong></td><td style=""padding: 8px; border: 1px solid #ddd;"">{msg.Titulo}</td></tr>
    <tr><td style=""padding: 8px; border: 1px solid #ddd;""><strong>Tipo</strong></td><td style=""padding: 8px; border: 1px solid #ddd;"">{msg.TipoContratacion}</td></tr>
    <tr><td style=""padding: 8px; border: 1px solid #ddd;""><strong>Motivo</strong></td><td style=""padding: 8px; border: 1px solid #ddd;"">{msg.Motivo}</td></tr>
  </table>
  <p style=""margin-top: 20px;"">Ante cualquier duda, comunícate con el organismo correspondiente.</p>
  <hr style=""border: none; border-top: 1px solid #e5e7eb;"" />
  <p style=""color: #6b7280; font-size: 12px;"">Portal de Subastas</p>
</div>";
    }
}
