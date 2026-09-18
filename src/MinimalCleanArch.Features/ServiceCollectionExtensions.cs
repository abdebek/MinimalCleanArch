using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace MinimalCleanArch.Features;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Binds <c>Features</c> and registers <see cref="IFeatureGate"/>.
    /// Register <see cref="IFeatureStore"/> before or after this call for table backing.
    /// </summary>
    public static IServiceCollection AddFeatures(
        this IServiceCollection services,
        IConfiguration configuration,
        string sectionName = FeatureOptions.SectionName)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddOptions<FeatureOptions>().Bind(configuration.GetSection(sectionName));
        services.TryAddSingleton<IFeatureGate, ConfigurationFeatureGate>();
        return services;
    }
}
