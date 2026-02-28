#if (UseAuth)
using MCA.Infrastructure.Services;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace MCA.Api.Endpoints;

/// <summary>
/// OAuth 2.0 PKCE demo flow — shows how to initiate and complete an authorization code + PKCE exchange.
/// This is a developer helper to test the authorization code flow without a separate client application.
/// </summary>
public static class OAuthEndpoints
{
    private const string DefaultWebClientId = "mca-web-client";
    private const string DefaultWebClientSecret = "mca-default-secret-change-me";
    private const string PkceStateStoreSessionKey = "oauth_pkce_state_store";
    private const string PkceCookiePrefix = "mca.oauth.pkce.";
    private static readonly TimeSpan PkceLifetime = TimeSpan.FromMinutes(10);

    public static void MapOAuthEndpoints(this IEndpointRouteBuilder app, bool isDevelopment = false)
    {
        if (!isDevelopment)
            return;

        // Start PKCE flow — generates code_verifier + code_challenge, stores in session, redirects to /connect/authorize
        app.MapGet("/oauth/demo/start", (
            HttpContext context,
            [FromServices] PkceService pkceService,
            [FromServices] IDataProtectionProvider dataProtectionProvider,
            [FromServices] IConfiguration configuration,
            [FromQuery] string? clientId,
            [FromQuery] string? scope) =>
        {
            var (codeVerifier, codeChallenge) = pkceService.GeneratePkce();
            var state = Guid.NewGuid().ToString("N");

            var store = LoadPkceStateStore(context);
            PruneExpired(store);
            store[state] = new PkceStateEntry
            {
                CodeVerifier = codeVerifier,
                ExpiresUtc = DateTimeOffset.UtcNow.Add(PkceLifetime)
            };
            SavePkceStateStore(context, store);

            // Cookie fallback keeps demo flow resilient when session state is unavailable.
            var protector = dataProtectionProvider.CreateProtector("MCA.OAuthDemo.PkceVerifier");
            var protectedVerifier = protector.Protect(codeVerifier);
            context.Response.Cookies.Append(
                PkceCookiePrefix + state,
                protectedVerifier,
                CreatePkceCookieOptions(context));

            var baseUrl = $"{context.Request.Scheme}://{context.Request.Host}";
            var redirectUri = Uri.EscapeDataString($"{baseUrl}/oauth/demo/callback");

            var effectiveClientId = clientId ?? ResolveWebClientId(configuration);
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

        // Callback — exchanges the auth code for tokens using stored code_verifier
        app.MapGet("/oauth/demo/callback", async (
            HttpContext context,
            [FromQuery] string? code,
            [FromQuery] string? state,
            [FromQuery] string? error,
            [FromServices] IHttpClientFactory httpClientFactory,
            [FromServices] IConfiguration configuration,
            [FromServices] IDataProtectionProvider dataProtectionProvider,
            [FromQuery] string? clientId) =>
        {
            if (!string.IsNullOrEmpty(error))
                return Results.BadRequest(new { error, description = "Authorization was denied or failed." });

            if (string.IsNullOrWhiteSpace(code))
                return Results.BadRequest(new { error = "Missing authorization code." });

            if (string.IsNullOrWhiteSpace(state))
                return Results.BadRequest(new { error = "Missing OAuth state. Start a new flow." });

            var store = LoadPkceStateStore(context);
            PruneExpired(store);
            store.TryGetValue(state, out var entry);
            store.Remove(state);
            SavePkceStateStore(context, store);

            var codeVerifier = entry?.CodeVerifier;
            if (string.IsNullOrWhiteSpace(codeVerifier))
            {
                var cookieKey = PkceCookiePrefix + state;
                if (context.Request.Cookies.TryGetValue(cookieKey, out var protectedVerifier) &&
                    !string.IsNullOrWhiteSpace(protectedVerifier))
                {
                    var protector = dataProtectionProvider.CreateProtector("MCA.OAuthDemo.PkceVerifier");
                    try
                    {
                        codeVerifier = protector.Unprotect(protectedVerifier);
                    }
                    catch
                    {
                        codeVerifier = null;
                    }
                }
            }

            context.Response.Cookies.Delete(PkceCookiePrefix + state);

            if (string.IsNullOrWhiteSpace(codeVerifier))
            {
                return Results.BadRequest(new
                {
                    error = "State mismatch. Possible CSRF attack or missing session.",
                    hint = "Restart with /oauth/demo/start in the same browser tab and ensure cookies are enabled."
                });
            }

            var baseUrl = $"{context.Request.Scheme}://{context.Request.Host}";
            var redirectUri = $"{baseUrl}/oauth/demo/callback";
            var effectiveClientId = clientId ?? ResolveWebClientId(configuration);
            var clientSecret = configuration["OpenIddict:Clients:Web:Secret"] ?? DefaultWebClientSecret;

            // Exchange code for tokens
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

    private static Dictionary<string, PkceStateEntry> LoadPkceStateStore(HttpContext context)
    {
        var json = context.Session.GetString(PkceStateStoreSessionKey);
        if (string.IsNullOrWhiteSpace(json))
            return new Dictionary<string, PkceStateEntry>(StringComparer.Ordinal);

        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, PkceStateEntry>>(json)
                ?? new Dictionary<string, PkceStateEntry>(StringComparer.Ordinal);
        }
        catch
        {
            return new Dictionary<string, PkceStateEntry>(StringComparer.Ordinal);
        }
    }

    private static void SavePkceStateStore(HttpContext context, Dictionary<string, PkceStateEntry> store)
    {
        context.Session.SetString(PkceStateStoreSessionKey, JsonSerializer.Serialize(store));
    }

    private static void PruneExpired(Dictionary<string, PkceStateEntry> store)
    {
        if (store.Count == 0)
            return;

        var now = DateTimeOffset.UtcNow;
        var expiredStates = store
            .Where(pair => pair.Value.ExpiresUtc <= now || string.IsNullOrWhiteSpace(pair.Value.CodeVerifier))
            .Select(pair => pair.Key)
            .ToArray();

        foreach (var expiredState in expiredStates)
            store.Remove(expiredState);
    }

    private static string ResolveWebClientId(IConfiguration configuration)
    {
        var configuredClientId = configuration["OpenIddict:Clients:Web:ClientId"];
        return string.IsNullOrWhiteSpace(configuredClientId) ? DefaultWebClientId : configuredClientId;
    }

    private static CookieOptions CreatePkceCookieOptions(HttpContext context)
    {
        return new CookieOptions
        {
            HttpOnly = true,
            IsEssential = true,
            Secure = context.Request.IsHttps,
            SameSite = SameSiteMode.Lax,
            Expires = DateTimeOffset.UtcNow.Add(PkceLifetime)
        };
    }

    private sealed class PkceStateEntry
    {
        public string CodeVerifier { get; set; } = string.Empty;
        public DateTimeOffset ExpiresUtc { get; set; }
    }
}
#endif
