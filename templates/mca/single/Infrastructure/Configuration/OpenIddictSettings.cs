namespace MCA.Infrastructure.Configuration;

public class OpenIddictSettings
{
    public const string SectionName = "OpenIddict";

    public CertificateSettings SigningCertificate { get; set; } = new();
    public CertificateSettings EncryptionCertificate { get; set; } = new();
    public Dictionary<string, ClientSettings> Clients { get; set; } = new();
    public TokenLifetimeSettings TokenLifetimes { get; set; } = new();
    public string[] AllowedOrigins { get; set; } = Array.Empty<string>();
}

public enum CertificateSource { None, File, Store, Base64 }

public class CertificateSettings
{
    public CertificateSource Source { get; set; } = CertificateSource.None;
    public string? Path { get; set; }
    public string? Password { get; set; }
    public string? Thumbprint { get; set; }
    public string? StoreName { get; set; }
    public string? StoreLocation { get; set; }
    public string? Base64Encoded { get; set; }
}

public class ClientSettings
{
    public string Secret { get; set; } = string.Empty;
    public string[] RedirectUris { get; set; } = Array.Empty<string>();
    public string[] PostLogoutRedirectUris { get; set; } = Array.Empty<string>();
}

public class TokenLifetimeSettings
{
    public TimeSpan AccessToken { get; set; } = TimeSpan.FromHours(1);
    public TimeSpan RefreshToken { get; set; } = TimeSpan.FromDays(14);
    public TimeSpan AuthorizationCode { get; set; } = TimeSpan.FromMinutes(5);
    public TimeSpan IdentityToken { get; set; } = TimeSpan.FromHours(1);
}
