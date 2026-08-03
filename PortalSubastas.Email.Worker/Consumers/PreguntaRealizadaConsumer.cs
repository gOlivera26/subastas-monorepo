using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PortalSubastas.Contracts.Events;
using PortalSubastas.Email.Worker.Services;

namespace PortalSubastas.Email.Worker.Consumers;

public class PreguntaRealizadaConsumer : IConsumer<PreguntaRealizadaEvent>
{
    private readonly IEmailService _emailService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<PreguntaRealizadaConsumer> _logger;

    public PreguntaRealizadaConsumer(IEmailService emailService, IConfiguration configuration, ILogger<PreguntaRealizadaConsumer> logger)
    {
        _emailService = emailService;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<PreguntaRealizadaEvent> context)
    {
        var msg = context.Message;
        if (string.IsNullOrWhiteSpace(msg.EmailOrganismo)) return;

        try
        {
            var subject = $"Nueva Consulta en Subasta #{msg.NroCotizacion}";
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
                                    <td style='background-color: #0f172a; border-bottom: 4px solid #02b8cc; padding: 30px 40px; text-align: center;'>
                                        <h1 style='color: #ffffff; margin: 0; font-size: 24px; letter-spacing: 2px;'>OWEN</h1>
                                    </td>
                                </tr>
                                <tr>
                                    <td style='padding: 40px;'>
                                        <h2 style='color: #111827; margin-top: 0; font-size: 20px;'>Nueva Consulta Recibida</h2>
                                        <p style='color: #4b5563; font-size: 15px; line-height: 1.6;'>Hola <strong>{msg.NombreOrganismo}</strong>,</p>
                                        <p style='color: #4b5563; font-size: 15px; line-height: 1.6;'>El proveedor <strong>{msg.UsuarioProveedor}</strong> ha realizado una nueva consulta en el foro de la subasta <strong>#{msg.NroCotizacion}</strong>.</p>
                                        
                                        <div style='background-color: #f8fafc; border-left: 4px solid #3b82f6; padding: 15px 20px; margin: 25px 0; color: #374151; font-style: italic; border-radius: 0 4px 4px 0;'>
                                            ""{msg.ContenidoPregunta}""
                                        </div>

                                        <div style='text-align: center; margin-top: 35px;'>
                                            <a href='{linkSubasta}' style='background-color: #02b8cc; color: #ffffff; padding: 14px 32px; text-decoration: none; font-size: 14px; font-weight: bold; border-radius: 8px; display: inline-block; letter-spacing: 0.5px;'>
                                                IR A RESPONDER
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

            await _emailService.SendEmailAsync(msg.EmailOrganismo, subject, body);
            _logger.LogInformation("✅ Email enviado a {Email} para subasta #{Nro}", msg.EmailOrganismo, msg.NroCotizacion);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error al enviar email a {Email}", msg.EmailOrganismo);
            throw;
        }
    }
}
