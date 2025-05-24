using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using Microsoft.AspNetCore.DataProtection;

namespace MinimalCleanArch.Security.Encryption;

/// <summary>
/// Implementation of <see cref="IEncryptionService"/> using Microsoft Data Protection API
/// Provides enterprise-grade key management, rotation, and secure storage
/// </summary>
public class DataProtectionEncryptionService : IEncryptionService
{
    private readonly IDataProtector _dataProtector;
    private readonly ILogger<DataProtectionEncryptionService>? _logger;
    private readonly DataProtectionEncryptionOptions _options;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="DataProtectionEncryptionService"/> class
    /// </summary>
    /// <param name="dataProtectionProvider">The data protection provider</param>
    /// <param name="options">The data protection encryption options</param>
    /// <param name="logger">Optional logger for security events</param>
    public DataProtectionEncryptionService(
        IDataProtectionProvider dataProtectionProvider,
        DataProtectionEncryptionOptions options,
        ILogger<DataProtectionEncryptionService>? logger = null)
    {
        ArgumentNullException.ThrowIfNull(dataProtectionProvider);
        ArgumentNullException.ThrowIfNull(options);

        _options = options;
        _logger = logger;

        try
        {
            // Create a data protector with a specific purpose string
            // This ensures data encrypted for one purpose cannot be decrypted by another
            _dataProtector = dataProtectionProvider.CreateProtector(_options.Purpose);
            
            // If lifetime is specified, create a time-limited protector
            if (_options.KeyLifetime.HasValue)
            {
                _dataProtector = _dataProtector.ToTimeLimitedDataProtector();
            }

            _logger?.LogInformation("Data Protection encryption service initialized with purpose: {Purpose}", _options.Purpose);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to initialize Data Protection encryption service");
            throw new CryptographicException("Failed to initialize Data Protection encryption service", ex);
        }
    }

    /// <summary>
    /// Encrypts the specified plaintext using Microsoft Data Protection
    /// </summary>
    /// <param name="plainText">The plaintext to encrypt</param>
    /// <returns>The encrypted ciphertext</returns>
    public string Encrypt(string plainText)
    {
        ThrowIfDisposed();

        if (plainText == null)
            throw new ArgumentNullException(nameof(plainText));

        try
        {
            string result;

            if (_options.KeyLifetime.HasValue && _dataProtector is ITimeLimitedDataProtector timeLimitedProtector)
            {
                // Use time-limited protection
                var expiration = DateTimeOffset.UtcNow.Add(_options.KeyLifetime.Value);
                result = timeLimitedProtector.Protect(plainText, expiration);
                
                _logger?.LogDebug("Successfully encrypted data with expiration: {Expiration}", expiration);
            }
            else
            {
                // Use standard protection
                result = _dataProtector.Protect(plainText);
                
                _logger?.LogDebug("Successfully encrypted data (length: {PlainLength} -> {EncryptedLength})", 
                    plainText.Length, result.Length);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Data Protection encryption operation failed");
            throw new CryptographicException("Data Protection encryption operation failed", ex);
        }
    }

    /// <summary>
    /// Decrypts the specified ciphertext using Microsoft Data Protection
    /// </summary>
    /// <param name="cipherText">The ciphertext to decrypt</param>
    /// <returns>The decrypted plaintext</returns>
    public string Decrypt(string cipherText)
    {
        ThrowIfDisposed();

        if (cipherText == null)
            throw new ArgumentNullException(nameof(cipherText));

        if (cipherText.Length == 0)
        {
            _logger?.LogWarning("Attempted to decrypt empty string - this may indicate data corruption");
            throw new CryptographicException("Cannot decrypt empty string");
        }

        try
        {
            string result;

            if (_options.KeyLifetime.HasValue && _dataProtector is ITimeLimitedDataProtector timeLimitedProtector)
            {
                // Use time-limited protection with automatic expiration handling
                result = timeLimitedProtector.Unprotect(cipherText);
            }
            else
            {
                // Use standard protection
                result = _dataProtector.Unprotect(cipherText);
            }

            _logger?.LogDebug("Successfully decrypted data (length: {EncryptedLength} -> {PlainLength})", 
                cipherText.Length, result.Length);

            return result;
        }
        catch (CryptographicException ex)
        {
            _logger?.LogError(ex, "Data Protection decryption failed - possible key rotation or data corruption");
            throw; // Re-throw CryptographicException as-is
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Unexpected error during Data Protection decryption");
            throw new CryptographicException("Data Protection decryption operation failed due to unexpected error", ex);
        }
    }

    /// <summary>
    /// Attempts to decrypt data, handling key rotation gracefully
    /// </summary>
    /// <param name="cipherText">The ciphertext to decrypt</param>
    /// <param name="fallbackDecryptor">Optional fallback decryption service for data encrypted with old keys</param>
    /// <returns>The decrypted plaintext</returns>
    public string DecryptWithFallback(string cipherText, IEncryptionService? fallbackDecryptor = null)
    {
        try
        {
            return Decrypt(cipherText);
        }
        catch (CryptographicException) when (fallbackDecryptor != null)
        {
            _logger?.LogInformation("Primary decryption failed, attempting fallback decryption");
            try
            {
                var result = fallbackDecryptor.Decrypt(cipherText);
                _logger?.LogInformation("Fallback decryption successful - consider re-encrypting with current key");
                return result;
            }
            catch (Exception fallbackEx)
            {
                _logger?.LogError(fallbackEx, "Fallback decryption also failed");
                throw; // Re-throw the fallback exception
            }
        }
    }

    /// <summary>
    /// Checks if the service can decrypt the given ciphertext
    /// </summary>
    /// <param name="cipherText">The ciphertext to test</param>
    /// <returns>True if decryption is possible, false otherwise</returns>
    public bool CanDecrypt(string cipherText)
    {
        if (string.IsNullOrEmpty(cipherText))
            return false;

        try
        {
            _dataProtector.Unprotect(cipherText);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Throws if the service has been disposed
    /// </summary>
    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(DataProtectionEncryptionService));
        }
    }

    /// <summary>
    /// Disposes the encryption service
    /// </summary>
    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
            _logger?.LogInformation("Data Protection encryption service disposed");
        }
    }
}

