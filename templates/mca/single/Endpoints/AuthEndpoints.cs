#if (UseAuth)
using MCA.Infrastructure.Services;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace MCA.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this IEndpointRouteBuilder app)
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
    }
}

public record RegisterRequest(string Email, string Password, string? FirstName = null, string? LastName = null);
public record ChangePasswordRequest(string CurrentPassword, string NewPassword);
#endif
