using Resend;

namespace PortalSubastas.Identity.Application.Services.Implementations;

public class EmailService : IEmailService
{
    private readonly IResend _resend;
    private readonly IConfiguration _configuration;

    public EmailService(IResend resend, IConfiguration configuration)
    {
        _resend = resend;
        _configuration = configuration;
    }

    public async Task SendEmailAsync(string to, string subject, string body)
    {
        var from = _configuration["Resend:From"]!;

        var message = new EmailMessage();
        message.From = from;
        message.To.Add(to);
        message.Subject = subject;
        message.HtmlBody = body;

        await _resend.EmailSendAsync(message);
    }
}
