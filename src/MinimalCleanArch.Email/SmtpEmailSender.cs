using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MinimalCleanArch.Email;

public sealed class SmtpEmailSender : IEmailSender
{
    private readonly EmailOptions _options;
    private readonly ILogger<SmtpEmailSender> _logger;

    public SmtpEmailSender(IOptions<EmailOptions> options, ILogger<SmtpEmailSender> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        using var client = new SmtpClient(_options.SmtpServer, _options.Port)
        {
            EnableSsl = _options.EnableSsl,
            UseDefaultCredentials = _options.UseDefaultCredentials,
            Timeout = Math.Max(1, _options.TimeoutSeconds) * 1000
        };

        if (!_options.UseDefaultCredentials && !string.IsNullOrEmpty(_options.Username))
        {
            client.Credentials = new NetworkCredential(_options.Username, _options.Password);
        }

        using var mail = new MailMessage
        {
            From = new MailAddress(_options.SenderEmail, _options.SenderName),
            Subject = message.Subject,
            Body = message.HtmlBody,
            IsBodyHtml = true
        };

        mail.To.Add(message.To);

        if (!string.IsNullOrEmpty(message.TextBody))
        {
            mail.AlternateViews.Add(AlternateView.CreateAlternateViewFromString(message.TextBody, null, "text/plain"));
        }

        if (!string.IsNullOrWhiteSpace(message.CorrelationId))
        {
            mail.Headers.Add("X-Correlation-ID", message.CorrelationId);
        }

        _logger.LogDebug("Sending SMTP email to {To}: {Subject}", message.To, message.Subject);
        await client.SendMailAsync(mail, cancellationToken);
    }
}
