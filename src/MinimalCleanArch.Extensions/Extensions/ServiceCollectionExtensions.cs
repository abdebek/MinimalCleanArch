using Microsoft.Extensions.DependencyInjection;
using FluentValidation;
using System.Reflection;
using MinimalCleanArch.Extensions.HealthChecks;
using MinimalCleanArch.Extensions.Middlewares;

namespace MinimalCleanArch.Extensions.Extensions;

/// <summary>
/// Extension methods for <see cref="IServiceCollection"/> for MinimalCleanArch Extensions
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds MinimalCleanArch extensions to the service collection.
    /// Includes validators, correlation ID accessor, and startup health check.
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection</returns>
    public static IServiceCollection AddMinimalCleanArchExtensions(this IServiceCollection services)
    {
        // Register validators from the Extensions assembly
        services.AddValidatorsFromAssemblyContaining<ServiceCollectionExtensionsMarker>();

        // Register correlation ID accessor
        services.AddHttpContextAccessor();
        services.AddScoped<ICorrelationIdAccessor, CorrelationIdAccessor>();

        // Register startup health check as singleton so it can be marked complete
        services.AddSingleton<StartupHealthCheck>();

        return services;
    }

    /// <summary>
    /// Adds validators from the specified assembly to the service collection
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="assembly">The assembly to scan for validators</param>
    /// <returns>The service collection</returns>
    public static IServiceCollection AddValidatorsFromAssembly(
        this IServiceCollection services,
        Assembly assembly)
    {
        // Register all validators in the assembly
        var validatorType = typeof(IValidator<>);
        var validatorTypes = assembly.GetTypes()
            .Where(t => t.GetInterfaces().Any(i =>
                i.IsGenericType &&
                i.GetGenericTypeDefinition() == validatorType));

        foreach (var validator in validatorTypes)
        {
            var interfaces = validator.GetInterfaces()
                .Where(i => i.IsGenericType &&
                          i.GetGenericTypeDefinition() == validatorType);

            foreach (var @interface in interfaces)
            {
                services.AddScoped(@interface, validator);
            }
        }

        return services;
    }

    /// <summary>
    /// Adds all validators from the assembly containing the specified type to the service collection
    /// </summary>
    /// <typeparam name="T">The type whose assembly should be scanned for validators</typeparam>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection</returns>
    public static IServiceCollection AddValidatorsFromAssemblyContaining<T>(
        this IServiceCollection services)
    {
        return services.AddValidatorsFromAssembly(typeof(T).Assembly);
    }
}

/// <summary>
/// Marker class for assembly scanning
/// </summary>
internal class ServiceCollectionExtensionsMarker
{
}