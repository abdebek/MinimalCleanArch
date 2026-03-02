using MCA.Application.Commands;
using MCA.Application.Interfaces;
using MinimalCleanArch.Domain.Common;

namespace MCA.Application.Handlers;

public class AuthLogoutHandler(IAuthSessionService authSessionService)
{
    private readonly IAuthSessionService _authSessionService = authSessionService;

    public async Task<Result> Handle(AuthLogoutCommand command, CancellationToken cancellationToken)
    {
        _ = command;
        _ = cancellationToken;

        await _authSessionService.SignOutAsync(cancellationToken);
        return Result.Success();
    }
}
