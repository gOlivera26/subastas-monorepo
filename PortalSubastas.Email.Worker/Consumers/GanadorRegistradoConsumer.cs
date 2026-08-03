using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PortalSubastas.Contracts.Events;
using PortalSubastas.Email.Worker.Services;

namespace PortalSubastas.Email.Worker.Consumers;

public class GanadorRegistradoConsumer : IConsumer<GanadorRegistradoEvent>
{
    private readonly IEmailService _emailService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<GanadorRegistradoConsumer> _logger;

    public GanadorRegistradoConsumer(IEmailService emailService, IConfiguration configuration, ILogger<GanadorRegistradoConsumer> logger)
    {
        _emailService = emailService;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<GanadorRegistradoEvent> context)
    {
        var msg = context.Message;
        if (string.IsNullOrWhiteSpace(msg.EmailProveedor)) return;

        try
        {
            var subject = $"¡Has resultado ganador en la subasta #{msg.NroCotizacion}!";
            var frontUrl = _configuration["FrontendUrl"] ?? "http://localhost:4200";
            var linkSubasta = $"{frontUrl}/compra-venta/subastas/{msg.IdCotizacion}";

            var body = BuildGanadorHtml(msg, linkSubasta);
            await _emailService.SendEmailAsync(msg.EmailProveedor, subject, body);
            _logger.LogInformation("✅ Email ganador enviado a {Email}", msg.EmailProveedor);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error enviando a {Email}", msg.EmailProveedor);
            throw;
        }
    }

    private static string BuildGanadorHtml(GanadorRegistradoEvent msg, string linkSubasta)
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
                                <td style='background-color: #0f172a; border-bottom: 4px solid #10b981; padding: 30px 40px; text-align: center;'>
                                    <h1 style='color: #ffffff; margin: 0; font-size: 24px; letter-spacing: 2px;'>OWEN</h1>
                                </td>
                            </tr>
                            <tr>
                                <td style='padding: 40px;'>
                                    <h2 style='color: #111827; margin-top: 0; font-size: 20px;'>¡Adjudicación Confirmada!</h2>
                                    <p style='color: #4b5563; font-size: 15px; line-height: 1.6;'>Hola <strong>{msg.NombreProveedor}</strong>,</p>
                                    <p style='color: #4b5563; font-size: 15px; line-height: 1.6;'>¡Felicitaciones! Nos alegra informarte que tu oferta ha resultado <strong>ganadora</strong> en la siguiente subasta electrónica:</p>
                                    
                                    <div style='background-color: #f8fafc; border: 1px solid #e5e7eb; border-radius: 8px; padding: 20px; margin: 25px 0;'>
                                        <table width='100%' cellpadding='0' cellspacing='0' style='font-size: 14px;'>
                                            <tr>
                                                <td style='padding: 8px 0; border-bottom: 1px solid #e5e7eb; color: #6b7280; width: 120px;'><strong>N° Subasta:</strong></td>
                                                <td style='padding: 8px 0; border-bottom: 1px solid #e5e7eb; color: #111827; font-family: monospace; font-size: 15px;'>{msg.NroCotizacion}</td>
                                            </tr>
                                            <tr>
                                                <td style='padding: 8px 0; border-bottom: 1px solid #e5e7eb; color: #6b7280;'><strong>Monto Ganador:</strong></td>
                                                <td style='padding: 8px 0; border-bottom: 1px solid #e5e7eb; color: #10b981; font-weight: bold; font-family: monospace; font-size: 16px;'>${msg.MontoGanador:N2}</td>
                                            </tr>
                                        </table>
                                    </div>

                                    <h3 style='color: #111827; font-size: 16px; margin-top: 30px;'>Próximos Pasos</h3>
                                    <p style='color: #4b5563; font-size: 14px; line-height: 1.6;'>Deberás presentar la documentación respaldatoria ante el organismo contratante dentro de los plazos establecidos en el pliego de condiciones.</p>

                                    <div style='text-align: center; margin-top: 35px;'>
                                        <a href='{linkSubasta}' style='background-color: #10b981; color: #ffffff; padding: 14px 32px; text-decoration: none; font-size: 14px; font-weight: bold; border-radius: 8px; display: inline-block; letter-spacing: 0.5px;'>
                                            GESTIONAR DOCUMENTACIÓN
                                        </a>
                                    </div>
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
