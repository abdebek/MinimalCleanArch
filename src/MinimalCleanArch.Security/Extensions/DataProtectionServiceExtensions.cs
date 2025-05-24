using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.DependencyInjection;
using MinimalCleanArch.Security.Encryption;
using MinimalCleanArch.Security.Configuration;
using Azure.Identity;
using Microsoft.Extensions.Logging;

namespace MinimalCleanArch.Security.Extensions;

/// <summary>
/// Extension methods for adding Data Protection encryption services
/// Focuses on persistent storage without Redis dependency
/// </summary>
public static class DataProtectionServiceExtensions
{
    /// <summary>
    /// Adds Data Protection encryption with file system persistence
    /// Ideal for: Single-server production deployments, Docker containers with volumes
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="keyDirectoryPath">Directory path for storing encryption keys (must be persistent)</param>
    /// <param name="applicationName">Application name for key isolation (defaults to entry assembly name)</param>
    /// <param name="keyLifetimeDays">Key lifetime in days (default: 90)</param>
    /// <returns>The service collection</returns>
    public static IServiceCollection AddDataProtectionEncryption(
        this IServiceCollection services,
        string keyDirectoryPath,
        string? applicationName = null,
        int keyLifetimeDays = 90)
    {
        applicationName ??= GetDefaultApplicationName();
        var keyDirectory = new DirectoryInfo(keyDirectoryPath);

        var options = new DataProtectionEncryptionOptions
        {
            ApplicationName = applicationName,
            Purpose = $"{applicationName}.EntityEncryption"
        };

        services.AddSingleton(options);

        // Ensure directory exists
        if (!keyDirectory.Exists)
        {
            keyDirectory.Create();
        }

        // Configure Data Protection with file system persistence
        services.AddDataProtection()
            .SetApplicationName(options.ApplicationName)
            .PersistKeysToFileSystem(keyDirectory)
            .SetDefaultKeyLifetime(TimeSpan.FromDays(keyLifetimeDays));

        services.AddSingleton<IEncryptionService, DataProtectionEncryptionService>();

        return services;
    }

    /// <summary>
    /// Adds Data Protection encryption with Azure Key Vault for key protection
    /// Ideal for: Enterprise cloud deployments with centralized key management
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="keyVaultUri">Azure Key Vault URI (e.g., https://vault.vault.azure.net/)</param>
    /// <param name="keyName">Name of the key in Azure Key Vault</param>
    /// <param name="applicationName">Application name for key isolation</param>
    /// <param name="keyLifetimeDays">Key lifetime in days (default: 90)</param>
    /// <param name="useDefaultAzureCredential">Whether to use DefaultAzureCredential (recommended for production)</param>
    /// <returns>The service collection</returns>
    public static IServiceCollection AddDataProtectionEncryptionWithAzureKeyVault(
        this IServiceCollection services,
        string keyVaultUri,
        string keyName,
        string? applicationName = null,
        int keyLifetimeDays = 90,
        bool useDefaultAzureCredential = true)
    {
        applicationName ??= GetDefaultApplicationName();

        var options = new DataProtectionEncryptionOptions
        {
            ApplicationName = applicationName,
            Purpose = $"{applicationName}.EntityEncryption"
        };

        services.AddSingleton(options);

        // Configure Data Protection with Azure Key Vault
        var dataProtectionBuilder = services.AddDataProtection()
            .SetApplicationName(options.ApplicationName)
            .SetDefaultKeyLifetime(TimeSpan.FromDays(keyLifetimeDays));

        if (useDefaultAzureCredential)
        {
            // Use DefaultAzureCredential (works with Managed Identity, Azure CLI, etc.)
            // keyIdentifier should be the full key URI: https://vault.vault.azure.net/keys/keyname
            var keyIdentifier = new Uri($"{keyVaultUri.TrimEnd('/')}/keys/{keyName}");
            dataProtectionBuilder.ProtectKeysWithAzureKeyVault(
                keyIdentifier, 
                new DefaultAzureCredential());
        }
        else
        {
            // Basic setup without explicit credentials (requires environment setup)
            var keyIdentifier = new Uri($"{keyVaultUri.TrimEnd('/')}/keys/{keyName}");
            dataProtectionBuilder.ProtectKeysWithAzureKeyVault(keyIdentifier, new DefaultAzureCredential());
        }

        services.AddSingleton<IEncryptionService, DataProtectionEncryptionService>();

        return services;
    }

    /// <summary>
    /// Adds Data Protection encryption with Azure Key Vault and Blob Storage
    /// Ideal for: Multi-region deployments with shared key storage
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="keyVaultUri">Azure Key Vault URI</param>
    /// <param name="keyName">Name of the key in Azure Key Vault</param>
    /// <param name="blobStorageConnectionString">Azure Blob Storage connection string</param>
    /// <param name="containerName">Blob container name for storing keys</param>
    /// <param name="applicationName">Application name for key isolation</param>
    /// <param name="keyLifetimeDays">Key lifetime in days (default: 90)</param>
    /// <returns>The service collection</returns>
    public static IServiceCollection AddDataProtectionEncryptionWithAzureStorage(
        this IServiceCollection services,
        string keyVaultUri,
        string keyName,
        string blobStorageConnectionString,
        string containerName = "dataprotection-keys",
        string? applicationName = null,
        int keyLifetimeDays = 90)
    {
        applicationName ??= GetDefaultApplicationName();

        var options = new DataProtectionEncryptionOptions
        {
            ApplicationName = applicationName,
            Purpose = $"{applicationName}.EntityEncryption"
        };

        services.AddSingleton(options);

        // Configure Data Protection with Azure Key Vault and Blob Storage
        services.AddDataProtection()
            .SetApplicationName(options.ApplicationName)
           // .PersistKeysToAzureBlobStorage(blobStorageConnectionString, containerName, "keys.xml") //TPDP: add support for Azure blob storage
            .ProtectKeysWithAzureKeyVault(
                new Uri($"{keyVaultUri.TrimEnd('/')}/keys/{keyName}"), 
                new DefaultAzureCredential())
            .SetDefaultKeyLifetime(TimeSpan.FromDays(keyLifetimeDays));

        services.AddSingleton<IEncryptionService, DataProtectionEncryptionService>();

        return services;
    }

    /// <summary>
    /// Adds Data Protection encryption with development-friendly settings
    /// Keys are stored in a local directory with reasonable defaults
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="applicationName">Application name for key isolation</param>
    /// <returns>The service collection</returns>
    public static IServiceCollection AddDataProtectionEncryptionForDevelopment(
        this IServiceCollection services,
        string? applicationName = null)
    {
        applicationName ??= GetDefaultApplicationName();
        
        // Store keys in a local directory for development
        var keyPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), 
            "MinimalCleanArch", applicationName, "DataProtection-Keys");

