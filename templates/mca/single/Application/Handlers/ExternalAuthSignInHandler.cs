using MCA.Application.Commands;
using MCA.Application.Interfaces;
using MCA.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using MinimalCleanArch.Domain.Common;

namespace MCA.Application.Handlers;

public class ExternalAuthSignInHandler(
    UserManager<ApplicationUser> userManager,
    IAuthSessionService authSessionService)
{
    private readonly UserManager<ApplicationUser> _userManager = userManager;
    private readonly IAuthSessionService _authSessionService = authSessionService;

    public async Task<Result<ExternalAuthSignInResult>> Handle(
        ExternalAuthSignInCommand command,
        CancellationToken cancellationToken)
    {
        _ = cancellationToken;

        if (string.IsNullOrWhiteSpace(command.Email))
        {
            return Result.Failure<ExternalAuthSignInResult>(new Error(
                "MISSING_EMAIL",
                $"Email claim not provided by external provider '{command.Provider}'."));
        }

        var user = await _userManager.FindByEmailAsync(command.Email);
        if (user is null)
        {
            user = new ApplicationUser(
                command.FirstName ?? string.Empty,
                command.LastName ?? string.Empty,
                command.Email);

            var createResult = await _userManager.CreateAsync(user);
            if (!createResult.Succeeded)
            {
                var errors = string.Join("; ", createResult.Errors.Select(e => e.Description));
                return Result.Failure<ExternalAuthSignInResult>(
                    new Error("EXTERNAL_SIGN_IN_FAILED", errors));
            }
        }

        await _authSessionService.SignInAsync(user, isPersistent: false, cancellationToken);

        var redirect = IsSafeRelativeUrl(command.ReturnUrl) ? command.ReturnUrl! : "/";
        return Result.Success(new ExternalAuthSignInResult(redirect));
    }

    private static bool IsSafeRelativeUrl(string? value)
        => !string.IsNullOrWhiteSpace(value) && Uri.IsWellFormedUriString(value, UriKind.Relative);
}
