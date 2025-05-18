using System.Security.Cryptography;
using System.Text;
using MinimalCleanArch.Security.Configuration;

namespace MinimalCleanArch.Security.Encryption;

/// <summary>
/// Implementation of <see cref="IEncryptionService"/> using AES encryption
/// </summary>
public class AesEncryptionService : IEncryptionService
{
    private readonly byte[] _key;

    /// <summary>
    /// Initializes a new instance of the <see cref="AesEncryptionService"/> class
    /// </summary>
    /// <param name="encryptionOptions">The encryption options</param>
    public AesEncryptionService(EncryptionOptions encryptionOptions)
    {
        if (string.IsNullOrEmpty(encryptionOptions.Key))
            throw new ArgumentException("Encryption key must be provided", nameof(encryptionOptions.Key));

        // Create a key from the key string
        using var sha256 = SHA256.Create();
        _key = sha256.ComputeHash(Encoding.UTF8.GetBytes(encryptionOptions.Key));
    }

    /// <summary>
    /// Encrypts the specified plaintext
    /// </summary>
    /// <param name="plainText">The plaintext to encrypt</param>
    /// <returns>The encrypted ciphertext</returns>
    public string Encrypt(string plainText)
    {
        if (string.IsNullOrEmpty(plainText))
            return plainText;

        using var aes = Aes.Create();
        aes.Key = _key;
        
        // Generate a new IV for each encryption
        aes.GenerateIV();
        var iv = aes.IV;

        using var encryptor = aes.CreateEncryptor(aes.Key, iv);
        using var msEncrypt = new MemoryStream();
        
        // Write the IV to the beginning of the output stream
        msEncrypt.Write(iv, 0, iv.Length);
        
        using (var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
        using (var swEncrypt = new StreamWriter(csEncrypt))
        {
            swEncrypt.Write(plainText);
        }

        return Convert.ToBase64String(msEncrypt.ToArray());
    }

    /// <summary>
    /// Decrypts the specified ciphertext
    /// </summary>
    /// <param name="cipherText">The ciphertext to decrypt</param>
    /// <returns>The decrypted plaintext</returns>
    public string Decrypt(string cipherText)
    {
        if (string.IsNullOrEmpty(cipherText))
            return cipherText;

        try
        {
            var fullCipher = Convert.FromBase64String(cipherText);
            
            // IV size is 16 bytes for AES
            if (fullCipher.Length < 16)
                throw new CryptographicException("Invalid ciphertext");
            
            // Get the IV from the beginning of the ciphertext
            var iv = new byte[16];
            Array.Copy(fullCipher, 0, iv, 0, iv.Length);
            
            // Get the actual ciphertext after the IV
            var cipher = new byte[fullCipher.Length - iv.Length];
            Array.Copy(fullCipher, iv.Length, cipher, 0, cipher.Length);

            using var aes = Aes.Create();
            aes.Key = _key;
            aes.IV = iv;

            using var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
            using var msDecrypt = new MemoryStream(cipher);
            using var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read);
            using var srDecrypt = new StreamReader(csDecrypt);
            
            return srDecrypt.ReadToEnd();
        }
        catch (Exception ex) when (ex is CryptographicException or FormatException)
        {
            // Log the exception in a real application
            // For security reasons, don't return the original ciphertext or error message
            throw new CryptographicException("Failed to decrypt the data", ex);
        }
    }
}
