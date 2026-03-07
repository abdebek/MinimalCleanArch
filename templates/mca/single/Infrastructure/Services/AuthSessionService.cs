#if (UseAuth)
using MCA.Application.Interfaces;
using MCA.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using MinimalCleanArch.Domain.Common;

namespace MCA.Infrastructure.Services;

public class AuthSessionService(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager) : IAuthSessionService
{
    private readonly UserManager<ApplicationUser> _userManager = userManager;
    private readonly SignInManager<ApplicationUser> _signInManager = signInManager;

    public async Task<Result<ApplicationUser>> ValidateCredentialsAsync(
        string emailOrUserName,
        string password,
        CancellationToken cancellationToken)
    {
        _ = cancellationToken;

        var user = await _userManager.FindByEmailAsync(emailOrUserName)
            ?? await _userManager.FindByNameAsync(emailOrUserName);
        if (user is null)
        {
            return Result.Failure<ApplicationUser>(new Error("INVALID_CREDENTIALS", "Invalid credentials"));
        }

        var result = await _signInManager.CheckPasswordSignInAsync(user, password, lockoutOnFailure: true);
        if (result.IsLockedOut)
        {
            return Result.Failure<ApplicationUser>(new Error("LOCKED_OUT", "Account is locked out."));
        }

        if (!result.Succeeded)
        {
            return Result.Failure<ApplicationUser>(new Error("INVALID_CREDENTIALS", "Invalid credentials"));
        }

        return Result.Success(user);
    }

    public async Task SignInAsync(ApplicationUser user, bool isPersistent, CancellationToken cancellationToken)
    {
        _ = cancellationToken;
        await _signInManager.SignInAsync(user, isPersistent);
    }

    public async Task SignOutAsync(CancellationToken cancellationToken)
    {
        _ = cancellationToken;
        await _signInManager.SignOutAsync();
    }
}
#endif
