using MCA.Application.Commands;
using MCA.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using MinimalCleanArch.Domain.Common;

namespace MCA.Application.Handlers;

public class ChangePasswordHandler
{
    private readonly UserManager<ApplicationUser> _userManager;

    public ChangePasswordHandler(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task<Result> Handle(ChangePasswordCommand command, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByIdAsync(command.UserId);
        if (user == null)
        {
            return Result.Failure(new Error("NOT_FOUND", "User not found"));
        }

        var result = await _userManager.ChangePasswordAsync(user, command.CurrentPassword, command.NewPassword);
        return result.Succeeded
            ? Result.Success()
            : Result.Failure(new Error("CHANGE_FAILED", string.Join("; ", result.Errors.Select(e => e.Description))));
    }
}
