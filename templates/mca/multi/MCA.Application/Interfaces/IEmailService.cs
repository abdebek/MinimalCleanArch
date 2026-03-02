#if (UseAuth)
namespace MCA.Application.Interfaces;

public interface IEmailService
{
    Task SendPasswordResetAsync(string email, string token, string userId);
    Task SendEmailConfirmationAsync(string email, string token, string userId);
}

public interface IEmailSender
{
    Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default);
}

public class EmailMessage
{
    public string To { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string HtmlBody { get; set; } = string.Empty;
    public string? TextBody { get; set; }
}
#endif
