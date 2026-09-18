namespace MinimalCleanArch.Email;

/// <summary>
/// Framework-neutral email port. Host adapters send mail; application code must not take ASP.NET types here.
/// </summary>
public interface IEmailSender
{
    Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default);
}

/// <summary>
/// A templated outbound message: recipient, subject, html/text bodies, optional correlation id.
/// </summary>
public sealed class EmailMessage
{
    public string To { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string HtmlBody { get; set; } = string.Empty;
    public string? TextBody { get; set; }
    public string? CorrelationId { get; set; }
}
