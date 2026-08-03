using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PortalSubastas.Contracts.Events;
using PortalSubastas.Email.Worker.Services;

namespace PortalSubastas.Email.Worker.Consumers;

public class SubastaDesistidaConsumer : IConsumer<SubastaDesistidaEvent>
{
    private readonly IEmailService _emailService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<SubastaDesistidaConsumer> _logger;

    public SubastaDesistidaConsumer(IEmailService emailService, IConfiguration configuration, ILogger<SubastaDesistidaConsumer> logger)
    {
        _emailService = emailService;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<SubastaDesistidaEvent> context)
    {
        var msg = context.Message;

        if (msg.Proveedores == null || msg.Proveedores.Count == 0) return;

        var subject = $"Subasta #{msg.NroCotizacion} Desistida / Cancelada";
        var frontUrl = _configuration["FrontendUrl"] ?? "http://localhost:4200";
        var linkSubasta = $"{frontUrl}/compra-venta/subastas";

        var bodyTemplate = BuildDesistimientoHtml(msg, linkSubasta);

        foreach (var proveedor in msg.Proveedores)
        {
            if (string.IsNullOrWhiteSpace(proveedor.EmailProveedor)) continue;

            try
            {
                var personalizedBody = bodyTemplate.Replace("{NombreProveedor}", proveedor.NombreProveedor);
                await _emailService.SendEmailAsync(proveedor.EmailProveedor, subject, personalizedBody);
                _logger.LogInformation("✅ Email de desistimiento enviado a {Email}", proveedor.EmailProveedor);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error al enviar email de desistimiento a {Email}", proveedor.EmailProveedor);
            }
        }
    }

    private static string BuildDesistimientoHtml(SubastaDesistidaEvent msg, string linkSubasta)
    {
        return $@"
        <!DOCTYPE html>
        <html>
        <head><meta charset='utf-8'></head>
        <body style='margin: 0; padding: 0; background-color: #f3f4f6; font-family: ""Segoe UI"", Tahoma, Geneva, Verdana, sans-serif;'>
            <table width='100%' cellpadding='0' cellspacing='0' style='background-color: #f3f4f6; padding: 40px 20px;'>
                <tr>
                    <td align='center'>
                        <table width='600' cellpadding='0' cellspacing='0' style='background-color: #ffffff; border-radius: 12px; overflow: hidden; box-shadow: 0 4px 6px rgba(0, 0, 0, 0.1);'>
                            <tr>
                                <td style='background-color: #0f172a; border-bottom: 4px solid #ef4444; padding: 30px 40px; text-align: center;'>
                                    <h1 style='color: #ffffff; margin: 0; font-size: 24px; letter-spacing: 2px;'>OWEN</h1>
                                    <p style='color: #9ca3af; margin: 5px 0 0 0; font-size: 12px; text-transform: uppercase; letter-spacing: 1px;'>Subastas Electrónicas</p>
                                </td>
                            </tr>
                            <tr>
                                <td style='padding: 40px;'>
                                    <h2 style='color: #111827; margin-top: 0; font-size: 20px;'>Aviso de Cancelación</h2>
                                    <p style='color: #4b5563; font-size: 15px; line-height: 1.6;'>Hola <strong>{{NombreProveedor}}</strong>,</p>
                                    <p style='color: #4b5563; font-size: 15px; line-height: 1.6;'>La subasta electrónica <strong>#{msg.NroCotizacion}</strong> en la que te encontrabas participando ha sido formalmente <strong>desistida/cancelada</strong> por el organismo contratante.</p>
                                    
                                    <div style='background-color: #fef2f2; border: 1px solid #fecaca; border-radius: 8px; padding: 20px; margin: 25px 0;'>
                                        <p style='margin: 0 0 10px 0; color: #991b1b; font-size: 14px; font-weight: bold;'>Motivo reportado:</p>
                                        <p style='margin: 0; color: #991b1b; font-size: 14px; font-style: italic;'>""{msg.Motivo}""</p>
                                    </div>

                                    <div style='text-align: center; margin-top: 35px;'>
                                        <a href='{linkSubasta}' style='background-color: #e5e7eb; color: #374151; padding: 14px 32px; text-decoration: none; font-size: 14px; font-weight: bold; border-radius: 8px; display: inline-block; letter-spacing: 0.5px;'>
                                            VOLVER AL TABLERO
                                        </a>
                                    </div>
                                </td>
                            </tr>
                            <tr>
                                <td style='background-color: #f8fafc; border-top: 1px solid #e5e7eb; padding: 20px; text-align: center;'>
                                    <p style='color: #9ca3af; font-size: 12px; margin: 0;'>Portal de Subastas OWEN.</p>
                                </td>
                            </tr>
                        </table>
                    </td>
                </tr>
            </table>
        </body>
        </html>";
    }
}
