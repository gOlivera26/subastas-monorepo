using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PortalSubastas.Contracts.Events;
using PortalSubastas.Email.Worker.Services;

namespace PortalSubastas.Email.Worker.Consumers;

public class SubastaProrrogadaConsumer : IConsumer<SubastaProrrogadaEvent>
{
    private readonly IEmailService _emailService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<SubastaProrrogadaConsumer> _logger;

    public SubastaProrrogadaConsumer(IEmailService emailService, IConfiguration configuration, ILogger<SubastaProrrogadaConsumer> logger)
    {
        _emailService = emailService;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<SubastaProrrogadaEvent> context)
    {
        var msg = context.Message;

        if (msg.Proveedores == null || msg.Proveedores.Count == 0) return;

        var subject = $"La subasta #{msg.NroCotizacion} fue prorrogada";
        var frontUrl = _configuration["FrontendUrl"] ?? "http://localhost:4200";
        var linkSubasta = $"{frontUrl}/compra-venta/subastas/{msg.IdCotizacion}";

        var bodyTemplate = BuildProrrogaHtml(msg, linkSubasta);

        foreach (var proveedor in msg.Proveedores)
        {
            if (string.IsNullOrWhiteSpace(proveedor.EmailProveedor)) continue;

            try
            {
                var personalizedBody = bodyTemplate.Replace("{NombreProveedor}", proveedor.NombreProveedor);
                await _emailService.SendEmailAsync(proveedor.EmailProveedor, subject, personalizedBody);
                _logger.LogInformation("✅ Email de prórroga enviado a {Email}", proveedor.EmailProveedor);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error al enviar email de prórroga a {Email}", proveedor.EmailProveedor);
            }
        }
    }

    private static string BuildProrrogaHtml(SubastaProrrogadaEvent msg, string linkSubasta)
    {
        var fechaOriginal = msg.FechaFinOriginal?.ToString("dd/MM/yyyy HH:mm") ?? "—";
        var fechaNueva = msg.FechaFinNueva?.ToString("dd/MM/yyyy HH:mm") ?? "—";

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
                                <td style='background-color: #0f172a; border-bottom: 4px solid #f59e0b; padding: 30px 40px; text-align: center;'>
                                    <h1 style='color: #ffffff; margin: 0; font-size: 24px; letter-spacing: 2px;'>OWEN</h1>
                                    <p style='color: #9ca3af; margin: 5px 0 0 0; font-size: 12px; text-transform: uppercase; letter-spacing: 1px;'>Subastas Electrónicas</p>
                                </td>
                            </tr>
                            <tr>
                                <td style='padding: 40px;'>
                                    <h2 style='color: #111827; margin-top: 0; font-size: 20px;'>Tiempo de Subasta Extendido</h2>
                                    <p style='color: #4b5563; font-size: 15px; line-height: 1.6;'>Hola <strong>{{NombreProveedor}}</strong>,</p>
                                    <p style='color: #4b5563; font-size: 15px; line-height: 1.6;'>La subasta a la que fuiste invitado ha sido <strong>prorrogada</strong>. A continuación, los nuevos tiempos:</p>
                                    
                                    <div style='background-color: #fffbeb; border: 1px solid #fcd34d; border-radius: 8px; padding: 20px; margin: 25px 0;'>
                                        <table width='100%' cellpadding='0' cellspacing='0' style='font-size: 14px;'>
                                            <tr>
                                                <td style='padding: 8px 0; border-bottom: 1px solid #fde68a; color: #92400e; width: 140px;'><strong>N° Subasta:</strong></td>
                                                <td style='padding: 8px 0; border-bottom: 1px solid #fde68a; color: #92400e; font-family: monospace; font-size: 15px;'>{msg.NroCotizacion}</td>
                                            </tr>
                                            <tr>
                                                <td style='padding: 8px 0; border-bottom: 1px solid #fde68a; color: #92400e;'><strong>Cierre Original:</strong></td>
                                                <td style='padding: 8px 0; border-bottom: 1px solid #fde68a; color: #92400e; text-decoration: line-through;'>{fechaOriginal}</td>
                                            </tr>
                                            <tr>
                                                <td style='padding: 8px 0; color: #92400e;'><strong>Nuevo Cierre:</strong></td>
                                                <td style='padding: 8px 0; color: #92400e; font-weight: bold;'>{fechaNueva}</td>
                                            </tr>
                                        </table>
                                    </div>

                                    <div style='text-align: center; margin-top: 35px;'>
                                        <a href='{linkSubasta}' style='background-color: #111827; color: #ffffff; padding: 14px 32px; text-decoration: none; font-size: 14px; font-weight: bold; border-radius: 8px; display: inline-block; letter-spacing: 0.5px;'>
                                            VOLVER A LA SALA DE SUBASTA
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
