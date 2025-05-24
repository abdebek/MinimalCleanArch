namespace MinimalCleanArch.Security.Encryption;

/// <summary>
/// Configuration options for Data Protection encryption
/// </summary>
public class DataProtectionEncryptionOptions
{
    /// <summary>
    /// Gets or sets the purpose string for data protection
    /// This should be unique for your application and encryption use case
    /// </summary>
    public string Purpose { get; set; } = "MinimalCleanArch.EntityEncryption";

    /// <summary>
    /// Gets or sets the key lifetime for time-limited protection
    /// If null, keys will not expire
    /// </summary>
    public TimeSpan? KeyLifetime { get; set; }

    /// <summary>
    /// Gets or sets whether to use time-limited data protection
    /// </summary>
    public bool UseTimeLimitedProtection { get; set; }

    /// <summary>
    /// Gets or sets the application name for key isolation
    /// </summary>
    public string ApplicationName { get; set; } = "MinimalCleanArch";

    /// <summary>
    /// Creates options for production use with recommended settings
    /// </summary>
    /// <param name="applicationName">The application name for key isolation</param>
    /// <returns>Production-ready data protection options</returns>
    public static DataProtectionEncryptionOptions ForProduction(string applicationName)
    {
        return new DataProtectionEncryptionOptions
        {
            Purpose = $"{applicationName}.EntityEncryption",
            ApplicationName = applicationName,
            UseTimeLimitedProtection = false, // Generally not needed for entity encryption
            KeyLifetime = null // Let Data Protection handle key rotation
        };
    }

    /// <summary>
    /// Creates options for development/testing
    /// </summary>
    /// <returns>Development-friendly data protection options</returns>
    public static DataProtectionEncryptionOptions ForDevelopment()
    {
        return new DataProtectionEncryptionOptions
        {
            Purpose = "MinimalCleanArch.Development.EntityEncryption",
            ApplicationName = "MinimalCleanArch.Development",
            UseTimeLimitedProtection = false
        };
    }
}
/// <summary>
/// Attribute for marking a property as encrypted
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public class EncryptedAttribute : Attribute
{
}

