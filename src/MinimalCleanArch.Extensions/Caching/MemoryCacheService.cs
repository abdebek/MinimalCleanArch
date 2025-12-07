using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace MinimalCleanArch.Extensions.Caching;

/// <summary>
/// In-memory implementation of <see cref="ICacheService"/> using IMemoryCache.
/// </summary>
public class MemoryCacheService : ICacheService
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<MemoryCacheService> _logger;
    private readonly CacheOptions _options;

    // Track keys for pattern-based removal (IMemoryCache doesn't support this natively)
    private readonly ConcurrentDictionary<string, byte> _keys = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="MemoryCacheService"/> class.
    /// </summary>
    public MemoryCacheService(
        IMemoryCache cache,
        ILogger<MemoryCacheService> logger,
        CacheOptions? options = null)
    {
        _cache = cache;
        _logger = logger;
        _options = options ?? new CacheOptions();
    }

    /// <inheritdoc />
    public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        var fullKey = GetFullKey(key);

        if (_cache.TryGetValue(fullKey, out T? value))
        {
            _logger.LogDebug("Cache hit for key: {Key}", key);
            return Task.FromResult(value);
        }

        _logger.LogDebug("Cache miss for key: {Key}", key);
        return Task.FromResult(default(T?));
    }

    /// <inheritdoc />
    public async Task<T?> GetOrCreateAsync<T>(
        string key,
        Func<CancellationToken, Task<T>> factory,
        CacheEntryOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var fullKey = GetFullKey(key);

        if (_cache.TryGetValue(fullKey, out T? existingValue))
        {
            _logger.LogDebug("Cache hit for key: {Key}", key);
            return existingValue;
        }

        _logger.LogDebug("Cache miss for key: {Key}, creating value", key);

        var value = await factory(cancellationToken);

        if (value != null)
        {
            await SetAsync(key, value, options, cancellationToken);
        }

        return value;
    }

    /// <inheritdoc />
    public Task SetAsync<T>(
        string key,
        T value,
        CacheEntryOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var fullKey = GetFullKey(key);
        var entryOptions = CreateMemoryCacheEntryOptions(options);

        // Register callback to remove key from tracking on eviction
        entryOptions.RegisterPostEvictionCallback((evictedKey, _, _, _) =>
        {
            _keys.TryRemove(evictedKey.ToString()!, out _);
            _logger.LogDebug("Cache entry evicted: {Key}", evictedKey);
        });

        _cache.Set(fullKey, value, entryOptions);
        _keys.TryAdd(fullKey, 0);

        _logger.LogDebug("Cache set for key: {Key}", key);

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        var fullKey = GetFullKey(key);

        _cache.Remove(fullKey);
        _keys.TryRemove(fullKey, out _);

        _logger.LogDebug("Cache removed for key: {Key}", key);

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task RemoveByPatternAsync(string pattern, CancellationToken cancellationToken = default)
    {
        var fullPattern = GetFullKey(pattern);
        var regex = new Regex(
            "^" + Regex.Escape(fullPattern).Replace("\\*", ".*") + "$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        var keysToRemove = _keys.Keys.Where(k => regex.IsMatch(k)).ToList();

        foreach (var key in keysToRemove)
        {
            _cache.Remove(key);
            _keys.TryRemove(key, out _);
        }

        _logger.LogDebug("Cache removed {Count} entries matching pattern: {Pattern}", keysToRemove.Count, pattern);

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        var fullKey = GetFullKey(key);
        return Task.FromResult(_cache.TryGetValue(fullKey, out _));
    }

    /// <inheritdoc />
    public Task RefreshAsync(string key, CancellationToken cancellationToken = default)
    {
        // IMemoryCache automatically handles sliding expiration on Get
        // This is a no-op for memory cache but included for interface compatibility
        var fullKey = GetFullKey(key);
        _cache.TryGetValue(fullKey, out _);
        return Task.CompletedTask;
    }

    private string GetFullKey(string key)
    {
        return string.IsNullOrEmpty(_options.KeyPrefix)
            ? key
            : $"{_options.KeyPrefix}:{key}";
    }

    private MemoryCacheEntryOptions CreateMemoryCacheEntryOptions(CacheEntryOptions? options)
    {
        var entryOptions = new MemoryCacheEntryOptions();

        if (options != null)
        {
            if (options.AbsoluteExpiration.HasValue)
                entryOptions.AbsoluteExpiration = options.AbsoluteExpiration;

            if (options.AbsoluteExpirationRelativeToNow.HasValue)
                entryOptions.AbsoluteExpirationRelativeToNow = options.AbsoluteExpirationRelativeToNow;

            if (options.SlidingExpiration.HasValue)
                entryOptions.SlidingExpiration = options.SlidingExpiration;
        }
        else if (_options.DefaultExpiration.HasValue)
        {
            entryOptions.AbsoluteExpirationRelativeToNow = _options.DefaultExpiration;
        }

        return entryOptions;
    }
}

/// <summary>
/// Configuration options for the cache service.
/// </summary>
public class CacheOptions
{
    /// <summary>
    /// Gets or sets the key prefix for all cache entries. Default: empty.
    /// </summary>
    public string KeyPrefix { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the default expiration time. Default: 30 minutes.
    /// </summary>
    public TimeSpan? DefaultExpiration { get; set; } = TimeSpan.FromMinutes(30);

    /// <summary>
    /// Gets or sets whether to enable cache logging. Default: true in development.
    /// </summary>
    public bool EnableLogging { get; set; } = true;
}
