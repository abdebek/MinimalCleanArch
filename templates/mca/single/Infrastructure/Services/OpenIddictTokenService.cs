#if (UseAuth)
using MCA.Application.Interfaces;
using OpenIddict.Abstractions;

namespace MCA.Infrastructure.Services;

public class OpenIddictTokenService : ITokenService
{
    private readonly IOpenIddictTokenManager _tokenManager;

    public OpenIddictTokenService(IOpenIddictTokenManager tokenManager)
    {
        _tokenManager = tokenManager;
    }

    public async Task RevokeAllTokensAsync(string userId, CancellationToken cancellationToken = default)
    {
        await foreach (var token in _tokenManager.FindBySubjectAsync(userId, cancellationToken))
        {
            await _tokenManager.TryRevokeAsync(token, cancellationToken);
        }
    }
}
#endif
