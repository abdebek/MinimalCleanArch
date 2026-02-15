#if (UseAuth)
using MCA.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using MinimalCleanArch.Domain.Common;

namespace MCA.Infrastructure.Services;

public interface IAuthenticationService
{
    Task<Result<Guid>> RegisterAsync(string email, string password, string? firstName = null, string? lastName = null);
    Task<Result> ChangePasswordAsync(string userId, string currentPassword, string newPassword);
}

public class AuthenticationService : IAuthenticationService
{
    private readonly UserManager<ApplicationUser> _userManager;

    public AuthenticationService(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
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
}
#endif
