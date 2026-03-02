#if (UseAuth && UseMessaging)
using MCA.Application.Interfaces;
using MCA.Domain.Entities;
using MCA.Domain.Events;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace MCA.Application.Handlers;

/// <summary>
/// Handles auth-related domain events.
/// </summary>
public class AuthEventHandler
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IEmailService _emailService;
    private readonly ILogger<AuthEventHandler> _logger;

    public AuthEventHandler(
        UserManager<ApplicationUser> userManager,
        IEmailService emailService,
        ILogger<AuthEventHandler> logger)
    {
        _userManager = userManager;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task Handle(UserRegisteredEvent message, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(message.EntityId.ToString());
        if (user is null || string.IsNullOrWhiteSpace(user.Email))
        {
            _logger.LogWarning("Skipping confirmation email for missing user {UserId}.", message.EntityId);
            return;
        }

        try
        {
            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            await _emailService.SendEmailConfirmationAsync(user.Email, token, user.Id.ToString());
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send email confirmation for user {UserId}.", message.EntityId);
        }
    }
}
#endif
