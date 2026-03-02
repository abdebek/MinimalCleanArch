#if (UseAuth)
using MCA.Application.Commands;
using MCA.Application.Handlers;
using Microsoft.AspNetCore.Mvc;
using MinimalCleanArch.Domain.Common;
using System.Security.Claims;
#if (UseMessaging)
using Wolverine;
#endif
#if (UseRateLimiting)
using MinimalCleanArch.Extensions.RateLimiting;
#endif

namespace MCA.Api.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this IEndpointRouteBuilder app, bool isDevelopment = false)
    {
        var auth = app.MapGroup("/api/auth").WithTags("Authentication");

        auth.MapPost("/register", async (
            [FromBody] RegisterRequest request,
#if (UseMessaging)
            IMessageBus bus,
            CancellationToken cancellationToken) =>
#else
            [FromServices] RegisterUserHandler handler,
            CancellationToken cancellationToken) =>
#endif
        {
            var command = new RegisterUserCommand(request.Email, request.Password, request.FirstName, request.LastName);
#if (UseMessaging)
            var result = await bus.InvokeAsync<Result<Guid>>(command, cancellationToken);
#else
            var result = await handler.Handle(command, cancellationToken);
#endif
            return result.IsSuccess
                ? Results.Ok(new { message = "User registered successfully", userId = result.Value })
                : Results.BadRequest(new { error = result.Error.Message });
        })
        .AllowAnonymous()
#if (UseRateLimiting)
        .RequireRateLimiting(RateLimitingExtensions.TokenBucketPolicy)
#endif
        .WithName("Register")
        .WithSummary("Register a new user");

        auth.MapPost("/change-password", async (
            HttpContext httpContext,
            [FromBody] ChangePasswordRequest request,
#if (UseMessaging)
            IMessageBus bus,
            CancellationToken cancellationToken) =>
#else
            [FromServices] ChangePasswordHandler handler,
            CancellationToken cancellationToken) =>
#endif
        {
            var userId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return Results.Unauthorized();

            var command = new ChangePasswordCommand(userId, request.CurrentPassword, request.NewPassword);
#if (UseMessaging)
            var result = await bus.InvokeAsync<Result>(command, cancellationToken);
#else
            var result = await handler.Handle(command, cancellationToken);
#endif
            return result.IsSuccess
                ? Results.Ok(new { message = "Password changed successfully" })
                : Results.BadRequest(new { error = result.Error.Message });
        })
        .RequireAuthorization()
#if (UseRateLimiting)
        .RequireRateLimiting(RateLimitingExtensions.FixedPolicy)
#endif
        .WithName("ChangePassword")
        .WithSummary("Change the current user's password");

        auth.MapPost("/confirm-email", async (
            [FromBody] ConfirmEmailRequest request,
#if (UseMessaging)
            IMessageBus bus,
            CancellationToken cancellationToken) =>
#else
            [FromServices] ConfirmEmailHandler handler,
            CancellationToken cancellationToken) =>
#endif
        {
            var command = new ConfirmEmailCommand(request.UserId, request.Token);
#if (UseMessaging)
            var result = await bus.InvokeAsync<Result>(command, cancellationToken);
#else
            var result = await handler.Handle(command, cancellationToken);
#endif
            return result.IsSuccess
                ? Results.Ok(new { message = "Email confirmed successfully" })
                : Results.BadRequest(new { error = result.Error.Message });
        })
        .AllowAnonymous()
        .WithName("ConfirmEmail")
        .WithSummary("Confirm a user's email address");

        auth.MapPost("/forgot-password", async (
            [FromBody] ForgotPasswordRequest request,
#if (UseMessaging)
            IMessageBus bus,
            CancellationToken cancellationToken) =>
#else
            [FromServices] ForgotPasswordHandler handler,
            CancellationToken cancellationToken) =>
#endif
        {
            // Always return 200 to prevent user enumeration
            var command = new ForgotPasswordCommand(request.Email);
#if (UseMessaging)
            await bus.InvokeAsync<Result<string>>(command, cancellationToken);
#else
            await handler.Handle(command, cancellationToken);
#endif
            return Results.Ok(new
            {
                message = "If an account exists for that email, a password reset link has been sent."
            });
        })
        .AllowAnonymous()
#if (UseRateLimiting)
        .RequireRateLimiting(RateLimitingExtensions.TokenBucketPolicy)
#endif
        .WithName("ForgotPassword")
        .WithSummary("Request a password reset email");

        auth.MapPost("/reset-password", async (
            [FromBody] ResetPasswordRequest request,
#if (UseMessaging)
            IMessageBus bus,
            CancellationToken cancellationToken) =>
#else
            [FromServices] ResetPasswordHandler handler,
            CancellationToken cancellationToken) =>
