using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using MCA.Domain.Constants;
using MCA.Application.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.DependencyInjection;
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

    [Fact]
    public async Task ScalarUi_LoadsInDevelopment()
    {
        var response = await _client.GetAsync("/scalar/v1");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task External_Unknown_Provider_Returns_NotFound()
    {
        using var response = await _client.GetAsync("/api/auth/external/NotAProvider");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await response.Content.ReadAsStringAsync()).Should().Contain("not configured");
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
    public async Task Register_ValidRequest_SendsEmailConfirmationLinkAndConfirms()
    {
        EmailSender.Clear();
        var email = UniqueEmail();

        var registerResponse = await _client.PostAsJsonAsync("/api/auth/register", new { email, password = "Test@1234" });

        registerResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var registerBody = await registerResponse.Content.ReadFromJsonAsync<RegisterResult>();
        registerBody.Should().NotBeNull();

        var message = EmailSender.GetLatestForRecipient(email, "confirm");
        var (userId, token) = ExtractAuthLinkParams(message.HtmlBody, "confirm-email");

        userId.Should().Be(registerBody!.userId);

        var confirmResponse = await _client.PostAsJsonAsync("/api/auth/confirm-email", new { userId, token });
        confirmResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await userManager.FindByIdAsync(userId);
        user.Should().NotBeNull();
        user!.EmailConfirmed.Should().BeTrue();
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
    public async Task ForgotPassword_KnownEmail_DoesNotLeakToken()
    {
        var email = UniqueEmail();
        await _client.PostAsJsonAsync("/api/auth/register", new { email, password = "Test@1234" });

        var response = await _client.PostAsJsonAsync("/api/auth/forgot-password", new { email });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        body.GetProperty("message").GetString().Should().NotBeNullOrWhiteSpace();
        body.TryGetProperty("token", out _).Should().BeFalse();
    }

    [Fact]
    public async Task ForgotPassword_KnownEmail_ResetLinkAllowsPasswordReset()
    {
        EmailSender.Clear();
        var email = UniqueEmail();

        var registerResponse = await _client.PostAsJsonAsync("/api/auth/register", new { email, password = "Test@1234" });
        registerResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var forgotResponse = await _client.PostAsJsonAsync("/api/auth/forgot-password", new { email });
        forgotResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var message = EmailSender.GetLatestForRecipient(email, "reset");
        var (userId, token) = ExtractAuthLinkParams(message.HtmlBody, "reset-password");

        var newPassword = "Reset!9876";
        var resetResponse = await _client.PostAsJsonAsync("/api/auth/reset-password", new { userId, token, newPassword });
        resetResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await userManager.FindByIdAsync(userId);
        user.Should().NotBeNull();
        (await userManager.CheckPasswordAsync(user!, newPassword)).Should().BeTrue();
    }

    // --- Confirm email ---

    [Fact]
    public async Task ConfirmEmail_InvalidToken_ReturnsBadRequest()
    {
        // Register first so the failure is invalid token (Validation/400), not missing user (NotFound/404)
        var email = UniqueEmail();
        var reg = await _client.PostAsJsonAsync("/api/auth/register", new { email, password = "Test@1234" });
        var regBody = await reg.Content.ReadFromJsonAsync<RegisterResult>();

        var request = new { userId = regBody!.userId, token = "bad-token" };
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
        var html = await response.Content.ReadAsStringAsync();
        html.Should().Contain("Continue with Google");
        html.Should().Contain("Continue with Microsoft");
        html.Should().Contain("Continue with GitHub");
        html.Should().NotContain("<!--");
        html.Should().Contain("/api/auth/external/Google");
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

    [Fact]
    public async Task OAuthDemoEndToEnd_CompletesPkceFlowAndReturnsTokens()
    {
        var email = UniqueEmail();
        await _client.PostAsJsonAsync("/api/auth/register", new { email, password = "Test@1234" });

        var demoClient = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true
        });

        var startResponse = await demoClient.GetAsync("/oauth/demo/start");
        startResponse.StatusCode.Should().Be(HttpStatusCode.Redirect);
        var authorizeLocation = ToRelativePathAndQuery(startResponse.Headers.Location!.ToString());

        var unauthAuthorizeResponse = await demoClient.GetAsync(authorizeLocation);
        unauthAuthorizeResponse.StatusCode.Should().Be(HttpStatusCode.Redirect);
        var loginLocation = unauthAuthorizeResponse.Headers.Location!.ToString();
        loginLocation.Should().Contain("/auth/login");

        var loginResponse = await demoClient.PostAsync("/auth/login", new FormUrlEncodedContent(
            new Dictionary<string, string>
            {
                ["email"] = email,
                ["password"] = "Test@1234",
                ["returnUrl"] = authorizeLocation
            }));

        loginResponse.StatusCode.Should().Be(HttpStatusCode.Redirect);
        var postLoginLocation = ToRelativePathAndQuery(loginResponse.Headers.Location!.ToString());
        postLoginLocation.Should().Contain("/connect/authorize");

        var authorizeAfterLoginResponse = await demoClient.GetAsync(postLoginLocation);
        authorizeAfterLoginResponse.StatusCode.Should().Be(HttpStatusCode.Redirect);
        var callbackLocation = ToRelativePathAndQuery(authorizeAfterLoginResponse.Headers.Location!.ToString());
        callbackLocation.Should().Contain("/oauth/demo/callback");
        callbackLocation.Should().Contain("code=");
        callbackLocation.Should().Contain("state=");

        var callbackResponse = await demoClient.GetAsync(callbackLocation);
        var callbackBodyText = await callbackResponse.Content.ReadAsStringAsync();
        callbackResponse.StatusCode.Should().Be(HttpStatusCode.OK, $"callback response body: {callbackBodyText}");

        var body = System.Text.Json.JsonDocument.Parse(callbackBodyText).RootElement;
        body.GetProperty("message").GetString().Should().Contain("PKCE flow completed successfully");
        body.TryGetProperty("tokens", out var tokens).Should().BeTrue();
        tokens.TryGetProperty("access_token", out _).Should().BeTrue();
        tokens.TryGetProperty("token_type", out _).Should().BeTrue();
    }

    [Fact]
    public async Task BootstrapAdminSeed_CreatesAdminRoleAndUser()
    {
        using var scope = _factory.Services.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        (await roleManager.RoleExistsAsync(Roles.Admin)).Should().BeTrue();

        var adminUser = await userManager.FindByEmailAsync("admin@example.com");
        adminUser.Should().NotBeNull();
        (await userManager.IsInRoleAsync(adminUser!, Roles.Admin)).Should().BeTrue();
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

    private RecordingEmailSender EmailSender => _factory.Services.GetRequiredService<RecordingEmailSender>();

    private static (string UserId, string Token) ExtractAuthLinkParams(string htmlBody, string routeSegment)
    {
        var marker = $"/{routeSegment}?";
        var markerIndex = htmlBody.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        markerIndex.Should().BeGreaterThanOrEqualTo(0, $"Expected link containing '{marker}'.");

        var urlStart = htmlBody.LastIndexOf("http", markerIndex, StringComparison.OrdinalIgnoreCase);
        urlStart.Should().BeGreaterThanOrEqualTo(0, "Expected absolute URL in email body.");

        var urlEnd = htmlBody.IndexOf('"', urlStart);
        urlEnd.Should().BeGreaterThan(urlStart, "Expected URL to end with a quote character.");

        var encodedUrl = htmlBody.Substring(urlStart, urlEnd - urlStart);
        var uri = new Uri(encodedUrl);
        var query = QueryHelpers.ParseQuery(uri.Query);

        query.TryGetValue("userId", out var userIdValues).Should().BeTrue();
        query.TryGetValue("token", out var tokenValues).Should().BeTrue();

        var userId = userIdValues.ToString();
        var token = tokenValues.ToString();

        userId.Should().NotBeNullOrWhiteSpace();
        token.Should().NotBeNullOrWhiteSpace();

        return (userId, token);
    }

    private static string UniqueEmail() => $"test{Guid.NewGuid():N}@example.com";

    private static string ToRelativePathAndQuery(string uriOrPath)
    {
        if (string.IsNullOrWhiteSpace(uriOrPath))
            return uriOrPath;

        // App-relative paths must be returned as-is. On Unix, UriKind.Absolute parses
        // "/connect/authorize?..." as file:///... and PathAndQuery encodes '?' to %3F,
        // which breaks the subsequent request to the authorize endpoint.
        if (uriOrPath.StartsWith('/') && !uriOrPath.StartsWith("//", StringComparison.Ordinal))
            return uriOrPath;

        if (Uri.TryCreate(uriOrPath, UriKind.Absolute, out var absolute)
            && (absolute.Scheme == Uri.UriSchemeHttp || absolute.Scheme == Uri.UriSchemeHttps))
        {
            return absolute.PathAndQuery;
        }

        return uriOrPath;
    }

    private record RegisterResult(string userId, string message);
}


