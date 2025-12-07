namespace MinimalCleanArch.Extensions.Caching;

/// <summary>
/// Abstraction for caching operations. Supports both in-memory and distributed caching.
/// </summary>
public interface ICacheService
{
    /// <summary>
    /// Gets a value from the cache.
    /// </summary>
    /// <typeparam name="T">The type of the cached value.</typeparam>
    /// <param name="key">The cache key.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The cached value, or default if not found.</returns>
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a value from the cache, or creates it using the factory if not found.
    /// </summary>
    /// <typeparam name="T">The type of the cached value.</typeparam>
    /// <param name="key">The cache key.</param>
    /// <param name="factory">Factory function to create the value if not cached.</param>
    /// <param name="options">Optional cache entry options.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The cached or newly created value.</returns>
    Task<T?> GetOrCreateAsync<T>(
        string key,
        Func<CancellationToken, Task<T>> factory,
        CacheEntryOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets a value in the cache.
    /// </summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="key">The cache key.</param>
    /// <param name="value">The value to cache.</param>
    /// <param name="options">Optional cache entry options.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SetAsync<T>(
        string key,
        T value,
        CacheEntryOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a value from the cache.
    /// </summary>
    /// <param name="key">The cache key.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task RemoveAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes all values matching the specified pattern.
    /// </summary>
    /// <param name="pattern">The key pattern (supports * wildcard).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task RemoveByPatternAsync(string pattern, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a key exists in the cache.
    /// </summary>
    /// <param name="key">The cache key.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the key exists.</returns>
    Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Refreshes the expiration of a cached item (sliding expiration).
    /// </summary>
    /// <param name="key">The cache key.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task RefreshAsync(string key, CancellationToken cancellationToken = default);
}

/// <summary>
/// Options for cache entries.
/// </summary>
public class CacheEntryOptions
{
    /// <summary>
    /// Gets or sets the absolute expiration time.
    /// </summary>
    public DateTimeOffset? AbsoluteExpiration { get; set; }

    /// <summary>
    /// Gets or sets the absolute expiration relative to now.
    /// </summary>
    public TimeSpan? AbsoluteExpirationRelativeToNow { get; set; }

    /// <summary>
    /// Gets or sets the sliding expiration time.
    /// </summary>
    public TimeSpan? SlidingExpiration { get; set; }

    /// <summary>
    /// Creates options with absolute expiration.
    /// </summary>
    /// <param name="expiration">Time until expiration.</param>
    public static CacheEntryOptions Absolute(TimeSpan expiration) => new()
    {
        AbsoluteExpirationRelativeToNow = expiration
    };

    /// <summary>
    /// Creates options with sliding expiration.
    /// </summary>
    /// <param name="expiration">Sliding expiration time.</param>
    public static CacheEntryOptions Sliding(TimeSpan expiration) => new()
    {
        SlidingExpiration = expiration
    };

    /// <summary>
    /// Creates options with both absolute and sliding expiration.
    /// </summary>
    /// <param name="absoluteExpiration">Absolute expiration time.</param>
    /// <param name="slidingExpiration">Sliding expiration time.</param>
    public static CacheEntryOptions Mixed(TimeSpan absoluteExpiration, TimeSpan slidingExpiration) => new()
    {
        AbsoluteExpirationRelativeToNow = absoluteExpiration,
        SlidingExpiration = slidingExpiration
    };

    /// <summary>
    /// Default short-lived cache (5 minutes).
    /// </summary>
    public static CacheEntryOptions Short => Absolute(TimeSpan.FromMinutes(5));

    /// <summary>
    /// Default medium-lived cache (30 minutes).
    /// </summary>
    public static CacheEntryOptions Medium => Absolute(TimeSpan.FromMinutes(30));

    /// <summary>
    /// Default long-lived cache (1 hour).
    /// </summary>
    public static CacheEntryOptions Long => Absolute(TimeSpan.FromHours(1));
}
