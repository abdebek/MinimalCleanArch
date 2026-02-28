namespace MCA.Infrastructure.Configuration;

public class EmailSettings
{
    public const string SectionName = "EmailSettings";

    public string Provider { get; set; } = EmailProviders.Smtp;
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
    public ApiEmailSettings Api { get; set; } = new();
}

public static class EmailProviders
{
    public const string Smtp = "Smtp";
    public const string Api = "Api";
}

public class ApiEmailSettings
{
    public string Endpoint { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string ApiKeyHeaderName { get; set; } = "Authorization";
    public string ApiKeyPrefix { get; set; } = "Bearer";
    public Dictionary<string, string> Headers { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}
