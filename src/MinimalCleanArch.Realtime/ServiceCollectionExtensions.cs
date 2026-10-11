using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace MinimalCleanArch.Realtime;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="IRealtimePublisher"/> as a no-op until a host adapter replaces it.
    /// </summary>
    public static IServiceCollection AddRealtime(this IServiceCollection services)
    {
        services.TryAddSingleton<IRealtimePublisher, NoOpRealtimePublisher>();
        return services;
    }
}
