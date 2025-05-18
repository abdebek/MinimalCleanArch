using Microsoft.Extensions.DependencyInjection;
using MinimalCleanArch.Security.Configuration;
using MinimalCleanArch.Security.Encryption;

namespace MinimalCleanArch.Security.Extensions;

/// <summary>
/// Extension methods for <see cref="IServiceCollection"/>
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds encryption services to the service collection
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="encryptionOptions">The encryption options</param>
    /// <returns>The service collection</returns>
    public static IServiceCollection AddEncryption(
        this IServiceCollection services,
        EncryptionOptions encryptionOptions)
    {
        services.AddSingleton(encryptionOptions);
        services.AddSingleton<IEncryptionService, AesEncryptionService>();

        return services;
    }
}
