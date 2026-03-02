#if (UseAuth)
using MCA.Application.Interfaces;
using MCA.Infrastructure.Providers;
using Microsoft.Extensions.Logging;

namespace MCA.Infrastructure.Services;

public class EmailService : IEmailService
{
    private readonly IEmailSender _emailSender;
    private readonly AuthEmailTemplateProvider _templateProvider;
    private readonly ILogger<EmailService> _logger;

    public EmailService(
        IEmailSender emailSender,
        AuthEmailTemplateProvider templateProvider,
        ILogger<EmailService> logger)
    {
        _emailSender = emailSender;
        _templateProvider = templateProvider;
        _logger = logger;
    }

    public async Task SendPasswordResetAsync(string email, string token, string userId)
    {
        var message = _templateProvider.CreatePasswordResetEmail(email, token, userId);
        _logger.LogInformation("Sending password reset email to {Email}", email);
        await _emailSender.SendAsync(message);
    }

    public async Task SendEmailConfirmationAsync(string email, string token, string userId)
    {
        var message = _templateProvider.CreateEmailConfirmationEmail(email, token, userId);
        _logger.LogInformation("Sending email confirmation to {Email}", email);
        await _emailSender.SendAsync(message);
    }
}
#endif
