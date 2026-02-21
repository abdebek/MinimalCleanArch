#if (UseAuth)
using MCA.Domain.Entities;
using MinimalCleanArch.Domain.Common;

namespace MCA.Application.Interfaces;

public interface IAuthenticationService
{
    Task<Result<Guid>> RegisterAsync(string email, string password, string? firstName = null, string? lastName = null);
    Task<Result> ChangePasswordAsync(string userId, string currentPassword, string newPassword);
    Task<Result> ConfirmEmailAsync(string userId, string token);
    Task<Result<string>> SendPasswordResetAsync(string email);
    Task<Result> ResetPasswordAsync(string userId, string token, string newPassword);
    Task<Result<string>> GenerateEmailConfirmationTokenAsync(string userId);
}
#endif
