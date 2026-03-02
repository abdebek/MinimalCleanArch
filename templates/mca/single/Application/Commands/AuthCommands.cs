#if (UseAuth)
namespace MCA.Application.Commands;

public record RegisterUserCommand(
    string Email,
    string Password,
    string? FirstName = null,
    string? LastName = null);

public record ChangePasswordCommand(string UserId, string CurrentPassword, string NewPassword);

public record ConfirmEmailCommand(string UserId, string Token);

public record ForgotPasswordCommand(string Email);

public record ResetPasswordCommand(string UserId, string Token, string NewPassword);
#endif
