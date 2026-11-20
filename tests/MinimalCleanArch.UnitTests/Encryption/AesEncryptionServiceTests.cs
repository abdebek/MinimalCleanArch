using System.Security.Cryptography;
using System.Text;
using FluentAssertions;
using MinimalCleanArch.Security.Configuration;
using MinimalCleanArch.Security.Encryption;

namespace MinimalCleanArch.UnitTests.Encryption;

public class AesEncryptionServiceTests
{
    private const string TestKey = "this-is-a-very-strong-test-encryption-key";
    private readonly AesEncryptionService _encryptionService;

    public AesEncryptionServiceTests()
    {
        var options = new EncryptionOptions
        {
            Key = TestKey
        };

        _encryptionService = new AesEncryptionService(options);
    }

    [Fact]
    public void Encrypt_ShouldReturnVersionedAuthenticatedCiphertext()
    {
        var plainText = "Sensitive data";

        var encrypted1 = _encryptionService.Encrypt(plainText);
        var encrypted2 = _encryptionService.Encrypt(plainText);

        encrypted1.Should().NotBeEmpty();
        encrypted1.Should().NotBe(plainText);
        encrypted1.Should().NotBe(encrypted2, "each encryption uses a fresh salt and nonce");

        var raw = Convert.FromBase64String(encrypted1);
        Encoding.ASCII.GetString(raw, 0, 4).Should().Be("MCA2");
    }

    [Fact]
    public void Decrypt_ShouldReturnOriginalText()
    {
        var plainText = "Sensitive data with unicode: مرحبا 🚀";
        var encrypted = _encryptionService.Encrypt(plainText);

        var decrypted = _encryptionService.Decrypt(encrypted);

        decrypted.Should().Be(plainText);
    }

    [Fact]
    public void EncryptDecrypt_ShouldWorkForEmptyString()
    {
        var encrypted = _encryptionService.Encrypt(string.Empty);
        var decrypted = _encryptionService.Decrypt(encrypted);

        decrypted.Should().BeEmpty();
    }

    [Fact]
    public void Decrypt_WhenV2CiphertextIsTampered_ShouldThrowCryptographicException()
    {
        var encrypted = _encryptionService.Encrypt("Do not tamper");
        var bytes = Convert.FromBase64String(encrypted);
        bytes[^1] ^= 0xFF; // flip last byte of ciphertext
        var tampered = Convert.ToBase64String(bytes);

        var act = () => _encryptionService.Decrypt(tampered);

        act.Should().Throw<CryptographicException>();
    }

    [Fact]
    public void Decrypt_ShouldDecryptLegacyV1AesCbcPayload()
    {
        var plainText = "Legacy encrypted row";
        var legacyCipherText = EncryptLegacyV1Cbc(plainText, TestKey);

        var decrypted = _encryptionService.Decrypt(legacyCipherText);

        decrypted.Should().Be(plainText);
    }

    [Fact]
    public void GenerateStrongKey_ShouldReturnRequestedLength()
    {
        var key = EncryptionOptions.GenerateStrongKey(64);

        key.Should().HaveLength(64);
    }

    private static string EncryptLegacyV1Cbc(string plainText, string rawKey)
    {
        var key = SHA256.HashData(Encoding.UTF8.GetBytes(rawKey));
        using var aes = Aes.Create();
        aes.Key = key;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;
        aes.GenerateIV();

        using var encryptor = aes.CreateEncryptor();
        using var ms = new MemoryStream();
        ms.Write(aes.IV, 0, aes.IV.Length);
        using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
        using (var sw = new StreamWriter(cs, Encoding.UTF8))
        {
            sw.Write(plainText);
        }

        return Convert.ToBase64String(ms.ToArray());
    }
}
