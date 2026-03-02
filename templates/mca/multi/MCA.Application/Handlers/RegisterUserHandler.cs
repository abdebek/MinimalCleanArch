#if (UseAuth)
using MCA.Application.Commands;
using MCA.Application.Interfaces;
using MCA.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using MinimalCleanArch.Domain.Common;

namespace MCA.Application.Handlers;

public class RegisterUserHandler
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IEmailService _emailService;
    private readonly ILogger<RegisterUserHandler> _logger;

    public RegisterUserHandler(
        UserManager<ApplicationUser> userManager,
        IEmailService emailService,
        ILogger<RegisterUserHandler> logger)
    {
        _userManager = userManager;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task<Result<Guid>> Handle(RegisterUserCommand command, CancellationToken cancellationToken)
    {
        var user = new ApplicationUser(command.FirstName ?? string.Empty, command.LastName ?? string.Empty, command.Email);
#if (UseMessaging)
        user.MarkAsRegistered();
#endif

        var result = await _userManager.CreateAsync(user, command.Password);
        if (!result.Succeeded)
        {
            var errors = string.Join("; ", result.Errors.Select(e => e.Description));
            return Result.Failure<Guid>(new Error("REGISTRATION_FAILED", errors));
        }

#if (!UseMessaging)
        try
        {
            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            await _emailService.SendEmailConfirmationAsync(user.Email!, token, user.Id.ToString());
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send email confirmation for {Email}.", command.Email);
        }
#endif

        return Result.Success(user.Id);
    }
}
#endif
