#if (UseAuth)
using MCA.Application.Interfaces;
using MCA.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using MinimalCleanArch.Domain.Common;

namespace MCA.Infrastructure.Services;

public class AuthenticationService : IAuthenticationService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IEmailService _emailService;
    private readonly ILogger<AuthenticationService> _logger;

    public AuthenticationService(
        UserManager<ApplicationUser> userManager,
        IEmailService emailService,
        ILogger<AuthenticationService> logger)
    {
        _userManager = userManager;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task<Result<Guid>> RegisterAsync(string email, string password, string? firstName = null, string? lastName = null)
    {
        var user = new ApplicationUser(firstName ?? string.Empty, lastName ?? string.Empty, email);
        var result = await _userManager.CreateAsync(user, password);

        if (!result.Succeeded)
        {
            var errors = string.Join("; ", result.Errors.Select(e => e.Description));
            return Result.Failure<Guid>(new Error("REGISTRATION_FAILED", errors));
        }

        return Result.Success(user.Id);
    }

    public async Task<Result> ChangePasswordAsync(string userId, string currentPassword, string newPassword)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return Result.Failure(new Error("NOT_FOUND", "User not found"));

        var result = await _userManager.ChangePasswordAsync(user, currentPassword, newPassword);
        return result.Succeeded
            ? Result.Success()
            : Result.Failure(new Error("CHANGE_FAILED", string.Join("; ", result.Errors.Select(e => e.Description))));
    }

    public async Task<Result> ConfirmEmailAsync(string userId, string token)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return Result.Failure(new Error("NOT_FOUND", "User not found"));

        var result = await _userManager.ConfirmEmailAsync(user, token);
        return result.Succeeded
            ? Result.Success()
            : Result.Failure(new Error("CONFIRM_FAILED", string.Join("; ", result.Errors.Select(e => e.Description))));
    }

    public async Task<Result<string>> SendPasswordResetAsync(string email)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user == null)
        {
            // Avoid user enumeration: return success even if user doesn't exist
            _logger.LogInformation("Password reset requested for non-existent email: {Email}", email);
            return Result.Success(string.Empty);
        }

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);

        try
        {
            await _emailService.SendPasswordResetAsync(user.Email!, token, user.Id.ToString());
        }
        catch (Exception ex)
        {
            // Log and continue — no SMTP configured in development
            _logger.LogWarning(ex, "Failed to send password reset email to {Email}. Token returned for dev convenience.", email);
        }

        // Return token for dev convenience when SMTP is not configured
        _logger.LogInformation("Password reset token for {Email}: {Token}", email, token);
        return Result.Success(token);
    }

    public async Task<Result> ResetPasswordAsync(string userId, string token, string newPassword)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return Result.Failure(new Error("NOT_FOUND", "User not found"));

        var result = await _userManager.ResetPasswordAsync(user, token, newPassword);
        return result.Succeeded
            ? Result.Success()
            : Result.Failure(new Error("RESET_FAILED", string.Join("; ", result.Errors.Select(e => e.Description))));
    }

    public async Task<Result<string>> GenerateEmailConfirmationTokenAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return Result.Failure<string>(new Error("NOT_FOUND", "User not found"));

        var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
        return Result.Success(token);
    }
}
#endif
