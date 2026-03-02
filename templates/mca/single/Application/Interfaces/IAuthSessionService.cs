#if (UseAuth)
using MCA.Domain.Entities;
using MinimalCleanArch.Domain.Common;

namespace MCA.Application.Interfaces;

public interface IAuthSessionService
{
    Task<Result<ApplicationUser>> ValidateCredentialsAsync(
        string emailOrUserName,
        string password,
        CancellationToken cancellationToken);

    Task SignInAsync(ApplicationUser user, bool isPersistent, CancellationToken cancellationToken);

    Task SignOutAsync(CancellationToken cancellationToken);
}
#endif
