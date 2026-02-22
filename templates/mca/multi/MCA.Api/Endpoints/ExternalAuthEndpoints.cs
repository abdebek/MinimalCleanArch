#if (UseAuth)
using MCA.Domain.Entities;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace MCA.Api.Endpoints;

/// <summary>
/// External OAuth provider endpoints (Google, Microsoft, GitHub).
/// To enable: uncomment the relevant provider in IdentityServiceExtensions and add the NuGet packages.
/// </summary>
public static class ExternalAuthEndpoints
{
    public static void MapExternalAuthEndpoints(this IEndpointRouteBuilder app)
    {
        // Challenge external provider — redirects user to provider's login page
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

        // External provider callback
        app.MapGet("/api/auth/external/{provider}/callback", async (
            string provider,
            [FromQuery] string? returnUrl,
            HttpContext context,
            [FromServices] SignInManager<ApplicationUser> signInManager,
            [FromServices] UserManager<ApplicationUser> userManager) =>
        {
            // Authenticate via the external cookie set by the provider handler
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

            // Find or create the user
            var user = await userManager.FindByEmailAsync(email);
            if (user == null)
            {
                var firstName = externalUser.FindFirstValue(ClaimTypes.GivenName) ?? string.Empty;
                var lastName = externalUser.FindFirstValue(ClaimTypes.Surname) ?? string.Empty;

                user = new ApplicationUser(firstName, lastName, email);
                var createResult = await userManager.CreateAsync(user);
                if (!createResult.Succeeded)
                {
                    var errors = string.Join("; ", createResult.Errors.Select(e => e.Description));
                    return Results.BadRequest(new { error = errors });
                }
            }

            // Sign in via the application cookie
            await signInManager.SignInAsync(user, isPersistent: false);
            await context.SignOutAsync("ExternalCookie");

            var redirect = !string.IsNullOrEmpty(returnUrl) && Uri.IsWellFormedUriString(returnUrl, UriKind.Relative)
                ? returnUrl
                : "/";

            return Results.Redirect(redirect);
        })
        .AllowAnonymous()
        .WithName("ExternalLoginCallback")
        .WithTags("Authentication")
        .WithSummary("Handle external provider OAuth callback");

        // List available external providers
        app.MapGet("/api/auth/external/providers", (
            [FromServices] IAuthenticationSchemeProvider schemeProvider) =>
        {
            // Return all schemes that are not the default application/cookie scheme
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
