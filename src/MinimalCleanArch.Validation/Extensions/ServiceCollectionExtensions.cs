using System.Reflection;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using MinimalCleanArchExtensions = MinimalCleanArch.Extensions.Extensions.ServiceCollectionExtensions;

namespace MinimalCleanArch.Validation.Extensions;

/// <summary>
/// Extension methods for <see cref="IServiceCollection"/> related to validation
/// </summary>
public static class ServiceCollectionExtensions
{
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
        return MinimalCleanArchExtensions.AddValidatorsFromAssembly(services, assembly);
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
        return MinimalCleanArchExtensions.AddValidatorsFromAssemblyContaining<T>(services);
    }

    /// <summary>
    /// Adds validation services and registers validators from the specified assemblies.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="assemblies">Assemblies to scan for validators.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddValidation(
        this IServiceCollection services,
        params Assembly[] assemblies)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(assemblies);

        foreach (var assembly in assemblies.Where(a => a != null).Distinct())
        {
            MinimalCleanArchExtensions.AddValidatorsFromAssembly(services, assembly);
        }

        return services;
    }

    /// <summary>
    /// Adds validation services and registers validators from the assembly containing the specified type.
    /// </summary>
    /// <typeparam name="T">A type from the assembly to scan.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddValidationFromAssemblyContaining<T>(
        this IServiceCollection services)
    {
        return services.AddValidation(typeof(T).Assembly);
    }

    /// <summary>
    /// Adds MinimalCleanArch validation services and registers validators from the specified assemblies.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="assemblies">Assemblies to scan for validators.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddMinimalCleanArchValidation(
        this IServiceCollection services,
        params Assembly[] assemblies)
    {
        return services.AddValidation(assemblies);
    }

    /// <summary>
    /// Adds MinimalCleanArch validation services and registers validators from the assembly containing the specified type.
    /// </summary>
    /// <typeparam name="T">A type from the assembly to scan.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddMinimalCleanArchValidationFromAssemblyContaining<T>(
        this IServiceCollection services)
    {
        return services.AddValidationFromAssemblyContaining<T>();
    }
}
