namespace MinimalCleanArch.Security.Encryption;

/// <summary>
/// Attribute for marking a property as encrypted
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public class EncryptedAttribute : Attribute
{
}
