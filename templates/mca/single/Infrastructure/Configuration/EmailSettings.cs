namespace MCA.Infrastructure.Configuration;

public class EmailSettings
{
    public const string SectionName = "EmailSettings";

    public string SmtpServer { get; set; } = "localhost";
    public int Port { get; set; } = 25;
    public string SenderEmail { get; set; } = "no-reply@example.com";
    public string SenderName { get; set; } = "MCA";
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public bool EnableSsl { get; set; } = false;
    public bool UseDefaultCredentials { get; set; } = false;
    public int TimeoutSeconds { get; set; } = 30;
    public string AppBaseUrl { get; set; } = "https://localhost:5001";
    public string AppName { get; set; } = "MCA";
}
