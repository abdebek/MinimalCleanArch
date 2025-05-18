using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using MinimalCleanArch.Security.Encryption;

namespace MinimalCleanArch.Security.EntityFramework;

/// <summary>
/// Value converter for encrypted properties
/// </summary>
public class EncryptedConverter : ValueConverter<string, string>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EncryptedConverter"/> class
    /// </summary>
    /// <param name="encryptionService">The encryption service</param>
    public EncryptedConverter(IEncryptionService encryptionService)
        : base(
            v => EncryptValue(v, encryptionService),
            v => DecryptValue(v, encryptionService),
            null)
    {
        ArgumentNullException.ThrowIfNull(encryptionService);
    }

    private static string EncryptValue(string? value, IEncryptionService encryptionService)
    {
        if (value == null)
            return string.Empty; // Return a non-null value to avoid CS8603
        var encrypted = encryptionService.Encrypt(value);
        if (encrypted == null)
            throw new InvalidOperationException("Encryption returned null.");
        return encrypted;
    }

    private static string DecryptValue(string? value, IEncryptionService encryptionService)
    {
        if (value == null)
            return string.Empty; // Return a non-null value to avoid CS8603
        var decrypted = encryptionService.Decrypt(value);
        if (decrypted == null)
            throw new InvalidOperationException("Decryption returned null.");
        return decrypted;
    }
}
