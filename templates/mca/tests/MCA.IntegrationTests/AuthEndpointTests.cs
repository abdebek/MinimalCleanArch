#if (UseAuth && !UseMessaging)
using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using MCA.Application.Interfaces;
using MCA.Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace MCA.IntegrationTests;

public class AuthEndpointTests : IClassFixture<AuthTestApiFactory>
{
    private readonly AuthTestApiFactory _factory;
    private readonly HttpClient _client;

    public AuthEndpointTests(AuthTestApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    // --- Register ---

    [Fact]
    public async Task Register_ValidRequest_ReturnsOkWithUserId()
    {
        var request = new { email = UniqueEmail(), password = "Test@1234" };

        var response = await _client.PostAsJsonAsync("/api/auth/register", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<RegisterResult>();
        body!.userId.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Register_DuplicateEmail_ReturnsBadRequest()
    {
        var email = UniqueEmail();
        await _client.PostAsJsonAsync("/api/auth/register", new { email, password = "Test@1234" });

        var response = await _client.PostAsJsonAsync("/api/auth/register", new { email, password = "Test@1234" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // --- Forgot password ---

    [Fact]
    public async Task ForgotPassword_UnknownEmail_ReturnsOkToPreventEnumeration()
    {
        var request = new { email = "nobody@example.com" };

        var response = await _client.PostAsJsonAsync("/api/auth/forgot-password", request);

        // Always 200 regardless of whether the email exists (prevents user enumeration)
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ForgotPassword_KnownEmail_ReturnsDevToken()
    {
        var email = UniqueEmail();
        await _client.PostAsJsonAsync("/api/auth/register", new { email, password = "Test@1234" });

        var response = await _client.PostAsJsonAsync("/api/auth/forgot-password", new { email });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ForgotPasswordResult>();
        body!.token.Should().NotBeNullOrWhiteSpace();
    }

    // --- Confirm email ---

    [Fact]
    public async Task ConfirmEmail_InvalidToken_ReturnsBadRequest()
    {
        // Use a valid Guid format — UserManager<Guid> will parse it before looking up the user
        var request = new { userId = Guid.NewGuid().ToString(), token = "bad-token" };

        var response = await _client.PostAsJsonAsync("/api/auth/confirm-email", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // --- Reset password ---

    [Fact]
    public async Task ResetPassword_InvalidToken_ReturnsBadRequest()
    {
        var email = UniqueEmail();
        var reg = await _client.PostAsJsonAsync("/api/auth/register", new { email, password = "Test@1234" });
        var regBody = await reg.Content.ReadFromJsonAsync<RegisterResult>();

        var request = new { userId = regBody!.userId, token = "bad-token", newPassword = "NewPass@9876" };
        var response = await _client.PostAsJsonAsync("/api/auth/reset-password", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // --- SSR login page (development only) ---

    [Fact]
    public async Task GetLoginPage_ReturnsHtml()
    {
        var response = await _client.GetAsync("/auth/login");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be("text/html");
    }

    [Fact]
    public async Task GetLoginPage_WithErrorParam_RendersErrorMessage()
    {
        var response = await _client.GetAsync("/auth/login?error=Invalid+credentials");

        var html = await response.Content.ReadAsStringAsync();
        html.Should().Contain("Invalid credentials");
    }

    [Fact]
    public async Task GetLoginPage_WithReturnUrl_EmbeddsItInForm()
    {
        var response = await _client.GetAsync("/auth/login?returnUrl=/connect/authorize%3Fresponse_type%3Dcode");

        var html = await response.Content.ReadAsStringAsync();
        html.Should().Contain("/connect/authorize");
    }

    [Fact]
    public async Task OAuthDemoStart_RedirectsToAuthorizeUrl()
    {
        var demoClient = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true
        });

        var response = await demoClient.GetAsync("/oauth/demo/start");

        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        var location = response.Headers.Location!.ToString();
        location.Should().Contain("/connect/authorize");
        location.Should().Contain("client_id=mca-web-client");
        location.Should().Contain("code_challenge_method=S256");
        location.Should().Contain("redirect_uri=http%3A%2F%2Flocalhost%2Foauth%2Fdemo%2Fcallback");
    }

    [Fact]
    public async Task AuthorizeEndpoint_Unauthenticated_RedirectsToLogin()
    {
        var demoClient = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true
        });

        var startResponse = await demoClient.GetAsync("/oauth/demo/start");
        var authorizeLocation = startResponse.Headers.Location!.ToString();

        var authorizeResponse = await demoClient.GetAsync(authorizeLocation);

        authorizeResponse.StatusCode.Should().Be(HttpStatusCode.Redirect);
        var loginLocation = authorizeResponse.Headers.Location!.ToString();
        loginLocation.Should().Contain("/auth/login");
        Uri.UnescapeDataString(loginLocation).Should().Contain("/connect/authorize");
    }

    [Fact]
    public async Task OAuthDemoCallback_WithInvalidState_ReturnsBadRequest()
    {
        var demoClient = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true
        });

        _ = await demoClient.GetAsync("/oauth/demo/start");

        var response = await demoClient.GetAsync("/oauth/demo/callback?code=fake-code&state=wrong-state");
        var body = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        body.Should().Contain("State mismatch");
    }

    // --- SSR form login (development only) ---

    [Fact]
    public async Task FormLogin_InvalidCredentials_RedirectsBackWithError()
    {
        var loginClient = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        var form = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["email"] = "nobody@example.com",
            ["password"] = "WrongPassword",
            ["returnUrl"] = ""
        });

        var response = await loginClient.PostAsync("/auth/login", form);

        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location!.ToString().Should().Contain("/auth/login");
        response.Headers.Location!.ToString().Should().Contain("error=");
    }

    [Fact]
    public async Task FormLogin_ValidCredentials_RedirectsAndSetsAuthCookie()
    {
        var email = UniqueEmail();
        await _client.PostAsJsonAsync("/api/auth/register", new { email, password = "Test@1234" });

        var loginClient = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true
        });
        var form = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["email"] = email,
            ["password"] = "Test@1234",
            ["returnUrl"] = ""
        });

