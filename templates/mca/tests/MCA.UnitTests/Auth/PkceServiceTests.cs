using System.Security.Cryptography;
using System.Text;
using FluentAssertions;
using MCA.Infrastructure.Services;
using Xunit;

namespace MCA.UnitTests.Auth;

public class PkceServiceTests
{
    private readonly PkceService _sut = new();

    [Fact]
    public void GeneratePkce_ReturnsNonEmptyStrings()
    {
        var (verifier, challenge) = _sut.GeneratePkce();

        verifier.Should().NotBeNullOrWhiteSpace();
        challenge.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void GeneratePkce_VerifierAndChallengeDiffer()
    {
        var (verifier, challenge) = _sut.GeneratePkce();

        verifier.Should().NotBe(challenge);
    }

    [Fact]
    public void GeneratePkce_ChallengeIsSha256OfVerifier()
    {
        var (verifier, challenge) = _sut.GeneratePkce();

        var hash = SHA256.HashData(Encoding.ASCII.GetBytes(verifier));
        var expected = Base64UrlEncode(hash);

        challenge.Should().Be(expected);
    }

    [Fact]
    public void GeneratePkce_EachCallProducesUniqueVerifier()
    {
        var (verifier1, _) = _sut.GeneratePkce();
        var (verifier2, _) = _sut.GeneratePkce();

        verifier1.Should().NotBe(verifier2);
    }

    [Fact]
    public void GeneratePkce_VerifierUsesBase64UrlCharset()
    {
        var (verifier, _) = _sut.GeneratePkce();

        // Base64url charset: A-Z a-z 0-9 - _  (no +, /, or padding =)
        verifier.Should().MatchRegex(@"^[A-Za-z0-9\-_]+$");
    }

    [Fact]
    public void GeneratePkce_ChallengeUsesBase64UrlCharset()
    {
        var (_, challenge) = _sut.GeneratePkce();

        challenge.Should().MatchRegex(@"^[A-Za-z0-9\-_]+$");
    }

    private static string Base64UrlEncode(byte[] input) =>
        Convert.ToBase64String(input).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
