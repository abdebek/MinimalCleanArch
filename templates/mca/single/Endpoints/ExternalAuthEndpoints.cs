#if (UseAuth)
using MCA.Application.Commands;
using MCA.Application.Handlers;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using MinimalCleanArch.Domain.Common;
using System.Security.Claims;
#if (UseMessaging)
using Wolverine;
#endif

namespace MCA.Endpoints;

/// <summary>
/// External OAuth provider endpoints (Google, Microsoft, GitHub).
/// A provider is enabled when <c>Authentication:{Provider}:ClientId</c> and <c>ClientSecret</c> are set (user-secrets or environment). Empty values keep the scheme unregistered.
/// </summary>
public static class ExternalAuthEndpoints
{
    public static void MapExternalAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var knownProviders = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Google", "Microsoft", "GitHub"
        };

        app.MapGet("/api/auth/external/providers", (HttpContext context) =>
        {
            var configuration = context.RequestServices.GetRequiredService<IConfiguration>();
            var providers = knownProviders
                .Where(name => IsProviderConfigured(configuration, name))
                .ToArray();
            return Results.Ok(new { providers });
        })
        .AllowAnonymous()
        .WithName("ExternalLoginProviders")
        .WithTags("Authentication")
        .WithSummary("List configured external authentication providers");

        app.MapGet("/api/auth/external/{provider}", async (
            string provider,
            [FromQuery] string? returnUrl,
            HttpContext context) =>
        {
            var schemes = await context.RequestServices
                .GetRequiredService<IAuthenticationSchemeProvider>()
                .GetAllSchemesAsync();
            var configuration = context.RequestServices.GetRequiredService<IConfiguration>();
            var scheme = schemes.FirstOrDefault(s =>
                string.Equals(s.Name, provider, StringComparison.OrdinalIgnoreCase)
                && IsProviderConfigured(configuration, s.Name));
            if (scheme is null)
            {
                return Results.NotFound(new
                {
                    error = $"External provider '{provider}' is not configured. Set Authentication:{provider}:ClientId and ClientSecret."
                });
            }

            var redirectUrl = $"/api/auth/external/{scheme.Name}/callback?returnUrl={Uri.EscapeDataString(returnUrl ?? "/")}";
            var properties = new AuthenticationProperties { RedirectUri = redirectUrl };
            return Results.Challenge(properties, new[] { scheme.Name });
        })
        .AllowAnonymous()
        .WithName("ExternalLoginChallenge")
        .WithTags("Authentication")
        .WithSummary("Initiate external provider login (Google, Microsoft, GitHub)");

        app.MapGet("/api/auth/external/{provider}/callback", async (
            string provider,
            [FromQuery] string? returnUrl,
            HttpContext context,
#if (UseMessaging)
            IMessageBus bus,
            CancellationToken cancellationToken) =>
#else
            [FromServices] ExternalAuthSignInHandler handler,
            CancellationToken cancellationToken) =>
#endif
        {
            var result = await context.AuthenticateAsync("ExternalCookie");
            if (!result.Succeeded)
                return Results.BadRequest(new { error = "External authentication failed." });

            var externalUser = result.Principal;
            var email = externalUser.FindFirstValue(ClaimTypes.Email)
                ?? externalUser.FindFirstValue("email")
                ?? externalUser.FindFirstValue("urn:github:email");

            if (string.IsNullOrEmpty(email))
            {
                var message = string.Equals(provider, "GitHub", StringComparison.OrdinalIgnoreCase)
                    ? "GitHub did not provide an email claim. Ensure options.Scope.Add(\"user:email\") is set and the GitHub account has a verified email."
                    : $"Email claim not provided by external provider '{provider}'.";

                return Results.BadRequest(new { error = message });
            }

            var command = new ExternalAuthSignInCommand(
                email,
                provider,
                externalUser.FindFirstValue(ClaimTypes.GivenName),
                externalUser.FindFirstValue(ClaimTypes.Surname),
                returnUrl);
#if (UseMessaging)
            var signInResult = await bus.InvokeAsync<Result<ExternalAuthSignInResult>>(command, cancellationToken);
#else
            var signInResult = await handler.Handle(command, cancellationToken);
#endif
            if (!signInResult.IsSuccess)
                return Results.BadRequest(new { error = signInResult.Error.Message });

            await context.SignOutAsync("ExternalCookie");

            return Results.Redirect(signInResult.Value.RedirectUrl);
        })
        .AllowAnonymous()
        .WithName("ExternalLoginCallback")
        .WithTags("Authentication")
        .WithSummary("Handle external provider OAuth callback");
    }

    private static bool IsProviderConfigured(IConfiguration configuration, string? provider)
    {
        if (string.IsNullOrWhiteSpace(provider))
            return false;

        var clientId = configuration[$"Authentication:{provider}:ClientId"];
        var clientSecret = configuration[$"Authentication:{provider}:ClientSecret"];
        return !string.IsNullOrWhiteSpace(clientId) && !string.IsNullOrWhiteSpace(clientSecret);
    }
}
#endif
