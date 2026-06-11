using Resend;

namespace PortalSubastas.Email.Worker.Services;

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
        var fromEmail = _configuration["Resend:From"]
            ?? throw new InvalidOperationException("Resend:From is not configured.");

        var message = new EmailMessage
        {
            From = fromEmail,
            To = [to],
            Subject = subject,
            HtmlBody = body
        };

        var response = await _resend.EmailSendAsync(message);

        _logger.LogInformation("📧 Email enviado a {To} | Subject: {Subject} | ResendId: {Id}", to, subject, response?.Content);
    }
}
