#if (UseAuth)
namespace MCA.Application.Interfaces;

public interface ITokenService
{
    Task RevokeAllTokensAsync(string userId, CancellationToken cancellationToken = default);
}
#endif
