using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MinimalCleanArch.Realtime;

namespace MinimalCleanArch.Extensions.Realtime;

public static class RealtimeExtensions
{
    /// <summary>
    /// Adds SignalR and replaces <see cref="IRealtimePublisher"/> with the hub adapter.
    /// </summary>
    public static IServiceCollection AddMinimalCleanArchRealtime(this IServiceCollection services)
    {
        services.AddRealtime();
        services.AddSignalR();
        services.Replace(ServiceDescriptor.Singleton<IRealtimePublisher, SignalRRealtimePublisher>());
        return services;
    }

    /// <summary>
    /// Maps <see cref="RealtimeHub"/> at <see cref="RealtimeHub.Path"/>.
    /// </summary>
    public static IEndpointRouteBuilder MapMinimalCleanArchRealtime(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapHub<RealtimeHub>(RealtimeHub.Path);
        return endpoints;
    }
}
