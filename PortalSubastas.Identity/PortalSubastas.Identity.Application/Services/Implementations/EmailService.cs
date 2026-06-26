using Microsoft.Extensions.Logging;
using Resend;

namespace PortalSubastas.Identity.Application.Services.Implementations;

public class EmailService : IEmailService
{
    private readonly IResend _resend;
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IResend resend, IConfiguration configuration, ILogger<EmailService> logger)
    {
        _resend = resend;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SendEmailAsync(string to, string subject, string body)
    {
        var from = _configuration["Resend:From"];
        var apiKey = _configuration["Resend:ApiKey"];

        if (string.IsNullOrWhiteSpace(to))
        {
            _logger.LogWarning("No se envio el email '{Subject}' porque el destinatario esta vacio.", subject);
            return;
        }

        if (string.IsNullOrWhiteSpace(from) || string.IsNullOrWhiteSpace(apiKey))
        {
            _logger.LogWarning("No se envio el email '{Subject}' a {To} porque falta configurar Resend:From o Resend:ApiKey.", subject, to);
            return;
        }

        try
        {
            var message = new EmailMessage();
            message.From = from;
            message.To.Add(to);
            message.Subject = subject;
            message.HtmlBody = body;

            await _resend.EmailSendAsync(message);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "No se pudo enviar el email '{Subject}' a {To}.", subject, to);
        }
    }
}
