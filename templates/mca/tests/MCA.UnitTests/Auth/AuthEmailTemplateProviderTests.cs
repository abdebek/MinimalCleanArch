#if (UseAuth)
using FluentAssertions;
using MCA.Infrastructure.Configuration;
using MCA.Infrastructure.Providers;
using Microsoft.Extensions.Options;
using Xunit;

namespace MCA.UnitTests.Auth;

public class AuthEmailTemplateProviderTests
{
    private readonly AuthEmailTemplateProvider _sut;
    private const string AppBaseUrl = "https://example.com";
    private const string AppName = "TestApp";

    public AuthEmailTemplateProviderTests()
    {
        var settings = Options.Create(new EmailSettings
        {
            AppBaseUrl = AppBaseUrl,
            AppName = AppName,
            SenderEmail = "no-reply@example.com",
            SenderName = AppName
        });
        _sut = new AuthEmailTemplateProvider(settings);
    }

    // --- Password reset ---

    [Fact]
    public void CreatePasswordResetEmail_SetsRecipient()
    {
        var email = _sut.CreatePasswordResetEmail("user@example.com", "tok123", "uid-1");

        email.To.Should().Be("user@example.com");
    }

    [Fact]
    public void CreatePasswordResetEmail_SubjectContainsAppName()
    {
        var email = _sut.CreatePasswordResetEmail("user@example.com", "tok123", "uid-1");

        email.Subject.Should().Contain(AppName);
    }

    [Fact]
    public void CreatePasswordResetEmail_BodyContainsResetPath()
    {
        var email = _sut.CreatePasswordResetEmail("user@example.com", "tok123", "uid-1");

        email.HtmlBody.Should().Contain("/reset-password");
    }

    [Fact]
    public void CreatePasswordResetEmail_BodyContainsToken()
    {
        var email = _sut.CreatePasswordResetEmail("user@example.com", "tok123", "uid-1");

        email.HtmlBody.Should().Contain("tok123");
    }

    [Fact]
    public void CreatePasswordResetEmail_HtmlBodyIsNotEmpty()
    {
        var email = _sut.CreatePasswordResetEmail("user@example.com", "tok123", "uid-1");

        email.HtmlBody.Should().NotBeNullOrWhiteSpace();
    }

    // --- Email confirmation ---

    [Fact]
    public void CreateEmailConfirmationEmail_SetsRecipient()
    {
        var email = _sut.CreateEmailConfirmationEmail("user@example.com", "tok456", "uid-1");

        email.To.Should().Be("user@example.com");
    }

    [Fact]
    public void CreateEmailConfirmationEmail_SubjectContainsAppName()
    {
        var email = _sut.CreateEmailConfirmationEmail("user@example.com", "tok456", "uid-1");

        email.Subject.Should().Contain(AppName);
    }

    [Fact]
    public void CreateEmailConfirmationEmail_BodyContainsConfirmPath()
    {
        var email = _sut.CreateEmailConfirmationEmail("user@example.com", "tok456", "uid-1");

        email.HtmlBody.Should().Contain("/confirm-email");
    }

    [Fact]
    public void CreateEmailConfirmationEmail_BodyContainsToken()
    {
        var email = _sut.CreateEmailConfirmationEmail("user@example.com", "tok456", "uid-1");

        email.HtmlBody.Should().Contain("tok456");
    }

    [Fact]
    public void CreateEmailConfirmationEmail_HtmlBodyIsNotEmpty()
    {
        var email = _sut.CreateEmailConfirmationEmail("user@example.com", "tok456", "uid-1");

        email.HtmlBody.Should().NotBeNullOrWhiteSpace();
    }
}
#endif
