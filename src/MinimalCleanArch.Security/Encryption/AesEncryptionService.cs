using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;
using MinimalCleanArch.Security.Configuration;

namespace MinimalCleanArch.Security.Encryption;

/// <summary>
/// Implementation of <see cref="IEncryptionService"/> using authenticated AES-256-GCM with per-message HKDF-SHA256 key derivation.
/// Supports transparent decryption of legacy v1 AES-CBC ciphertexts for seamless migration.
/// </summary>
public class AesEncryptionService : IEncryptionService, IDisposable
{
    private static readonly byte[] V2MagicHeader = "MCA2"u8.ToArray();
    private static readonly byte[] HkdfInfo = "MinimalCleanArch.AesGcm.v2"u8.ToArray();

    private const int SaltLength = 16;
    private const int NonceLength = 12;
    private const int TagLength = 16;
    private const int KeyLength = 32;
    private const int V2HeaderLength = 4 + SaltLength + NonceLength + TagLength;

    private readonly byte[] _ikm;
    private readonly byte[] _legacyCbcKey;
    private readonly ILogger<AesEncryptionService>? _logger;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="AesEncryptionService"/> class
    /// </summary>
    /// <param name="encryptionOptions">The encryption options</param>
    /// <param name="logger">Optional logger for security events</param>
    public AesEncryptionService(EncryptionOptions encryptionOptions, ILogger<AesEncryptionService>? logger = null)
    {
        ArgumentNullException.ThrowIfNull(encryptionOptions);

        if (string.IsNullOrWhiteSpace(encryptionOptions.Key))
            throw new ArgumentException("Encryption key must be provided and cannot be empty", nameof(encryptionOptions));

        // Validate key strength
        if (encryptionOptions.Key.Length < 32)
        {
            logger?.LogWarning("Encryption key length ({Length}) is below recommended minimum of 32 characters", encryptionOptions.Key.Length);
        }

        _logger = logger;

        try
        {
            _ikm = Encoding.UTF8.GetBytes(encryptionOptions.Key);
            _legacyCbcKey = SHA256.HashData(_ikm);

            _logger?.LogInformation("AES-GCM encryption service initialized successfully");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to initialize AES encryption service");
            throw new CryptographicException("Failed to initialize encryption service", ex);
        }
    }

    /// <summary>
    /// Encrypts the specified plaintext using AES-256-GCM with a random salt, HKDF-derived key, and random 96-bit nonce.
    /// </summary>
    /// <param name="plainText">The plaintext to encrypt</param>
    /// <returns>The Base64-encoded versioned ciphertext</returns>
    public string Encrypt(string plainText)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(plainText);

        if (plainText.Length == 0)
        {
            _logger?.LogDebug("Encrypting empty string");
        }