        return services.AddDataProtectionEncryption(keyPath, $"{applicationName}.Development", 30); // 30-day keys for dev
    }

    /// <summary>
    /// Adds hybrid encryption with Data Protection as primary and AES as fallback
    /// Enables gradual migration from AES to Data Protection
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="aesEncryptionKey">Legacy AES key for fallback decryption</param>
    /// <param name="keyDirectoryPath">Directory for Data Protection keys</param>
    /// <param name="applicationName">Application name for key isolation</param>
    /// <returns>The service collection</returns>
    public static IServiceCollection AddHybridEncryptionWithDataProtection(
        this IServiceCollection services,
        string aesEncryptionKey,
        string keyDirectoryPath,
        string? applicationName = null)
    {
        applicationName ??= GetDefaultApplicationName();

        // Add AES encryption for fallback
        var aesOptions = new EncryptionOptions 
        { 
            Key = aesEncryptionKey,
            ValidateKeyStrength = false // Skip validation for legacy keys
        };
        services.AddSingleton(aesOptions);
        services.AddSingleton<AesEncryptionService>();

        // Add Data Protection encryption
        var dpOptions = new DataProtectionEncryptionOptions
        {
            ApplicationName = applicationName,
            Purpose = $"{applicationName}.EntityEncryption"
        };
        services.AddSingleton(dpOptions);

        var keyDirectory = new DirectoryInfo(keyDirectoryPath);
        if (!keyDirectory.Exists)
        {
            keyDirectory.Create();
        }

        services.AddDataProtection()
            .SetApplicationName(dpOptions.ApplicationName)
            .PersistKeysToFileSystem(keyDirectory)
            .SetDefaultKeyLifetime(TimeSpan.FromDays(90));

        services.AddSingleton<DataProtectionEncryptionService>();

        // Register hybrid service as the primary IEncryptionService
        services.AddSingleton<IEncryptionService, HybridEncryptionService>();

        return services;
    }

    /// <summary>
    /// Gets the default application name from the entry assembly
    /// </summary>
    private static string GetDefaultApplicationName()
    {
        return System.Reflection.Assembly.GetEntryAssembly()?.GetName().Name ?? "MinimalCleanArch";
    }
}

/// <summary>
/// Hybrid encryption service that uses Data Protection as primary and AES as fallback
/// Enables zero-downtime migration from AES to Data Protection
/// </summary>
public class HybridEncryptionService : IEncryptionService
{
    private readonly DataProtectionEncryptionService _primaryService;
    private readonly AesEncryptionService _fallbackService;
    private readonly ILogger<HybridEncryptionService>? _logger;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="HybridEncryptionService"/> class
    /// </summary>
    public HybridEncryptionService(
        DataProtectionEncryptionService primaryService,
        AesEncryptionService fallbackService,
        ILogger<HybridEncryptionService>? logger = null)
    {
        _primaryService = primaryService ?? throw new ArgumentNullException(nameof(primaryService));
        _fallbackService = fallbackService ?? throw new ArgumentNullException(nameof(fallbackService));
        _logger = logger;
    }

    /// <summary>
    /// Encrypts using Data Protection (all new data uses Data Protection)
    /// </summary>
    public string Encrypt(string plainText)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(HybridEncryptionService));

        // Always use Data Protection for new encryptions
        return _primaryService.Encrypt(plainText);
    }

    /// <summary>
    /// Attempts decryption with Data Protection first, then AES fallback
    /// Allows reading both new (Data Protection) and legacy (AES) encrypted data
    /// </summary>
    public string Decrypt(string cipherText)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(HybridEncryptionService));

        try
        {
            // Try Data Protection first (for new data)
            return _primaryService.Decrypt(cipherText);
        }
        catch (Exception)
        {
            _logger?.LogDebug("Data Protection decryption failed, attempting AES fallback for legacy data");
            try
            {
                // Fallback to AES (for legacy data)
                var result = _fallbackService.Decrypt(cipherText);
                _logger?.LogInformation("Successfully decrypted legacy AES data - consider re-encrypting during next update");
                return result;
            }
            catch (Exception fallbackEx)
            {
                _logger?.LogError(fallbackEx, "Both Data Protection and AES decryption failed");
                throw new System.Security.Cryptography.CryptographicException(
                    "Unable to decrypt data with either Data Protection or AES encryption", fallbackEx);
            }
        }
    }

    /// <summary>
    /// Disposes the hybrid encryption service
    /// </summary>
    public void Dispose()
    {
        if (!_disposed)
        {
            _primaryService?.Dispose();
            _fallbackService?.Dispose();
            _disposed = true;
        }
    }
}