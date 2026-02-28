#if (UseAuth)
using MCA.Infrastructure.Services;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace MCA.Endpoints;

/// <summary>
/// OAuth 2.0 PKCE demo flow — developer helper to test the authorization code + PKCE flow.
/// </summary>
public static class OAuthEndpoints
{
    private const string VerifierSessionKey = "pkce_verifier";
    private const string StateSessionKey = "oauth_state";

    public static void MapOAuthEndpoints(this IEndpointRouteBuilder app, bool isDevelopment = false)
    {
        if (!isDevelopment)
            return;

        app.MapGet("/oauth/demo/start", (
            HttpContext context,
            [FromServices] PkceService pkceService,
            [FromQuery] string? clientId,
            [FromQuery] string? scope) =>
        {
            var (codeVerifier, codeChallenge) = pkceService.GeneratePkce();
            var state = Guid.NewGuid().ToString("N");

            context.Session.SetString(VerifierSessionKey, codeVerifier);
            context.Session.SetString(StateSessionKey, state);

            var baseUrl = $"{context.Request.Scheme}://{context.Request.Host}";
            var redirectUri = Uri.EscapeDataString($"{baseUrl}/oauth/demo/callback");

            var effectiveClientId = clientId ?? "mca-web-client";
            var effectiveScope = scope ?? "openid profile email offline_access mca.api";

            var authorizeUrl = $"{baseUrl}/connect/authorize" +
                $"?response_type=code" +
                $"&client_id={Uri.EscapeDataString(effectiveClientId)}" +
                $"&redirect_uri={redirectUri}" +
                $"&scope={Uri.EscapeDataString(effectiveScope)}" +
                $"&state={state}" +
                $"&code_challenge={codeChallenge}" +
                $"&code_challenge_method=S256";

            return Results.Redirect(authorizeUrl);
        })
        .AllowAnonymous()
        .WithName("OAuthDemoStart")
        .WithTags("OAuth Demo")
        .WithSummary("Start PKCE authorization code flow demo");

        app.MapGet("/oauth/demo/callback", async (
            HttpContext context,
            [FromQuery] string? code,
            [FromQuery] string? state,
            [FromQuery] string? error,
            [FromServices] IHttpClientFactory httpClientFactory,
            [FromServices] IConfiguration configuration,
            [FromQuery] string? clientId) =>
        {
            if (!string.IsNullOrEmpty(error))
                return Results.BadRequest(new { error, description = "Authorization was denied or failed." });

            var storedState = context.Session.GetString(StateSessionKey);
            if (storedState != state)
                return Results.BadRequest(new { error = "State mismatch. Possible CSRF attack." });

            var codeVerifier = context.Session.GetString(VerifierSessionKey);
            if (string.IsNullOrEmpty(codeVerifier))
                return Results.BadRequest(new { error = "No code verifier found in session. Start a new flow." });

            context.Session.Remove(VerifierSessionKey);
            context.Session.Remove(StateSessionKey);

            var baseUrl = $"{context.Request.Scheme}://{context.Request.Host}";
            var redirectUri = $"{baseUrl}/oauth/demo/callback";
            var effectiveClientId = clientId ?? "mca-web-client";
            var clientSecret = configuration["OpenIddict:Clients:Web:Secret"];

            if (string.IsNullOrWhiteSpace(clientSecret))
                return Results.Problem("OpenIddict web client secret is not configured.", statusCode: 500);

            var httpClient = httpClientFactory.CreateClient();
            var tokenRequest = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "authorization_code",
                ["code"] = code!,
                ["redirect_uri"] = redirectUri,
                ["client_id"] = effectiveClientId,
                ["client_secret"] = clientSecret,
                ["code_verifier"] = codeVerifier
            });

            var tokenResponse = await httpClient.PostAsync($"{baseUrl}/connect/token", tokenRequest);
            var tokenJson = await tokenResponse.Content.ReadAsStringAsync();

            if (!tokenResponse.IsSuccessStatusCode)
                return Results.BadRequest(new { error = "Token exchange failed", details = tokenJson });

            return Results.Ok(new
            {
                message = "PKCE flow completed successfully",
                tokens = JsonSerializer.Deserialize<object>(tokenJson)
            });
        })
        .AllowAnonymous()
        .WithName("OAuthDemoCallback")
        .WithTags("OAuth Demo")
        .WithSummary("Handle PKCE authorization code callback and exchange for tokens");
    }
}
#endif