        byte[]? derivedKey = null;
        try
        {
            var plainBytes = Encoding.UTF8.GetBytes(plainText);
            var payload = new byte[V2HeaderLength + plainBytes.Length];

            V2MagicHeader.CopyTo(payload, 0);

            var salt = payload.AsSpan(4, SaltLength);
            var nonce = payload.AsSpan(4 + SaltLength, NonceLength);
            var tag = payload.AsSpan(4 + SaltLength + NonceLength, TagLength);
            var cipherBytes = payload.AsSpan(V2HeaderLength);

            RandomNumberGenerator.Fill(salt);
            RandomNumberGenerator.Fill(nonce);

            derivedKey = new byte[KeyLength];
            HKDF.DeriveKey(HashAlgorithmName.SHA256, _ikm, derivedKey, salt, HkdfInfo);

            using var aesGcm = new AesGcm(derivedKey, TagLength);
            aesGcm.Encrypt(nonce, plainBytes, cipherBytes, tag, V2MagicHeader);

            var result = Convert.ToBase64String(payload);

            _logger?.LogDebug("Successfully encrypted data (length: {PlainLength} -> {EncryptedLength})",
                plainText.Length, result.Length);

            return result;
        }
        catch (Exception ex) when (ex is not CryptographicException)
        {
            _logger?.LogError(ex, "Encryption operation failed");
            throw new CryptographicException("Encryption operation failed", ex);
        }
        finally
        {
            if (derivedKey is not null)
            {
                CryptographicOperations.ZeroMemory(derivedKey);
            }
        }
    }

    /// <summary>
    /// Decrypts the specified ciphertext. Supports both v2 AES-GCM payloads and legacy v1 AES-CBC payloads.
    /// </summary>
    /// <param name="cipherText">The Base64-encoded ciphertext to decrypt</param>
    /// <returns>The decrypted plaintext</returns>
    public string Decrypt(string cipherText)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(cipherText);

        if (cipherText.Length == 0)
        {
            _logger?.LogWarning("Attempted to decrypt empty string - this may indicate data corruption");
            throw new CryptographicException("Cannot decrypt empty string");
        }

        try
        {
            var fullPayload = Convert.FromBase64String(cipherText);

            var result = IsV2Payload(fullPayload)
                ? DecryptV2Gcm(fullPayload)
                : DecryptLegacyV1Cbc(fullPayload);

            _logger?.LogDebug("Successfully decrypted data (length: {EncryptedLength} -> {PlainLength})",
                cipherText.Length, result.Length);

            return result;
        }
        catch (Exception ex) when (ex is FormatException or ArgumentException)
        {
            _logger?.LogError(ex, "Invalid encrypted data format");
            throw new CryptographicException("Invalid encrypted data format", ex);
        }
        catch (CryptographicException ex)
        {
            _logger?.LogError(ex, "Decryption operation failed - possible key mismatch, tampering, or data corruption");
            throw;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Unexpected error during decryption");
            throw new CryptographicException("Decryption operation failed due to unexpected error", ex);
        }
    }

    private static bool IsV2Payload(ReadOnlySpan<byte> payload) =>
        payload.Length >= V2HeaderLength && payload[..V2MagicHeader.Length].SequenceEqual(V2MagicHeader);

    private string DecryptV2Gcm(ReadOnlySpan<byte> payload)
    {
        var salt = payload.Slice(4, SaltLength);
        var nonce = payload.Slice(4 + SaltLength, NonceLength);
        var tag = payload.Slice(4 + SaltLength + NonceLength, TagLength);
        var cipherBytes = payload[V2HeaderLength..];

        var derivedKey = new byte[KeyLength];
        try
        {
            HKDF.DeriveKey(HashAlgorithmName.SHA256, _ikm, derivedKey, salt, HkdfInfo);

            var plainBytes = new byte[cipherBytes.Length];
            using var aesGcm = new AesGcm(derivedKey, TagLength);
            aesGcm.Decrypt(nonce, cipherBytes, tag, plainBytes, V2MagicHeader);

            return Encoding.UTF8.GetString(plainBytes);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(derivedKey);
        }
    }

    private string DecryptLegacyV1Cbc(byte[] fullCipher)
    {
        // Legacy format: [16-byte IV][AES-CBC-PKCS7 ciphertext]
        const int minLength = 16 + 16;
        if (fullCipher.Length < minLength)
        {
            _logger?.LogError("Encrypted data is too short (length: {Length}, minimum: {MinLength})",
                fullCipher.Length, minLength);
            throw new CryptographicException("Invalid encrypted data - insufficient length");
        }

        var iv = new byte[16];
        Array.Copy(fullCipher, 0, iv, 0, iv.Length);

        var cipher = new byte[fullCipher.Length - iv.Length];
        Array.Copy(fullCipher, iv.Length, cipher, 0, cipher.Length);

        using var aes = Aes.Create();
        aes.Key = _legacyCbcKey;
        aes.IV = iv;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        using var decryptor = aes.CreateDecryptor();
        using var msDecrypt = new MemoryStream(cipher);
        using var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read);
        using var srDecrypt = new StreamReader(csDecrypt, Encoding.UTF8);

        return srDecrypt.ReadToEnd();
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, nameof(AesEncryptionService));
    }

    /// <summary>
    /// Disposes the encryption service and clears key material from memory.
    /// </summary>
    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Disposes managed key material.
    /// </summary>
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed && disposing)
        {
            CryptographicOperations.ZeroMemory(_ikm);
            CryptographicOperations.ZeroMemory(_legacyCbcKey);

            _disposed = true;
            _logger?.LogInformation("AES encryption service disposed");
        }
    }
}
