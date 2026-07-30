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
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;

        // App-relative path only (query/fragment allowed for OpenIddict authorize return URLs).
        // Reject protocol-relative ("//evil") and anything that is not a well-formed relative URI.
        // Note: do not use UriKind.Absolute rejection — on Unix "/path" parses as file:///path.
        return value.StartsWith('/')
            && !value.StartsWith("//", StringComparison.Ordinal)
            && Uri.IsWellFormedUriString(value, UriKind.Relative);
    }
}
