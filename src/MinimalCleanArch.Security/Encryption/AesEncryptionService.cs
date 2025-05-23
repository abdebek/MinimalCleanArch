using System.Security.Cryptography;
using System.Text;
using MinimalCleanArch.Security.Configuration;
using Microsoft.Extensions.Logging;

namespace MinimalCleanArch.Security.Encryption;

/// <summary>
/// Implementation of <see cref="IEncryptionService"/> using AES encryption with enhanced security
/// </summary>
public class AesEncryptionService : IEncryptionService, IDisposable
{
    private readonly byte[] _key;
    private readonly ILogger<AesEncryptionService>? _logger;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="AesEncryptionService"/> class
    /// </summary>
    /// <param name="encryptionOptions">The encryption options</param>
    /// <param name="logger">Optional logger for security events</param>
    public AesEncryptionService(EncryptionOptions encryptionOptions, ILogger<AesEncryptionService>? logger = null)
    {
        if (encryptionOptions == null)
            throw new ArgumentNullException(nameof(encryptionOptions));

        if (string.IsNullOrWhiteSpace(encryptionOptions.Key))
            throw new ArgumentException("Encryption key must be provided and cannot be empty", nameof(encryptionOptions.Key));

        // Validate key strength
        if (encryptionOptions.Key.Length < 32) // Minimum recommended length
        {
            logger?.LogWarning("Encryption key length ({Length}) is below recommended minimum of 32 characters", encryptionOptions.Key.Length);
        }

        _logger = logger;

        try
        {
            // Create a strong key from the key string using SHA-256
            using var sha256 = SHA256.Create();
            _key = sha256.ComputeHash(Encoding.UTF8.GetBytes(encryptionOptions.Key));
            
            _logger?.LogInformation("AES encryption service initialized successfully");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to initialize AES encryption service");
            throw new CryptographicException("Failed to initialize encryption service", ex);
        }
    }

    /// <summary>
    /// Encrypts the specified plaintext
    /// </summary>
    /// <param name="plainText">The plaintext to encrypt</param>
    /// <returns>The encrypted ciphertext</returns>
    public string Encrypt(string plainText)
    {
        ThrowIfDisposed();

        if (plainText == null)
            throw new ArgumentNullException(nameof(plainText));

        // Handle empty string explicitly
        if (plainText.Length == 0)
        {
            _logger?.LogDebug("Encrypting empty string");
        }

        try
        {
            using var aes = Aes.Create();
            aes.Key = _key;
            aes.Mode = CipherMode.CBC; // Explicitly set secure mode
            aes.Padding = PaddingMode.PKCS7; // Explicitly set padding
            
            // Generate a new IV for each encryption (crucial for security)
            aes.GenerateIV();
            var iv = aes.IV;

            using var encryptor = aes.CreateEncryptor();
            using var msEncrypt = new MemoryStream();
            
            // Write the IV to the beginning of the output stream
            msEncrypt.Write(iv, 0, iv.Length);
            
            using (var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
            using (var swEncrypt = new StreamWriter(csEncrypt, Encoding.UTF8))
            {
                swEncrypt.Write(plainText);
            }

            var result = Convert.ToBase64String(msEncrypt.ToArray());
            
            _logger?.LogDebug("Successfully encrypted data (length: {PlainLength} -> {EncryptedLength})", 
                plainText.Length, result.Length);
            
            return result;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Encryption operation failed");
            throw new CryptographicException("Encryption operation failed", ex);
        }
    }

    /// <summary>
    /// Decrypts the specified ciphertext
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
            var fullCipher = Convert.FromBase64String(cipherText);
            
            // Validate minimum length (IV + at least one block)
            const int minLength = 16 + 16; // IV + minimum encrypted content
            if (fullCipher.Length < minLength)
            {
                _logger?.LogError("Encrypted data is too short (length: {Length}, minimum: {MinLength})", 
                    fullCipher.Length, minLength);
                throw new CryptographicException("Invalid encrypted data - insufficient length");
            }
            
            // Extract IV from the beginning of the ciphertext
            var iv = new byte[16]; // AES block size is always 16 bytes
            Array.Copy(fullCipher, 0, iv, 0, iv.Length);
            
            // Extract the actual ciphertext after the IV
            var cipher = new byte[fullCipher.Length - iv.Length];
            Array.Copy(fullCipher, iv.Length, cipher, 0, cipher.Length);

            using var aes = Aes.Create();
            aes.Key = _key;
            aes.IV = iv;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            using var decryptor = aes.CreateDecryptor();
            using var msDecrypt = new MemoryStream(cipher);
            using var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read);
            using var srDecrypt = new StreamReader(csDecrypt, Encoding.UTF8);
            
            var result = srDecrypt.ReadToEnd();
            
            _logger?.LogDebug("Successfully decrypted data (length: {EncryptedLength} -> {PlainLength})", 
                cipherText.Length, result.Length);
            
            return result;
        }
        catch (Exception ex) when (ex is FormatException or ArgumentException)
        {
            _logger?.LogError(ex, "Invalid encrypted data format");
            throw new CryptographicException("Invalid encrypted data format", ex);
        }
        catch (Exception ex) when (ex is CryptographicException)
        {
            _logger?.LogError(ex, "Decryption operation failed - possible key mismatch or data corruption");
            throw; // Re-throw CryptographicException as-is
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Unexpected error during decryption");
            throw new CryptographicException("Decryption operation failed due to unexpected error", ex);
        }
    }

    /// <summary>
    /// Throws if the service has been disposed
    /// </summary>
    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(AesEncryptionService));
        }
    }

    /// <summary>
    /// Disposes the encryption service and clears sensitive data
    /// </summary>
    public void Dispose()
    {
        if (!_disposed)
        {
            // Clear the key from memory for security
            if (_key != null)
            {
                Array.Clear(_key, 0, _key.Length);
            }
            
            _disposed = true;
            _logger?.LogInformation("AES encryption service disposed");
        }
    }
}