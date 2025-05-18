namespace MinimalCleanArch.Security.Encryption;

/// <summary>
/// Interface for encryption services
/// </summary>
public interface IEncryptionService
{
    /// <summary>
    /// Encrypts the specified plaintext
    /// </summary>
    /// <param name="plainText">The plaintext to encrypt</param>
    /// <returns>The encrypted ciphertext</returns>
    string Encrypt(string plainText);

    /// <summary>
    /// Decrypts the specified ciphertext
    /// </summary>
    /// <param name="cipherText">The ciphertext to decrypt</param>
    /// <returns>The decrypted plaintext</returns>
    string Decrypt(string cipherText);
}
