using Microsoft.Extensions.DependencyInjection;

namespace MinimalCleanArch.Extensions.Caching;

/// <summary>
/// Extension methods for adding caching services.
/// </summary>
public static class CachingExtensions
{
    /// <summary>
    /// Adds in-memory caching services.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Optional action to configure cache options.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddMinimalCleanArchCaching(
        this IServiceCollection services,
        Action<CacheOptions>? configure = null)
    {
        var options = new CacheOptions();
        configure?.Invoke(options);

        services.AddMemoryCache();
        services.AddSingleton(options);
        services.AddSingleton<ICacheService, MemoryCacheService>();
        services.AddSingleton<ICachedRepositoryFactory, CachedRepositoryFactory>();

        return services;
    }

    /// <summary>
    /// Adds a custom cache service implementation.
    /// </summary>
    /// <typeparam name="TCacheService">The cache service implementation type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Optional action to configure cache options.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddMinimalCleanArchCaching<TCacheService>(
        this IServiceCollection services,
        Action<CacheOptions>? configure = null)
        where TCacheService : class, ICacheService
    {
        var options = new CacheOptions();
        configure?.Invoke(options);

        services.AddSingleton(options);
        services.AddSingleton<ICacheService, TCacheService>();
        services.AddSingleton<ICachedRepositoryFactory, CachedRepositoryFactory>();

        return services;
    }

    /// <summary>
    /// Adds distributed caching services using IDistributedCache.
    /// Requires a distributed cache provider (Redis, SQL Server, etc.) to be registered.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Optional action to configure cache options.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddMinimalCleanArchDistributedCaching(
        this IServiceCollection services,
        Action<CacheOptions>? configure = null)
    {
        var options = new CacheOptions();
        configure?.Invoke(options);

        services.AddSingleton(options);
        services.AddSingleton<ICacheService, DistributedCacheService>();
        services.AddSingleton<ICachedRepositoryFactory, CachedRepositoryFactory>();

        return services;
    }
}

/// <summary>
/// Extension methods for cache key generation.
/// </summary>
public static class CacheKeyExtensions
{
    /// <summary>
    /// Generates a cache key for an entity by type and ID.
    /// </summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <param name="id">The entity ID.</param>
    /// <returns>A cache key string.</returns>
    public static string EntityKey<TEntity>(object id)
    {
        return $"entity:{typeof(TEntity).Name}:{id}";
    }

    /// <summary>
    /// Generates a cache key for a list of entities.
    /// </summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <param name="suffix">Optional suffix for the key.</param>
    /// <returns>A cache key string.</returns>
    public static string EntityListKey<TEntity>(string? suffix = null)
    {
        var key = $"entities:{typeof(TEntity).Name}";
        return suffix != null ? $"{key}:{suffix}" : key;
    }

    /// <summary>
    /// Generates a cache key pattern for all entities of a type.
    /// </summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <returns>A cache key pattern with wildcard.</returns>
    public static string EntityPattern<TEntity>()
    {
        return $"entity:{typeof(TEntity).Name}:*";
    }

    /// <summary>
    /// Generates a cache key for a query result.
    /// </summary>
    /// <param name="queryName">The query name.</param>
    /// <param name="parameters">Query parameters.</param>
    /// <returns>A cache key string.</returns>
    public static string QueryKey(string queryName, params object[] parameters)
    {
        var paramStr = string.Join(":", parameters.Select(p => p?.ToString() ?? "null"));
        return $"query:{queryName}:{paramStr}";
    }

    /// <summary>
    /// Generates a cache key for user-specific data.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="dataType">The type of data.</param>
    /// <returns>A cache key string.</returns>
    public static string UserKey(string userId, string dataType)
    {
        return $"user:{userId}:{dataType}";
    }
}
