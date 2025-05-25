using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Microsoft.Extensions.Logging;
using MinimalCleanArch.Security.Encryption;
using System.Security.Cryptography;

namespace MinimalCleanArch.Security.EntityEncryption;

/// <summary>
/// Value converter for encrypted properties with enhanced security and proper null handling
/// </summary>
public class EncryptedConverter : ValueConverter<string?, string?>
{
    private const string NULL_MARKER = "<<NULL>>";
    private const string EMPTY_MARKER = "<<EMPTY>>";
    
    private readonly ILogger<EncryptedConverter>? _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="EncryptedConverter"/> class
    /// </summary>
    /// <param name="encryptionService">The encryption service</param>
    /// <param name="logger">Optional logger for security events</param>
    public EncryptedConverter(IEncryptionService encryptionService, ILogger<EncryptedConverter>? logger = null)
        : base(
            v => EncryptValue(v, encryptionService, logger),
            v => DecryptValue(v, encryptionService, logger),
            null)
    {
        ArgumentNullException.ThrowIfNull(encryptionService);
        _logger = logger;
    }

    /// <summary>
    /// Encrypts a value with proper null and empty string handling
    /// </summary>
    /// <param name="value">The value to encrypt</param>
    /// <param name="encryptionService">The encryption service</param>
    /// <param name="logger">Optional logger</param>
    /// <returns>The encrypted value or special marker</returns>
    private static string? EncryptValue(string? value, IEncryptionService encryptionService, ILogger<EncryptedConverter>? logger)
    {
        try
        {
            // Handle special cases with explicit markers
            if (value == null)
            {
                return NULL_MARKER;
            }

            if (value.Length == 0)
            {
                return EMPTY_MARKER;
            }

            // Encrypt the actual value
            var encrypted = encryptionService.Encrypt(value);
            
            if (string.IsNullOrEmpty(encrypted))
            {
                logger?.LogError("Encryption service returned null or empty result for non-null input");
                throw new CryptographicException("Encryption operation failed - service returned invalid result");
            }

            return encrypted;
        }
        catch (Exception ex) when (!(ex is CryptographicException))
        {
            logger?.LogError(ex, "Unexpected error during encryption operation");
            throw new CryptographicException("Encryption operation failed due to unexpected error", ex);
        }
    }

    /// <summary>
    /// Decrypts a value with proper null and empty string handling
    /// </summary>
    /// <param name="value">The value to decrypt</param>
    /// <param name="encryptionService">The encryption service</param>
    /// <param name="logger">Optional logger</param>
    /// <returns>The decrypted value</returns>
    private static string? DecryptValue(string? value, IEncryptionService encryptionService, ILogger<EncryptedConverter>? logger)
    {
        try
        {
            // Handle database null
            if (value == null)
            {
                logger?.LogWarning("Attempted to decrypt null value from database - this may indicate data corruption");
                return null;
            }

            // Handle special markers
            if (value == NULL_MARKER)
            {
                return null;
            }

            if (value == EMPTY_MARKER)
            {
                return string.Empty;
            }

            // Handle edge case - empty string in database (shouldn't happen with proper encryption)
            if (value.Length == 0)
            {
                logger?.LogWarning("Found empty string in encrypted column - this may indicate data corruption");
                return string.Empty;
            }

            // Decrypt the actual value
            var decrypted = encryptionService.Decrypt(value);
            
            // The encryption service should handle its own null checking,
            // but we verify the result for security
            if (decrypted == null)
            {
                logger?.LogError("Decryption service returned null for non-null encrypted input - possible data corruption or key mismatch");
                throw new CryptographicException("Decryption operation failed - service returned null result");
            }

            return decrypted;
        }
        catch (Exception ex) when (!(ex is CryptographicException))
        {
            logger?.LogError(ex, "Unexpected error during decryption operation");
            throw new CryptographicException("Decryption operation failed due to unexpected error", ex);
        }
    }
}

/// <summary>
/// Value converter for encrypted properties that ensures non-null results
/// Use this when the property should never be null
/// </summary>
public class EncryptedConverterNonNull : ValueConverter<string, string>
{
    private readonly ILogger<EncryptedConverterNonNull>? _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="EncryptedConverterNonNull"/> class
    /// </summary>
    /// <param name="encryptionService">The encryption service</param>
    /// <param name="logger">Optional logger for security events</param>
    public EncryptedConverterNonNull(IEncryptionService encryptionService, ILogger<EncryptedConverterNonNull>? logger = null)
        : base(
            v => EncryptValueNonNull(v, encryptionService, logger),
            v => DecryptValueNonNull(v, encryptionService, logger),
            null)
    {
        ArgumentNullException.ThrowIfNull(encryptionService);
        _logger = logger;
    }

    /// <summary>
    /// Encrypts a non-null value
    /// </summary>
    private static string EncryptValueNonNull(string value, IEncryptionService encryptionService, ILogger<EncryptedConverterNonNull>? logger)
    {
        if (value == null)
        {
            logger?.LogError("Attempted to encrypt null value with non-null converter");
            throw new ArgumentNullException(nameof(value), "Cannot encrypt null value with non-null converter");
        }

        try
        {
            var encrypted = encryptionService.Encrypt(value);
            
            if (string.IsNullOrEmpty(encrypted))
            {
                logger?.LogError("Encryption service returned null or empty result for non-null input");
                throw new CryptographicException("Encryption operation failed - service returned invalid result");
            }

            return encrypted;
        }
        catch (Exception ex) when (!(ex is CryptographicException or ArgumentNullException))
        {
            logger?.LogError(ex, "Unexpected error during encryption operation");
            throw new CryptographicException("Encryption operation failed due to unexpected error", ex);
        }
    }

    /// <summary>
    /// Decrypts a value ensuring non-null result
    /// </summary>
    private static string DecryptValueNonNull(string value, IEncryptionService encryptionService, ILogger<EncryptedConverterNonNull>? logger)
    {
        if (value == null)
        {
            logger?.LogError("Attempted to decrypt null value with non-null converter - possible data corruption");
            throw new CryptographicException("Cannot decrypt null value - possible data corruption");
        }

        try
        {
            var decrypted = encryptionService.Decrypt(value);
            
            if (decrypted == null)
            {
                logger?.LogError("Decryption service returned null for non-null encrypted input");
                throw new CryptographicException("Decryption operation failed - service returned null result");
            }

            return decrypted;
        }
        catch (Exception ex) when (!(ex is CryptographicException))
        {
            logger?.LogError(ex, "Unexpected error during decryption operation");
            throw new CryptographicException("Decryption operation failed due to unexpected error", ex);
        }
    }
}