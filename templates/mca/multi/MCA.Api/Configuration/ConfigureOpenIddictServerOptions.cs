using Microsoft.Extensions.Options;
using OpenIddict.Server;

using MCA.Infrastructure.Configuration;

namespace MCA.Api.Configuration;

public class ConfigureOpenIddictServerOptions : IConfigureOptions<OpenIddictServerOptions>
{
    private readonly IOptions<AuthSettings> _authSettings;

    public ConfigureOpenIddictServerOptions(IOptions<AuthSettings> authSettings)
    {
        _authSettings = authSettings;
    }

    public void Configure(OpenIddictServerOptions options)
    {
        var settings = _authSettings.Value;
        if (settings?.TokenLifetimes is not null)
        {
            options.AccessTokenLifetime = settings.TokenLifetimes.AccessToken;
            options.RefreshTokenLifetime = settings.TokenLifetimes.RefreshToken;
            options.AuthorizationCodeLifetime = settings.TokenLifetimes.AuthorizationCode;
        }
    }
}
