#if (UseAuth)
using MCA.Application.Interfaces;
using MCA.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace MCA.Api.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this IEndpointRouteBuilder app, bool isDevelopment = false)
    {
        var auth = app.MapGroup("/api/auth").WithTags("Authentication");

        auth.MapPost("/register", async (
            [FromBody] RegisterRequest request,
            [FromServices] IAuthenticationService authService) =>
        {
            var result = await authService.RegisterAsync(request.Email, request.Password, request.FirstName, request.LastName);
            return result.IsSuccess
                ? Results.Ok(new { message = "User registered successfully", userId = result.Value })
                : Results.BadRequest(new { error = result.Error.Message });
        })
        .AllowAnonymous()
        .WithName("Register")
        .WithSummary("Register a new user");

        auth.MapPost("/change-password", async (
            HttpContext httpContext,
            [FromBody] ChangePasswordRequest request,
            [FromServices] IAuthenticationService authService) =>
        {
            var userId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return Results.Unauthorized();

            var result = await authService.ChangePasswordAsync(userId, request.CurrentPassword, request.NewPassword);
            return result.IsSuccess
                ? Results.Ok(new { message = "Password changed successfully" })
                : Results.BadRequest(new { error = result.Error.Message });
        })
        .RequireAuthorization()
        .WithName("ChangePassword")
        .WithSummary("Change the current user's password");

        auth.MapPost("/confirm-email", async (
            [FromBody] ConfirmEmailRequest request,
            [FromServices] IAuthenticationService authService) =>
        {
            var result = await authService.ConfirmEmailAsync(request.UserId, request.Token);
            return result.IsSuccess
                ? Results.Ok(new { message = "Email confirmed successfully" })
                : Results.BadRequest(new { error = result.Error.Message });
        })
        .AllowAnonymous()
        .WithName("ConfirmEmail")
        .WithSummary("Confirm a user's email address");

        auth.MapPost("/forgot-password", async (
            [FromBody] ForgotPasswordRequest request,
            [FromServices] IAuthenticationService authService) =>
        {
            var result = await authService.SendPasswordResetAsync(request.Email);
            // Always return 200 to prevent user enumeration
            // In development, the reset token is returned for convenience
            return Results.Ok(new
            {
                message = "If an account exists for that email, a password reset link has been sent.",
                // Remove the 'token' field in production — only here for dev convenience
                token = result.IsSuccess ? result.Value : null
            });
        })
        .AllowAnonymous()
        .WithName("ForgotPassword")
        .WithSummary("Request a password reset email");

        auth.MapPost("/reset-password", async (
            [FromBody] ResetPasswordRequest request,
            [FromServices] IAuthenticationService authService) =>
        {
            var result = await authService.ResetPasswordAsync(request.UserId, request.Token, request.NewPassword);
            return result.IsSuccess
                ? Results.Ok(new { message = "Password reset successfully" })
                : Results.BadRequest(new { error = result.Error.Message });
        })
        .AllowAnonymous()
        .WithName("ResetPassword")
        .WithSummary("Reset a user's password using a reset token");

        // SSR login — signs in via cookie for the authorization code flow
        auth.MapPost("/login", async (
            HttpContext httpContext,
            [FromBody] LoginRequest request,
            [FromServices] SignInManager<ApplicationUser> signInManager,
            [FromServices] UserManager<ApplicationUser> userManager) =>
        {
            var user = await userManager.FindByEmailAsync(request.Email)
                ?? await userManager.FindByNameAsync(request.Email);

            if (user == null)
                return Results.Unauthorized();

            var result = await signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);

            if (result.IsLockedOut)
                return Results.Problem("Account is locked out.", statusCode: 423);

            if (!result.Succeeded)
                return Results.Unauthorized();

            await signInManager.SignInAsync(user, isPersistent: false);

            var returnUrl = request.ReturnUrl;
            if (!string.IsNullOrEmpty(returnUrl) && Uri.IsWellFormedUriString(returnUrl, UriKind.Relative))
                return Results.Ok(new { message = "Signed in", redirectUrl = returnUrl });

            return Results.Ok(new { message = "Signed in" });
        })
        .AllowAnonymous()
        .WithName("Login")
        .WithSummary("Sign in via cookie (for SSR/authorization code flow)");

        auth.MapPost("/logout", async (
            HttpContext httpContext,
            [FromServices] SignInManager<ApplicationUser> signInManager) =>
        {
            await signInManager.SignOutAsync();
            return Results.Ok(new { message = "Signed out" });
        })
        .AllowAnonymous()
        .WithName("CookieLogout")
        .WithSummary("Sign out and clear the authentication cookie");

        if (isDevelopment)
        {
            // SSR login page — GET /auth/login (development only)
            // OpenIddict redirects here when an unauthenticated user hits /connect/authorize.
            // In production, replace LoginPath in IdentityServiceExtensions with your real frontend login URL.
            app.MapGet("/auth/login", ([FromQuery] string? returnUrl, [FromQuery] string? error) =>
            {
                var errorHtml = string.IsNullOrEmpty(error)
                    ? ""
                    : $"<p class=\"error\">{System.Net.WebUtility.HtmlEncode(error)}</p>";

                var safeReturnUrl = !string.IsNullOrEmpty(returnUrl)
                    && Uri.IsWellFormedUriString(returnUrl, UriKind.Relative)
                    ? System.Net.WebUtility.HtmlEncode(returnUrl)
                    : "";

                var html = LoginPage.Html
                    .Replace("{errorHtml}", errorHtml)
                    .Replace("{returnUrl}", safeReturnUrl);

                return Results.Content(html, "text/html");
            })
            .AllowAnonymous()
            .ExcludeFromDescription();

            // SSR login form handler — POST /auth/login (development only)
            app.MapPost("/auth/login", async (
                HttpContext context,
                [FromServices] SignInManager<ApplicationUser> signInManager,
                [FromServices] UserManager<ApplicationUser> userManager) =>
            {
                var form = await context.Request.ReadFormAsync();
                var email = form["email"].ToString();
                var password = form["password"].ToString();
                var returnUrl = form["returnUrl"].ToString();

                var user = await userManager.FindByEmailAsync(email)
                    ?? await userManager.FindByNameAsync(email);

                if (user == null)
                    return Results.Redirect(
                        $"/auth/login?error=Invalid+credentials&returnUrl={Uri.EscapeDataString(returnUrl)}");

                var result = await signInManager.CheckPasswordSignInAsync(user, password, lockoutOnFailure: true);

                if (result.IsLockedOut)
                    return Results.Redirect("/auth/login?error=Account+is+locked+out");

                if (!result.Succeeded)
                    return Results.Redirect(
                        $"/auth/login?error=Invalid+credentials&returnUrl={Uri.EscapeDataString(returnUrl)}");

                await signInManager.SignInAsync(user, isPersistent: false);

                if (!string.IsNullOrEmpty(returnUrl) && Uri.IsWellFormedUriString(returnUrl, UriKind.Relative))
                    return Results.Redirect(returnUrl);

                return Results.Redirect("/");
            })
            .AllowAnonymous()
            .ExcludeFromDescription();
        }
    }

    private static class LoginPage
    {
        public const string Html = """
            <!DOCTYPE html>
            <html lang="en">
            <head>
                <meta charset="utf-8">
                <meta name="viewport" content="width=device-width, initial-scale=1">
                <title>Sign In</title>
                <style>
                    *, *::before, *::after { box-sizing: border-box; margin: 0; padding: 0; }
                    body { font-family: system-ui, sans-serif; background: #f5f5f5; display: flex; align-items: center; justify-content: center; min-height: 100vh; }
                    .card { background: #fff; border-radius: 8px; box-shadow: 0 2px 8px rgba(0,0,0,.12); padding: 2rem; width: 100%; max-width: 380px; }
                    h1 { font-size: 1.4rem; margin-bottom: 1.5rem; }
                    label { display: block; font-size: .875rem; margin-bottom: .25rem; color: #444; }
                    input[type=email], input[type=password] { width: 100%; padding: .5rem .75rem; border: 1px solid #ccc; border-radius: 4px; font-size: 1rem; margin-bottom: 1rem; }
                    input:focus { outline: none; border-color: #4f46e5; box-shadow: 0 0 0 2px rgba(79,70,229,.2); }
                    button { width: 100%; padding: .6rem; background: #4f46e5; color: #fff; border: none; border-radius: 4px; font-size: 1rem; cursor: pointer; }
                    button:hover { background: #4338ca; }
                    .error { color: #dc2626; font-size: .875rem; margin-bottom: 1rem; }
                    .providers { margin-top: 1rem; display: grid; gap: .5rem; }
                    .provider-button { display: block; width: 100%; text-align: center; text-decoration: none; padding: .6rem; border: 1px solid #d1d5db; color: #111827; border-radius: 4px; font-size: .95rem; background: #fff; }
                    .provider-button:hover { background: #f9fafb; }
                </style>
            </head>
            <body>
            <div class="card">
                <h1>Sign In</h1>
                {errorHtml}
                <form method="post" action="/auth/login">
                    <label for="email">Email</label>
                    <input id="email" name="email" type="email" autocomplete="username" required>
                    <label for="password">Password</label>
                    <input id="password" name="password" type="password" autocomplete="current-password" required>
                    <input type="hidden" name="returnUrl" value="{returnUrl}">
                    <button type="submit">Sign in</button>
                </form>
                <!-- External providers (optional): enable provider handlers first in IdentityServiceExtensions, then uncomment. -->
                <!--
                <div class="providers">
                    <a class="provider-button" href="/api/auth/external/Google?returnUrl={returnUrl}">Continue with Google</a>
                    <a class="provider-button" href="/api/auth/external/Microsoft?returnUrl={returnUrl}">Continue with Microsoft</a>
                    <a class="provider-button" href="/api/auth/external/GitHub?returnUrl={returnUrl}">Continue with GitHub</a>
                </div>
                -->
            </div>
            </body>
            </html>
            """;
    }
}

public record RegisterRequest(string Email, string Password, string? FirstName = null, string? LastName = null);
public record ChangePasswordRequest(string CurrentPassword, string NewPassword);
public record ConfirmEmailRequest(string UserId, string Token);
public record ForgotPasswordRequest(string Email);
public record ResetPasswordRequest(string UserId, string Token, string NewPassword);
public record LoginRequest(string Email, string Password, string? ReturnUrl = null);
#endif
