using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PortalSubastas.Contracts.Events;
using PortalSubastas.Email.Worker.Services;

namespace PortalSubastas.Email.Worker.Consumers;

public class PreguntaRespondidaConsumer : IConsumer<PreguntaRespondidaEvent>
{
    private readonly IEmailService _emailService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<PreguntaRespondidaConsumer> _logger;

    public PreguntaRespondidaConsumer(IEmailService emailService, IConfiguration configuration, ILogger<PreguntaRespondidaConsumer> logger)
    {
        _emailService = emailService;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<PreguntaRespondidaEvent> context)
    {
        var msg = context.Message;
        if (string.IsNullOrWhiteSpace(msg.EmailProveedor)) return;

        try
        {
            var subject = $"Respuesta a tu consulta en subasta #{msg.NroCotizacion}";
            var frontUrl = _configuration["FrontendUrl"] ?? "http://localhost:4200";
            var linkSubasta = $"{frontUrl}/compra-venta/subastas/{msg.IdCotizacion}";

            var body = $@"
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
                                        <h2 style='color: #111827; margin-top: 0; font-size: 20px;'>Consulta Respondida</h2>
                                        <p style='color: #4b5563; font-size: 15px; line-height: 1.6;'>Hola <strong>{msg.NombreProveedor}</strong>,</p>
                                        <p style='color: #4b5563; font-size: 15px; line-height: 1.6;'>La administración ha respondido oficialmente a tu consulta en la subasta <strong>#{msg.NroCotizacion}</strong>:</p>
                                        
                                        <!-- Tu Consulta -->
                                        <div style='margin-top: 20px; margin-bottom: 5px; color: #6b7280; font-size: 12px; font-weight: bold; text-transform: uppercase;'>TU CONSULTA:</div>
                                        <div style='background-color: #fffbeb; border-left: 4px solid #f59e0b; padding: 15px 20px; color: #4b5563; font-style: italic; border-radius: 0 4px 4px 0; margin-bottom: 20px;'>
                                            ""{msg.ContenidoPregunta}""
                                        </div>

                                        <!-- Respuesta -->
                                        <div style='margin-bottom: 5px; color: #10b981; font-size: 12px; font-weight: bold; text-transform: uppercase;'>RESPUESTA OFICIAL:</div>
                                        <div style='background-color: #ecfdf5; border-left: 4px solid #10b981; padding: 15px 20px; color: #111827; border-radius: 0 4px 4px 0; white-space: pre-wrap;'>
                                            {msg.ContenidoRespuesta}
                                        </div>

                                        <div style='text-align: center; margin-top: 35px;'>
                                            <a href='{linkSubasta}' style='background-color: #111827; color: #ffffff; padding: 14px 32px; text-decoration: none; font-size: 14px; font-weight: bold; border-radius: 8px; display: inline-block; letter-spacing: 0.5px;'>
                                                VER EN EL FORO
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

            await _emailService.SendEmailAsync(msg.EmailProveedor, subject, body);
            _logger.LogInformation("✅ Email enviado a {Email}", msg.EmailProveedor);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error al enviar email a {Email}", msg.EmailProveedor);
            throw;
        }
    }
}