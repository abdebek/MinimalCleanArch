#if (UseAuth)
using MCA.Domain.Entities;
using MCA.Infrastructure.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using System.Security.Claims;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace MCA.Api.Endpoints;

public static class OpenIddictEndpoints
{
    public static void MapOpenIddictEndpoints(this IEndpointRouteBuilder app)
    {
        // Authorization endpoint
        app.MapMethods("/connect/authorize", new[] { "GET", "POST" }, async (
            HttpContext context,
            [FromServices] UserManager<ApplicationUser> userManager,
            [FromServices] SignInManager<ApplicationUser> signInManager) =>
        {
            var request = context.GetOpenIddictServerRequest() ??
                throw new InvalidOperationException("The OpenID Connect request cannot be retrieved.");

            var result = await context.AuthenticateAsync(IdentityConstants.ApplicationScheme);

            if (result?.Principal == null)
            {
                return Results.Challenge(
                    authenticationSchemes: new[] { IdentityConstants.ApplicationScheme },
                    properties: new AuthenticationProperties
                    {
                        RedirectUri = context.Request.Path + context.Request.QueryString
                    });
            }

            var user = await userManager.GetUserAsync(result.Principal);
            if (user == null)
            {
                return Results.Forbid(
                    authenticationSchemes: new[] { OpenIddictServerAspNetCoreDefaults.AuthenticationScheme },
                    properties: new AuthenticationProperties(new Dictionary<string, string?>
                    {
                        [OpenIddictServerAspNetCoreConstants.Properties.Error] = Errors.InvalidGrant,
                        [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = "User not found."
                    }));
            }

            var principal = await signInManager.CreateUserPrincipalAsync(user);
            principal.SetScopes(request.GetScopes());
            principal.SetResources("mca.api");

            foreach (var claim in principal.Claims)
                claim.SetDestinations(GetDestinations(claim, principal));

            return Results.SignIn(principal, properties: null, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        })
        .AllowAnonymous()
        .WithName("Authorize")
        .WithTags("OpenIddict");

        // Token endpoint
        app.MapPost("/connect/token", async (
            HttpContext context,
            [FromServices] UserManager<ApplicationUser> userManager,
            [FromServices] SignInManager<ApplicationUser> signInManager) =>
        {
            var request = context.GetOpenIddictServerRequest() ??
                throw new InvalidOperationException("The OpenID Connect request cannot be retrieved.");

            // Password grant
            if (request.IsPasswordGrantType())
            {
                var user = await userManager.FindByNameAsync(request.Username!)
                    ?? await userManager.FindByEmailAsync(request.Username!);

                if (user == null)
                {
                    return Results.Forbid(
                        authenticationSchemes: new[] { OpenIddictServerAspNetCoreDefaults.AuthenticationScheme },
                        properties: new AuthenticationProperties(new Dictionary<string, string?>
                        {
                            [OpenIddictServerAspNetCoreConstants.Properties.Error] = Errors.InvalidGrant,
                            [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = "Invalid credentials."
                        }));
                }

                var result = await signInManager.CheckPasswordSignInAsync(user, request.Password!, lockoutOnFailure: true);
                if (!result.Succeeded)
                {
                    return Results.Forbid(
                        authenticationSchemes: new[] { OpenIddictServerAspNetCoreDefaults.AuthenticationScheme },
                        properties: new AuthenticationProperties(new Dictionary<string, string?>
                        {
                            [OpenIddictServerAspNetCoreConstants.Properties.Error] = Errors.InvalidGrant,
                            [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = "Invalid credentials."
                        }));
                }

                var principal = await signInManager.CreateUserPrincipalAsync(user);
                principal.SetScopes(request.GetScopes());
                principal.SetResources("mca.api");

                foreach (var claim in principal.Claims)
                    claim.SetDestinations(GetDestinations(claim, principal));

                return Results.SignIn(principal, properties: null, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
            }

            // Authorization code grant
            if (request.IsAuthorizationCodeGrantType())
            {
                var result = await context.AuthenticateAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
                var principal = result.Principal!;

                var userId = principal.GetClaim(Claims.Subject);
                var user = await userManager.FindByIdAsync(userId!);

                if (user == null)
                {
                    return Results.Forbid(
                        authenticationSchemes: new[] { OpenIddictServerAspNetCoreDefaults.AuthenticationScheme },
                        properties: new AuthenticationProperties(new Dictionary<string, string?>
                        {
                            [OpenIddictServerAspNetCoreConstants.Properties.Error] = Errors.InvalidGrant,
                            [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = "User no longer exists."
                        }));
                }

                principal = await signInManager.CreateUserPrincipalAsync(user);
                principal.SetScopes(request.GetScopes());
                principal.SetResources("mca.api");

                foreach (var claim in principal.Claims)
                    claim.SetDestinations(GetDestinations(claim, principal));

                return Results.SignIn(principal, properties: null, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
            }

            // Refresh token grant
            if (request.IsRefreshTokenGrantType())
            {
                var result = await context.AuthenticateAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
                var principal = result.Principal!;

                var userId = principal.GetClaim(Claims.Subject);
                var user = await userManager.FindByIdAsync(userId!);

                if (user == null)
                {
                    return Results.Forbid(
                        authenticationSchemes: new[] { OpenIddictServerAspNetCoreDefaults.AuthenticationScheme },
                        properties: new AuthenticationProperties(new Dictionary<string, string?>
                        {
                            [OpenIddictServerAspNetCoreConstants.Properties.Error] = Errors.InvalidGrant,
                            [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = "User no longer exists."
                        }));
                }

                principal = await signInManager.CreateUserPrincipalAsync(user);
                principal.SetScopes(request.GetScopes());
                principal.SetResources("mca.api");

                foreach (var claim in principal.Claims)
                    claim.SetDestinations(GetDestinations(claim, principal));

                return Results.SignIn(principal, properties: null, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
            }

            return Results.Problem("The specified grant type is not supported.");
        })
        .AllowAnonymous()
        .WithName("Token")
        .WithTags("OpenIddict");

        // UserInfo endpoint
        app.MapMethods("/connect/userinfo", new[] { "GET", "POST" }, async (
            HttpContext context,
            [FromServices] UserManager<ApplicationUser> userManager) =>
        {
            var result = await context.AuthenticateAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
            if (result?.Principal == null)
                return Results.Challenge(authenticationSchemes: new[] { OpenIddictServerAspNetCoreDefaults.AuthenticationScheme });

            var userId = result.Principal.GetClaim(Claims.Subject);
            if (string.IsNullOrEmpty(userId))
                return Results.BadRequest(new { error = "Invalid user ID in token" });

            var user = await userManager.FindByIdAsync(userId);
            if (user == null)
                return Results.NotFound(new { error = "User not found" });

            var claims = new Dictionary<string, object> { [Claims.Subject] = userId };
            var scopes = result.Principal.GetScopes();

            if (scopes.Contains(Scopes.Profile))
            {
                claims[Claims.Name] = user.UserName ?? string.Empty;
                claims[Claims.PreferredUsername] = user.UserName ?? string.Empty;
                claims["given_name"] = user.FirstName;
                claims["family_name"] = user.LastName;
            }

            if (scopes.Contains(Scopes.Email))
            {
                claims[Claims.Email] = user.Email ?? string.Empty;
                claims[Claims.EmailVerified] = user.EmailConfirmed;
            }

            if (scopes.Contains(Scopes.Roles))
            {
                var roles = result.Principal.FindAll(Claims.Role).Select(c => c.Value).ToArray();
                if (roles.Length > 0) claims[Claims.Role] = roles;
            }

            return Results.Ok(claims);
        })
        .AllowAnonymous()
        .WithName("UserInfo")
        .WithTags("OpenIddict");

        // Logout endpoint
        app.MapPost("/connect/logout", async (
            HttpContext context,
            [FromServices] SignInManager<ApplicationUser> signInManager) =>
        {
            await signInManager.SignOutAsync();
            return Results.SignOut(authenticationSchemes: new[] { OpenIddictServerAspNetCoreDefaults.AuthenticationScheme });
        })
        .AllowAnonymous()
        .WithName("Logout")
        .WithTags("OpenIddict");
    }

    private static OpenIddictRequest? GetOpenIddictServerRequest(this HttpContext context)
    {
        var feature = context.Features.Get<OpenIddictServerAspNetCoreFeature>();
        return feature?.Transaction?.Request;
    }

    private static IEnumerable<string> GetDestinations(Claim claim, ClaimsPrincipal? principal = null)
    {
        switch (claim.Type)
        {
            case Claims.Name:
            case Claims.Email:
            case Claims.Role:
            case "given_name":
            case "family_name":
                yield return Destinations.AccessToken;
                if (principal?.HasScope(Scopes.OpenId) ?? false)
                    yield return Destinations.IdentityToken;
                break;

            default:
                yield return Destinations.AccessToken;
                break;
        }
    }
}
#endif
