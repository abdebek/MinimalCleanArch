#if (UseAuth)
using MCA.Application.Commands;
using MCA.Application.Interfaces;
using MCA.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using MinimalCleanArch.Domain.Common;

namespace MCA.Application.Handlers;

public class ForgotPasswordHandler
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IEmailService _emailService;
    private readonly ILogger<ForgotPasswordHandler> _logger;

    public ForgotPasswordHandler(
        UserManager<ApplicationUser> userManager,
        IEmailService emailService,
        ILogger<ForgotPasswordHandler> logger)
    {
        _userManager = userManager;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task<Result<string>> Handle(ForgotPasswordCommand command, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByEmailAsync(command.Email);
        if (user == null)
        {
            // Avoid user enumeration: return success even if user doesn't exist.
            _logger.LogInformation("Password reset requested for non-existent email: {Email}", command.Email);
            return Result.Success(string.Empty);
        }

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);

        try
        {
            await _emailService.SendPasswordResetAsync(user.Email!, token, user.Id.ToString());
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send password reset email to {Email}.", command.Email);
        }

        return Result.Success(token);
    }
}
#endif
