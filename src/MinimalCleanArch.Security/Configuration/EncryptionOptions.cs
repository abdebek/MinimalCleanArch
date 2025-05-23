using System.ComponentModel.DataAnnotations;

namespace MinimalCleanArch.Security.Configuration;

/// <summary>
/// Options for configuring encryption with enhanced security features
/// </summary>
public class EncryptionOptions
{
    /// <summary>
    /// Gets or sets the encryption key
    /// </summary>
    [Required]
    [MinLength(32, ErrorMessage = "Encryption key must be at least 32 characters long for security")]
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the encryption initialization vector (deprecated - IVs are now generated per encryption)
    /// </summary>
    [Obsolete("IV is now generated automatically for each encryption operation for better security. This property is ignored.")]
    public string? IV { get; set; }

    /// <summary>
    /// Gets or sets whether to validate the encryption key strength
    /// </summary>
    public bool ValidateKeyStrength { get; set; } = true;

    /// <summary>
    /// Gets or sets the minimum required key length
    /// </summary>
    public int MinimumKeyLength { get; set; } = 32;

    /// <summary>
    /// Gets or sets whether to log encryption/decryption operations (for debugging - disable in production)
    /// </summary>
    public bool EnableOperationLogging { get; set; } = false;

    /// <summary>
    /// Gets or sets whether to throw exceptions on validation failures
    /// </summary>
    public bool ThrowOnValidationFailure { get; set; } = true;

    /// <summary>
    /// Validates the encryption options
    /// </summary>
    /// <returns>Validation result</returns>
    public ValidationResult Validate()
    {
        var validationContext = new ValidationContext(this);
        var validationResults = new List<ValidationResult>();
        
        if (!Validator.TryValidateObject(this, validationContext, validationResults, true))
        {
            return validationResults.First();
        }

        // Additional custom validation
        if (ValidateKeyStrength)
        {
            if (string.IsNullOrWhiteSpace(Key))
            {
                return new ValidationResult("Encryption key cannot be null or empty");
            }

            if (Key.Length < MinimumKeyLength)
            {
                return new ValidationResult($"Encryption key must be at least {MinimumKeyLength} characters long");
            }

            // Check for weak patterns
            if (IsWeakKey(Key))
            {
                return new ValidationResult("Encryption key appears to be weak. Use a strong, random key.");
            }
        }

        return ValidationResult.Success!;
    }

    /// <summary>
    /// Checks if the key appears to be weak
    /// </summary>
    /// <param name="key">The key to check</param>
    /// <returns>True if the key appears weak</returns>
    private static bool IsWeakKey(string key)
    {
        // Basic weak key detection
        var weakPatterns = new[]
        {
            "password",
            "123456",
            "qwerty",
            "admin",
            "secret",
            "key",
            "test"
        };

        var lowerKey = key.ToLowerInvariant();
        
        // Check for common weak patterns
        if (weakPatterns.Any(pattern => lowerKey.Contains(pattern)))
        {
            return true;
        }

        // Check for repetitive characters
        if (key.Distinct().Count() < key.Length / 2)
        {
            return true;
        }

        // Check for sequential characters
        var hasSequential = false;
        for (int i = 0; i < key.Length - 2; i++)
        {
            if (key[i] + 1 == key[i + 1] && key[i + 1] + 1 == key[i + 2])
            {
                hasSequential = true;
                break;
            }
        }

        return hasSequential;
    }

    /// <summary>
    /// Generates a strong random encryption key
    /// </summary>
    /// <param name="length">The desired key length (minimum 32)</param>
    /// <returns>A strong random key</returns>
    public static string GenerateStrongKey(int length = 64)
    {
        if (length < 32)
            throw new ArgumentException("Key length must be at least 32 characters", nameof(length));

        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*()_+-=[]{}|;:,.<>?";
        
        using var rng = System.Security.Cryptography.RandomNumberGenerator.Create();
        var bytes = new byte[length];
        rng.GetBytes(bytes);
        
        var result = new char[length];
        for (int i = 0; i < length; i++)
        {
            result[i] = chars[bytes[i] % chars.Length];
        }
        
        return new string(result);
    }
}