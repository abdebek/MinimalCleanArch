#if (UseAuth)
using MCA.Application.Interfaces;
using MCA.Infrastructure.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Mail;

namespace MCA.Infrastructure.Services;

public class SmtpEmailSender : IEmailSender
{
    private readonly EmailSettings _settings;
    private readonly ILogger<SmtpEmailSender> _logger;

    public SmtpEmailSender(IOptions<EmailSettings> settings, ILogger<SmtpEmailSender> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        using var client = new SmtpClient(_settings.SmtpServer, _settings.Port)
        {
            EnableSsl = _settings.EnableSsl,
            UseDefaultCredentials = _settings.UseDefaultCredentials
        };

        if (!_settings.UseDefaultCredentials && !string.IsNullOrEmpty(_settings.Username))
            client.Credentials = new NetworkCredential(_settings.Username, _settings.Password);

        using var mail = new MailMessage
        {
            From = new MailAddress(_settings.SenderEmail, _settings.SenderName),
            Subject = message.Subject,
            Body = message.HtmlBody,
            IsBodyHtml = true
        };

        mail.To.Add(message.To);

        if (!string.IsNullOrEmpty(message.TextBody))
            mail.AlternateViews.Add(AlternateView.CreateAlternateViewFromString(message.TextBody, null, "text/plain"));

        _logger.LogDebug("Sending email to {To}: {Subject}", message.To, message.Subject);
        await client.SendMailAsync(mail, cancellationToken);
    }
}
#endif
