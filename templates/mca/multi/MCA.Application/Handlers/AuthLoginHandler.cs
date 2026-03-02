using MCA.Application.Commands;
using MCA.Application.Interfaces;
using MinimalCleanArch.Domain.Common;

namespace MCA.Application.Handlers;

public class AuthLoginHandler(
    IAuthSessionService authSessionService)
{
    private readonly IAuthSessionService _authSessionService = authSessionService;

    public async Task<Result<AuthLoginResult>> Handle(AuthLoginCommand command, CancellationToken cancellationToken)
    {
        _ = cancellationToken;

        var validateResult = await _authSessionService.ValidateCredentialsAsync(
            command.Email,
            command.Password,
            cancellationToken);
        if (!validateResult.IsSuccess)
        {
            return Result.Failure<AuthLoginResult>(validateResult.Error);
        }

        await _authSessionService.SignInAsync(validateResult.Value, isPersistent: false, cancellationToken);

        var redirectUrl = IsSafeRelativeUrl(command.ReturnUrl) ? command.ReturnUrl : null;
        return Result.Success(new AuthLoginResult(redirectUrl));
    }

    private static bool IsSafeRelativeUrl(string? value)
        => !string.IsNullOrWhiteSpace(value) && Uri.IsWellFormedUriString(value, UriKind.Relative);
}
