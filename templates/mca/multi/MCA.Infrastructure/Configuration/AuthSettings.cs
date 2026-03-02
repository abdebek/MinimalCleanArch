namespace MCA.Infrastructure.Configuration;

public class AuthSettings
{
    public const string SectionName = "Auth";

    public string ClientId { get; set; } = "mca-api-client";
    public string ClientSecret { get; set; } = "mca-default-secret-change-me";
    public TokenLifetimeSettings TokenLifetimes { get; set; } = new();
}

public class TokenLifetimeSettings
{
    public TimeSpan AccessToken { get; set; } = TimeSpan.FromHours(1);
    public TimeSpan RefreshToken { get; set; } = TimeSpan.FromDays(14);
    public TimeSpan AuthorizationCode { get; set; } = TimeSpan.FromMinutes(5);
}
