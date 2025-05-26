using Microsoft.AspNetCore.Identity.UI.Services;
using System.Net;
using System.Net.Mail;

namespace MinimalCleanArch.Sample.Infrastructure.Services;

/// <summary>
/// Configuration for email settings
/// </summary>
public class EmailSettings
{
    public string SmtpServer { get; set; } = string.Empty;
    public int SmtpPort { get; set; } = 587;
    public string SmtpUsername { get; set; } = string.Empty;
    public string SmtpPassword { get; set; } = string.Empty;
    public bool EnableSsl { get; set; } = true;
    public string FromEmail { get; set; } = string.Empty;
    public string FromName { get; set; } = string.Empty;
}

/// <summary>
/// Email sender implementation using SMTP
/// </summary>
public class EmailSender : IEmailSender
{
    private readonly EmailSettings _emailSettings;
    private readonly ILogger<EmailSender> _logger;

    public EmailSender(EmailSettings emailSettings, ILogger<EmailSender> logger)
    {
        _emailSettings = emailSettings;
        _logger = logger;
    }

    public async Task SendEmailAsync(string email, string subject, string htmlMessage)
    {
        try
        {
            var mailMessage = new MailMessage
            {
                From = new MailAddress(_emailSettings.FromEmail, _emailSettings.FromName),
                Subject = subject,
                Body = htmlMessage,
                IsBodyHtml = true
            };

            mailMessage.To.Add(email);

            using var smtpClient = new SmtpClient(_emailSettings.SmtpServer, _emailSettings.SmtpPort)
            {
                Credentials = new NetworkCredential(_emailSettings.SmtpUsername, _emailSettings.SmtpPassword),
                EnableSsl = _emailSettings.EnableSsl
            };

            await smtpClient.SendMailAsync(mailMessage);
            _logger.LogInformation("Email sent successfully to {Email}", email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {Email}", email);
            throw;
        }
    }
}