#endif
        {
            var command = new ResetPasswordCommand(request.UserId, request.Token, request.NewPassword);
#if (UseMessaging)
            var result = await bus.InvokeAsync<Result>(command, cancellationToken);
#else
            var result = await handler.Handle(command, cancellationToken);
#endif
            return result.IsSuccess
                ? Results.Ok(new { message = "Password reset successfully" })
                : Results.BadRequest(new { error = result.Error.Message });
        })
        .AllowAnonymous()
#if (UseRateLimiting)
        .RequireRateLimiting(RateLimitingExtensions.TokenBucketPolicy)
#endif
        .WithName("ResetPassword")
        .WithSummary("Reset a user's password using a reset token");

        // SSR login — signs in via cookie for the authorization code flow
        auth.MapPost("/login", async (
            [FromBody] LoginRequest request,
#if (UseMessaging)
            IMessageBus bus,
            CancellationToken cancellationToken) =>
#else
            [FromServices] AuthLoginHandler handler,
            CancellationToken cancellationToken) =>
#endif
        {
            var command = new AuthLoginCommand(request.Email, request.Password, request.ReturnUrl);
#if (UseMessaging)
            var result = await bus.InvokeAsync<Result<AuthLoginResult>>(command, cancellationToken);
#else
            var result = await handler.Handle(command, cancellationToken);
#endif
            if (result.IsSuccess)
            {
                return string.IsNullOrWhiteSpace(result.Value.RedirectUrl)
                    ? Results.Ok(new { message = "Signed in" })
                    : Results.Ok(new { message = "Signed in", redirectUrl = result.Value.RedirectUrl });
            }

            if (result.Error.Code == "LOCKED_OUT")
                return Results.Problem("Account is locked out.", statusCode: 423);

            return Results.Unauthorized();
        })
        .AllowAnonymous()
#if (UseRateLimiting)
        .RequireRateLimiting(RateLimitingExtensions.TokenBucketPolicy)
#endif
        .WithName("Login")
        .WithSummary("Sign in via cookie (for SSR/authorization code flow)");

        auth.MapPost("/logout", async (
#if (UseMessaging)
            IMessageBus bus,
            CancellationToken cancellationToken) =>
#else
            [FromServices] AuthLogoutHandler handler,
            CancellationToken cancellationToken) =>
#endif
        {
#if (UseMessaging)
            await bus.InvokeAsync<Result>(new AuthLogoutCommand(), cancellationToken);
#else
            await handler.Handle(new AuthLogoutCommand(), cancellationToken);
#endif
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
                    ? returnUrl
                    : "/";

                var safeReturnUrlHtml = System.Net.WebUtility.HtmlEncode(safeReturnUrl);
                var safeReturnUrlQuery = Uri.EscapeDataString(safeReturnUrl);

                var html = LoginPage.Html
                    .Replace("{errorHtml}", errorHtml)
                    .Replace("{returnUrl}", safeReturnUrlHtml)
                    .Replace("{returnUrlQuery}", safeReturnUrlQuery);

                return Results.Content(html, "text/html");
            })
            .AllowAnonymous()
            .ExcludeFromDescription();

            // SSR login form handler — POST /auth/login (development only)
            app.MapPost("/auth/login", async (
                HttpContext context,
#if (UseMessaging)
                IMessageBus bus,
                CancellationToken cancellationToken) =>
#else
                [FromServices] AuthLoginHandler handler,
                CancellationToken cancellationToken) =>
#endif
            {
                var form = await context.Request.ReadFormAsync();
                var email = form["email"].ToString();
                var password = form["password"].ToString();
                var returnUrl = form["returnUrl"].ToString();

                var command = new AuthLoginCommand(email, password, returnUrl);
#if (UseMessaging)
                var result = await bus.InvokeAsync<Result<AuthLoginResult>>(command, cancellationToken);
#else
                var result = await handler.Handle(command, cancellationToken);
#endif
                if (result.IsSuccess)
                {
                    return Results.Redirect(result.Value.RedirectUrl ?? "/");
                }

                if (result.Error.Code == "LOCKED_OUT")
                    return Results.Redirect("/auth/login?error=Account+is+locked+out");

                return Results.Redirect(
                    $"/auth/login?error=Invalid+credentials&returnUrl={Uri.EscapeDataString(returnUrl)}");
            })
            .AllowAnonymous()
#if (UseRateLimiting)
            .RequireRateLimiting(RateLimitingExtensions.TokenBucketPolicy)
#endif
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
                    <a class="provider-button" href="/api/auth/external/Google?returnUrl={returnUrlQuery}">Continue with Google</a>
                    <a class="provider-button" href="/api/auth/external/Microsoft?returnUrl={returnUrlQuery}">Continue with Microsoft</a>
                    <a class="provider-button" href="/api/auth/external/GitHub?returnUrl={returnUrlQuery}">Continue with GitHub</a>
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
