using MassTransit;
using PortalSubastas.Contracts.Events;
using PortalSubastas.Email.Worker.Services;

namespace PortalSubastas.Email.Worker.Consumers;

public class SubastaPublicadaConsumer : IConsumer<SubastaPublicadaEvent>
{
    private readonly IEmailService _emailService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<SubastaPublicadaConsumer> _logger;

    public SubastaPublicadaConsumer(IEmailService emailService, IConfiguration configuration, ILogger<SubastaPublicadaConsumer> logger)
    {
        _emailService = emailService;
        _configuration = configuration;
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

        var subject = $"Nueva Subasta Publicada: #{msg.NroCotizacion}";

        // Obtenemos la URL base desde la configuración
        var frontUrl = _configuration["FrontendUrl"] ?? "http://localhost:4200";
        var linkSubasta = $"{frontUrl}/compra-venta/subastas/{msg.IdCotizacion}";

        var bodyTemplate = BuildPublicationHtml(msg, linkSubasta);

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
                _logger.LogInformation("✅ Email de publicación enviado a {Email} para subasta #{Nro}",
                    proveedor.EmailProveedor, msg.NroCotizacion);
            }
            catch (Exception ex)
            {
                failCount++;
                _logger.LogError(ex, "❌ Error al enviar email de publicación a {Email} (Proveedor {Id}) para subasta #{Nro}",
                    proveedor.EmailProveedor, proveedor.IdProveedor, msg.NroCotizacion);
            }
        }

        _logger.LogInformation("📊 SubastaPublicadaEvent procesado: {Success} enviados, {Failed} fallos para subasta #{Nro}",
            successCount, failCount, msg.NroCotizacion);
    }

    private static string BuildPublicationHtml(SubastaPublicadaEvent msg, string linkSubasta)
    {
        var fechaInicio = msg.FechaInicio?.ToString("dd/MM/yyyy HH:mm") ?? "Pendiente";
        var fechaFin = msg.FechaFin?.ToString("dd/MM/yyyy HH:mm") ?? "Pendiente";

        // Plantilla moderna con colores corporativos (Dark/Neon)
        return $@"
        <!DOCTYPE html>
        <html>
        <head>
            <meta charset='utf-8'>
        </head>
        <body style='margin: 0; padding: 0; background-color: #f3f4f6; font-family: ""Segoe UI"", Tahoma, Geneva, Verdana, sans-serif;'>
            <table width='100%' cellpadding='0' cellspacing='0' style='background-color: #f3f4f6; padding: 40px 20px;'>
                <tr>
                    <td align='center'>
                        <table width='600' cellpadding='0' cellspacing='0' style='background-color: #ffffff; border-radius: 12px; overflow: hidden; box-shadow: 0 4px 6px rgba(0, 0, 0, 0.1);'>
                            
                            <!-- Encabezado Oscuro con acento Cyan -->
                            <tr>
                                <td style='background-color: #0f172a; border-bottom: 4px solid #02b8cc; padding: 30px 40px; text-align: center;'>
                                    <h1 style='color: #ffffff; margin: 0; font-size: 24px; letter-spacing: 2px;'>OWEN</h1>
                                    <p style='color: #9ca3af; margin: 5px 0 0 0; font-size: 12px; text-transform: uppercase; letter-spacing: 1px;'>Subastas Electrónicas</p>
                                </td>
                            </tr>

                            <!-- Cuerpo del correo -->
                            <tr>
                                <td style='padding: 40px;'>
                                    <h2 style='color: #111827; margin-top: 0; font-size: 20px;'>¡Nueva Subasta Disponible!</h2>
                                    <p style='color: #4b5563; font-size: 15px; line-height: 1.6;'>Hola <strong>{{NombreProveedor}}</strong>,</p>
                                    <p style='color: #4b5563; font-size: 15px; line-height: 1.6;'>Te informamos que la subasta a la que fuiste invitado ya se encuentra publicada y lista para operar. A continuación, los detalles:</p>
                                    
                                    <!-- Tarjeta de Detalles -->
                                    <div style='background-color: #f8fafc; border: 1px solid #e5e7eb; border-radius: 8px; padding: 20px; margin: 25px 0;'>
                                        <table width='100%' cellpadding='0' cellspacing='0' style='font-size: 14px;'>
                                            <tr>
                                                <td style='padding: 8px 0; border-bottom: 1px solid #e5e7eb; color: #6b7280; width: 120px;'><strong>N° Subasta:</strong></td>
                                                <td style='padding: 8px 0; border-bottom: 1px solid #e5e7eb; color: #111827; font-family: monospace; font-size: 15px;'>{msg.NroCotizacion}</td>
                                            </tr>
                                            <tr>
                                                <td style='padding: 8px 0; border-bottom: 1px solid #e5e7eb; color: #6b7280;'><strong>Título:</strong></td>
                                                <td style='padding: 8px 0; border-bottom: 1px solid #e5e7eb; color: #111827;'>{msg.Titulo}</td>
                                            </tr>
                                            <tr>
                                                <td style='padding: 8px 0; border-bottom: 1px solid #e5e7eb; color: #6b7280;'><strong>Modalidad:</strong></td>
                                                <td style='padding: 8px 0; border-bottom: 1px solid #e5e7eb; color: #111827;'>{msg.TipoContratacion}</td>
                                            </tr>
                                            <tr>
                                                <td style='padding: 8px 0; border-bottom: 1px solid #e5e7eb; color: #6b7280;'><strong>Apertura:</strong></td>
                                                <td style='padding: 8px 0; border-bottom: 1px solid #e5e7eb; color: #111827;'>{fechaInicio}</td>
                                            </tr>
                                            <tr>
                                                <td style='padding: 8px 0; color: #6b7280;'><strong>Cierre:</strong></td>
                                                <td style='padding: 8px 0; color: #111827;'>{fechaFin}</td>
                                            </tr>
                                        </table>
                                    </div>

                                    <!-- Botón de Acción CTA -->
                                    <div style='text-align: center; margin-top: 35px;'>
                                        <a href='{linkSubasta}' style='background-color: #e4f222; color: #000000; padding: 14px 32px; text-decoration: none; font-size: 14px; font-weight: bold; border-radius: 8px; display: inline-block; letter-spacing: 0.5px;'>
                                            INGRESAR A LA SUBASTA
                                        </a>
                                    </div>
                                </td>
                            </tr>

                            <!-- Pie de página -->
                            <tr>
                                <td style='background-color: #f8fafc; border-top: 1px solid #e5e7eb; padding: 20px; text-align: center;'>
                                    <p style='color: #9ca3af; font-size: 12px; margin: 0;'>Este es un correo automático generado por el Portal de Subastas. Por favor, no respondas a este mensaje.</p>
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