        var response = await loginClient.PostAsync("/auth/login", form);

        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location!.ToString().Should().Be("/");
    }

    // --- Change password ---

    [Fact]
    public async Task ChangePassword_Unauthenticated_Returns401()
    {
        var request = new { currentPassword = "Test@1234", newPassword = "New@5678" };

        var response = await _client.PostAsJsonAsync("/api/auth/change-password", request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ChangePassword_Authenticated_ReturnsOk()
    {
        var email = UniqueEmail();
        await _client.PostAsJsonAsync("/api/auth/register", new { email, password = "Test@1234" });

        // The change-password endpoint requires a bearer token (OpenIddict default policy).
        // Use the password grant to obtain one.
        var tokenResponse = await _client.PostAsync("/connect/token", new FormUrlEncodedContent(
            new Dictionary<string, string>
            {
                ["grant_type"] = "password",
                ["username"] = email,
                ["password"] = "Test@1234",
                ["client_id"] = "mca-web-client",
                ["client_secret"] = "mca-default-secret-change-me",
                ["scope"] = "openid profile email"
            }));

        tokenResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var tokenJson = await tokenResponse.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        var accessToken = tokenJson.GetProperty("access_token").GetString();

        var changeRequest = new HttpRequestMessage(HttpMethod.Post, "/api/auth/change-password");
        changeRequest.Headers.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
        changeRequest.Content = JsonContent.Create(new { currentPassword = "Test@1234", newPassword = "NewTest@9999" });

        var response = await _client.SendAsync(changeRequest);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private static string UniqueEmail() => $"test{Guid.NewGuid():N}@example.com";

    private record RegisterResult(string userId, string message);
    private record ForgotPasswordResult(string message, string? token);
}

public class AuthTestApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((_, configBuilder) =>
        {
            configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["App:BaseUrl"] = "http://localhost"
            });
        });
        builder.ConfigureServices(services =>
        {
            services.RemoveAll(typeof(DbContextOptions<AppDbContext>));
            services.RemoveAll(typeof(IDbContextOptionsConfiguration<AppDbContext>));
            services.RemoveAll<AppDbContext>();

            // Use a fresh in-memory DB per factory instance so test classes don't share state
            var dbName = $"AuthTestDb-{Guid.NewGuid()}";
            services.AddDbContext<AppDbContext>(options =>
            {
                options.UseInMemoryDatabase(dbName);
                options.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
                options.UseOpenIddict<Guid>();
            });

            // Replace SMTP sender with a no-op to prevent connection attempts in tests
            services.RemoveAll<IEmailSender>();
            services.AddTransient<IEmailSender, NoOpEmailSender>();
        });
    }
}

internal sealed class NoOpEmailSender : IEmailSender
{
    public Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}
#endif
