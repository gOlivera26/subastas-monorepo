using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PortalSubastas.Contracts.Events;
using PortalSubastas.Email.Worker.Services;

namespace PortalSubastas.Email.Worker.Consumers;

public class ProveedorInvitadoConsumer : IConsumer<ProveedorInvitadoEvent>
{
    private readonly IEmailService _emailService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ProveedorInvitadoConsumer> _logger;

    public ProveedorInvitadoConsumer(IEmailService emailService, IConfiguration configuration, ILogger<ProveedorInvitadoConsumer> logger)
    {
        _emailService = emailService;
        _configuration = configuration;
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
            var frontUrl = _configuration["FrontendUrl"] ?? "http://localhost:4200";
            var linkSubasta = $"{frontUrl}/compra-venta/subastas/{msg.IdCotizacion}";

            var body = BuildInviteHtml(msg, linkSubasta);

            await _emailService.SendEmailAsync(msg.EmailProveedor, subject, body);

            _logger.LogInformation("✅ Email de invitación enviado a {Email} para subasta #{Nro}",
                msg.EmailProveedor, msg.NroCotizacion);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error al enviar email de invitación a {Email} para subasta #{Nro}",
                msg.EmailProveedor, msg.NroCotizacion);
            throw;
        }
    }

    private static string BuildInviteHtml(ProveedorInvitadoEvent msg, string linkSubasta)
    {
        var fechaInicio = msg.FechaInicio?.ToString("dd/MM/yyyy HH:mm") ?? "Pendiente";
        var fechaFin = msg.FechaFin?.ToString("dd/MM/yyyy HH:mm") ?? "Pendiente";

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
                                <td style='background-color: #0f172a; border-bottom: 4px solid #02b8cc; padding: 30px 40px; text-align: center;'>
                                    <h1 style='color: #ffffff; margin: 0; font-size: 24px; letter-spacing: 2px;'>OWEN</h1>
                                    <p style='color: #9ca3af; margin: 5px 0 0 0; font-size: 12px; text-transform: uppercase; letter-spacing: 1px;'>Subastas Electrónicas</p>
                                </td>
                            </tr>
                            <tr>
                                <td style='padding: 40px;'>
                                    <h2 style='color: #111827; margin-top: 0; font-size: 20px;'>¡Invitación a Participar!</h2>
                                    <p style='color: #4b5563; font-size: 15px; line-height: 1.6;'>Hola <strong>{msg.NombreProveedor}</strong>,</p>
                                    <p style='color: #4b5563; font-size: 15px; line-height: 1.6;'>Has sido invitado formalmente a participar en la siguiente subasta electrónica:</p>
                                    
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

                                    <div style='text-align: center; margin-top: 35px;'>
                                        <a href='{linkSubasta}' style='background-color: #e4f222; color: #000000; padding: 14px 32px; text-decoration: none; font-size: 14px; font-weight: bold; border-radius: 8px; display: inline-block; letter-spacing: 0.5px;'>
                                            VER DETALLES DE INVITACIÓN
                                        </a>
                                    </div>
                                </td>
                            </tr>
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