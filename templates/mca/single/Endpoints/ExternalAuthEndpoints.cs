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
/// To enable: uncomment the relevant provider in IdentityServiceExtensions and add the NuGet packages.
/// </summary>
public static class ExternalAuthEndpoints
{
    public static void MapExternalAuthEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/auth/external/{provider}", (
            string provider,
            [FromQuery] string? returnUrl,
            HttpContext context) =>
        {
            var redirectUrl = $"/api/auth/external/{provider}/callback?returnUrl={Uri.EscapeDataString(returnUrl ?? "/")}";
            var properties = new AuthenticationProperties { RedirectUri = redirectUrl };
            return Results.Challenge(properties, new[] { provider });
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

        app.MapGet("/api/auth/external/providers", () =>
        {
            var providers = new[] { "Google", "Microsoft", "GitHub" };
            return Results.Ok(new { providers });
        })
        .AllowAnonymous()
        .WithName("ExternalLoginProviders")
        .WithTags("Authentication")
        .WithSummary("List available external authentication providers");
    }
}
#endif
