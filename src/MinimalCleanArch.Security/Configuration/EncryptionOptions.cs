namespace MinimalCleanArch.Security.Configuration;

/// <summary>
/// Options for configuring encryption
/// </summary>
public class EncryptionOptions
{
    /// <summary>
    /// Gets or sets the encryption key
    /// </summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the encryption initialization vector
    /// </summary>
    public string? IV { get; set; }
}
