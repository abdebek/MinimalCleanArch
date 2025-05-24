using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using System.Text;

namespace MinimalCleanArch.Security.Configuration;

/// <summary>
/// Options for configuring encryption with enhanced security features and environment variable support
/// </summary>
public class EncryptionOptions
{
    private string? _key;
    private bool _keyLoadedFromEnvironment;

    /// <summary>
    /// Gets or sets the encryption key
    /// Priority: 1. Explicitly set Key property, 2. ENCRYPTION_KEY environment variable, 3. MINIMALCLEANARCH_ENCRYPTION_KEY
    /// </summary>
    [Required]
    public string Key 
    { 
        get => _key ?? LoadKeyFromEnvironment() ?? throw new InvalidOperationException("Encryption key not configured");
        set 
        {
            _key = value;
            _keyLoadedFromEnvironment = false;
        }
    }

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
    /// Gets or sets the environment variable name to check for the encryption key
    /// </summary>
    public string PrimaryEnvironmentVariable { get; set; } = "ENCRYPTION_KEY";

    /// <summary>
    /// Gets or sets the fallback environment variable name to check for the encryption key
    /// </summary>
    public string FallbackEnvironmentVariable { get; set; } = "MINIMALCLEANARCH_ENCRYPTION_KEY";

    /// <summary>
    /// Gets or sets whether to allow key loading from environment variables
    /// </summary>
    public bool AllowEnvironmentVariables { get; set; } = true;

    /// <summary>
    /// Gets a value indicating whether the key was loaded from an environment variable
    /// </summary>
    public bool IsKeyFromEnvironment => _keyLoadedFromEnvironment;

    /// <summary>
    /// Loads the encryption key from environment variables
    /// </summary>
    /// <returns>The encryption key or null if not found</returns>
    private string? LoadKeyFromEnvironment()
    {
        if (!AllowEnvironmentVariables)
            return null;

        // Try primary environment variable first
        var key = Environment.GetEnvironmentVariable(PrimaryEnvironmentVariable);
        if (!string.IsNullOrWhiteSpace(key))
        {
            _keyLoadedFromEnvironment = true;
            return key;
        }

        // Try fallback environment variable
        key = Environment.GetEnvironmentVariable(FallbackEnvironmentVariable);
        if (!string.IsNullOrWhiteSpace(key))
        {
            _keyLoadedFromEnvironment = true;
            return key;
        }

        return null;
    }

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

        // Get the actual key for validation
        string? actualKey;
        try
        {
            actualKey = Key; // This will trigger environment variable loading if needed
        }
        catch (InvalidOperationException ex)
        {
            return new ValidationResult(ex.Message);
        }

        // Additional custom validation
        if (ValidateKeyStrength)
        {
            if (string.IsNullOrWhiteSpace(actualKey))
            {
                return new ValidationResult("Encryption key cannot be null or empty");
            }

            if (actualKey.Length < MinimumKeyLength)
            {
                return new ValidationResult($"Encryption key must be at least {MinimumKeyLength} characters long");
            }

            // Check for weak patterns
            if (IsWeakKey(actualKey))
            {
                return new ValidationResult("Encryption key appears to be weak. Use a strong, random key.");
            }

            // Additional check for test keys in production
            if (IsTestKey(actualKey))
            {
                return new ValidationResult("Test encryption keys detected. Never use test keys in production!");
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
            "test",
            "default",
            "sample",
            "demo",
            "example"
        };

        var lowerKey = key.ToLowerInvariant();
        
        // Check for common weak patterns
        if (weakPatterns.Any(pattern => lowerKey.Contains(pattern)))
        {
            return true;
        }

        // Check for repetitive characters (more than 50% same character)
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
    /// Checks if the key appears to be a test key
    /// </summary>
    /// <param name="key">The key to check</param>
    /// <returns>True if the key appears to be for testing</returns>
    private static bool IsTestKey(string key)
    {
        var testPatterns = new[]
        {
            "test-encryption-key",
            "unit-test",
            "integration-test",
            "benchmark",
            "for-tests",
            "test-key",
            "fake-key"
        };

        var lowerKey = key.ToLowerInvariant();
        return testPatterns.Any(pattern => lowerKey.Contains(pattern));
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

        // Use only alphanumeric and safe symbols to avoid encoding issues
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*()_+-=[]{}|;:,.<>?";
        
        using var rng = RandomNumberGenerator.Create();
        var bytes = new byte[length * 2]; // Generate more bytes than needed for better randomness
        rng.GetBytes(bytes);
        
        var result = new StringBuilder(length);
        for (int i = 0; i < length; i++)
        {
            // Use two bytes to get better distribution
            var randomIndex = (bytes[i * 2] << 8 | bytes[i * 2 + 1]) % chars.Length;
            result.Append(chars[randomIndex]);
        }
        
        return result.ToString();
    }

    /// <summary>
    /// Generates a strong key and prints environment variable setup instructions
    /// </summary>
    /// <param name="length">The desired key length</param>
    /// <returns>Setup instructions with the generated key</returns>
    public static string GenerateKeyWithInstructions(int length = 64)
    {
        var key = GenerateStrongKey(length);
        
        var instructions = new StringBuilder();
        instructions.AppendLine("Generated strong encryption key. Set it as an environment variable:");
        instructions.AppendLine();
        instructions.AppendLine("Windows (Command Prompt):");
        instructions.AppendLine($"set ENCRYPTION_KEY={key}");
        instructions.AppendLine();
        instructions.AppendLine("Windows (PowerShell):");
        instructions.AppendLine($"$env:ENCRYPTION_KEY=\"{key}\"");
        instructions.AppendLine();
        instructions.AppendLine("Linux/macOS:");
        instructions.AppendLine($"export ENCRYPTION_KEY=\"{key}\"");
        instructions.AppendLine();
        instructions.AppendLine("Docker:");
        instructions.AppendLine($"docker run -e ENCRYPTION_KEY=\"{key}\" your-app");
        instructions.AppendLine();
        instructions.AppendLine("appsettings.json (NOT RECOMMENDED for production):");
        instructions.AppendLine("{");
        instructions.AppendLine("  \"Encryption\": {");
        instructions.AppendLine($"    \"Key\": \"{key}\"");
        instructions.AppendLine("  }");
        instructions.AppendLine("}");
        instructions.AppendLine();
        instructions.AppendLine("⚠️  SECURITY WARNING: Never commit this key to source control!");
        
        return instructions.ToString();
    }

    /// <summary>
    /// Creates encryption options optimized for testing
    /// </summary>
    /// <returns>Encryption options suitable for testing</returns>
    public static EncryptionOptions ForTesting()
    {
        return new EncryptionOptions
        {
            Key = GenerateStrongKey(32), // Generate a new key each time for test isolation
            ValidateKeyStrength = false, // Disable validation in tests for speed
            EnableOperationLogging = false,
            AllowEnvironmentVariables = false // Don't load from environment in tests
        };
    }

    /// <summary>
    /// Creates encryption options optimized for production
    /// </summary>
    /// <returns>Encryption options suitable for production</returns>
    public static EncryptionOptions ForProduction()
    {
        return new EncryptionOptions
        {
            ValidateKeyStrength = true,
            EnableOperationLogging = false, // Disable logging in production for security
            AllowEnvironmentVariables = true,
            ThrowOnValidationFailure = true
        };
    }
}