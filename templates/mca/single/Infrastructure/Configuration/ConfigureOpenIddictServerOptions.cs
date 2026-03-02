using Microsoft.Extensions.Options;
using OpenIddict.Server;

namespace MCA.Infrastructure.Configuration;

public class ConfigureOpenIddictServerOptions : IConfigureOptions<OpenIddictServerOptions>
{
    private readonly IOptions<OpenIddictSettings> _settings;

    public ConfigureOpenIddictServerOptions(IOptions<OpenIddictSettings> settings)
    {
        _settings = settings;
    }

    public void Configure(OpenIddictServerOptions options)
    {
        var lifetimes = _settings.Value?.TokenLifetimes;
        if (lifetimes is not null)
        {
            options.AccessTokenLifetime = lifetimes.AccessToken;
            options.RefreshTokenLifetime = lifetimes.RefreshToken;
            options.AuthorizationCodeLifetime = lifetimes.AuthorizationCode;
            options.IdentityTokenLifetime = lifetimes.IdentityToken;
        }
    }
}
