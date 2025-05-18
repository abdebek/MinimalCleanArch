using MinimalCleanArch.Security.Configuration;
using MinimalCleanArch.Security.Encryption;
using FluentAssertions;

namespace MinimalCleanArch.UnitTests.Encryption;

public class AesEncryptionServiceTests
{
    private readonly IEncryptionService _encryptionService;
    
    public AesEncryptionServiceTests()
    {
        var options = new EncryptionOptions
        {
            Key = "this-is-a-very-strong-test-encryption-key"
        };
        
        _encryptionService = new AesEncryptionService(options);
    }
    
    [Fact]
    public void Encrypt_ShouldReturnEncryptedString()
    {
        // Arrange
        var plainText = "Sensitive data";
        
        // Act
        var encrypted = _encryptionService.Encrypt(plainText);
        
        // Assert
        encrypted.Should().NotBeEmpty();
        encrypted.Should().NotBe(plainText);
    }
    
    [Fact]
    public void Decrypt_ShouldReturnOriginalText()
    {
        // Arrange
        var plainText = "Sensitive data";
        var encrypted = _encryptionService.Encrypt(plainText);
        
        // Act
        var decrypted = _encryptionService.Decrypt(encrypted);
        
        // Assert
        decrypted.Should().Be(plainText);
    }
    
    [Fact]
    public void EncryptDecrypt_ShouldWorkForEmptyString()
    {
        // Arrange
        var plainText = string.Empty;
        
        // Act
        var encrypted = _encryptionService.Encrypt(plainText);
        var decrypted = _encryptionService.Decrypt(encrypted);
        
        // Assert
        decrypted.Should().Be(plainText);
    }
}
