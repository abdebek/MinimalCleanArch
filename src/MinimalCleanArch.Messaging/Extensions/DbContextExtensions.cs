using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MinimalCleanArch.Messaging.Middleware;

namespace MinimalCleanArch.Messaging.Extensions;

/// <summary>
/// Extension methods for configuring DbContext with domain event publishing.
/// </summary>
public static class DbContextExtensions
{
    /// <summary>
    /// Adds the domain event publishing interceptor to the DbContext options.
    /// </summary>
    /// <param name="optionsBuilder">The DbContext options builder.</param>
    /// <param name="serviceProvider">The service provider to resolve the interceptor.</param>
    /// <returns>The options builder for chaining.</returns>
    public static DbContextOptionsBuilder UseDomainEventPublishing(
        this DbContextOptionsBuilder optionsBuilder,
        IServiceProvider serviceProvider)
    {
        var interceptor = serviceProvider.GetRequiredService<DomainEventPublishingInterceptor>();
        optionsBuilder.AddInterceptors(interceptor);
        return optionsBuilder;
    }

    /// <summary>
    /// Registers the domain event publishing interceptor in the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddDomainEventPublishing(this IServiceCollection services)
    {
        services.AddScoped<DomainEventPublishingInterceptor>();
        services.AddScoped<IDomainEventPublisher, WolverineDomainEventPublisher>();
        return services;
    }
}
