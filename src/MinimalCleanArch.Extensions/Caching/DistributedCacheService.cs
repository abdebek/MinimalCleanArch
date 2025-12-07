using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace MinimalCleanArch.Extensions.Caching;

/// <summary>
/// Distributed cache implementation of <see cref="ICacheService"/> using IDistributedCache.
/// Supports Redis, SQL Server, and other distributed cache providers.
/// </summary>
public class DistributedCacheService : ICacheService
{
    private readonly IDistributedCache _cache;
    private readonly ILogger<DistributedCacheService> _logger;
    private readonly CacheOptions _options;
    private readonly JsonSerializerOptions _jsonOptions;

    /// <summary>
    /// Initializes a new instance of the <see cref="DistributedCacheService"/> class.
    /// </summary>
    public DistributedCacheService(
        IDistributedCache cache,
        ILogger<DistributedCacheService> logger,
        CacheOptions? options = null)
    {
        _cache = cache;
        _logger = logger;
        _options = options ?? new CacheOptions();
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    /// <inheritdoc />
    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        var fullKey = GetFullKey(key);

        try
        {
            var data = await _cache.GetStringAsync(fullKey, cancellationToken);

            if (data == null)
            {
                _logger.LogDebug("Cache miss for key: {Key}", key);
                return default;
            }

            _logger.LogDebug("Cache hit for key: {Key}", key);
            return JsonSerializer.Deserialize<T>(data, _jsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error getting cache key: {Key}", key);
            return default;
        }
    }

    /// <inheritdoc />
    public async Task<T?> GetOrCreateAsync<T>(
        string key,
        Func<CancellationToken, Task<T>> factory,
        CacheEntryOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var existingValue = await GetAsync<T>(key, cancellationToken);

        if (existingValue != null)
        {
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
    public async Task SetAsync<T>(
        string key,
        T value,
        CacheEntryOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var fullKey = GetFullKey(key);

        try
        {
            var data = JsonSerializer.Serialize(value, _jsonOptions);
            var distributedOptions = CreateDistributedCacheEntryOptions(options);

            await _cache.SetStringAsync(fullKey, data, distributedOptions, cancellationToken);

            _logger.LogDebug("Cache set for key: {Key}", key);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error setting cache key: {Key}", key);
        }
    }

    /// <inheritdoc />
    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        var fullKey = GetFullKey(key);

        try
        {
            await _cache.RemoveAsync(fullKey, cancellationToken);
            _logger.LogDebug("Cache removed for key: {Key}", key);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error removing cache key: {Key}", key);
        }
    }

    /// <inheritdoc />
    public Task RemoveByPatternAsync(string pattern, CancellationToken cancellationToken = default)
    {
        // Note: IDistributedCache doesn't support pattern-based removal natively.
        // For Redis, you would need to use IConnectionMultiplexer directly.
        // This is a limitation - consider using a Redis-specific implementation if needed.
        _logger.LogWarning(
            "Pattern-based removal not supported by IDistributedCache. Pattern: {Pattern}. " +
            "Consider using a Redis-specific implementation.",
            pattern);

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        var fullKey = GetFullKey(key);

        try
        {
            var data = await _cache.GetAsync(fullKey, cancellationToken);
            return data != null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error checking cache key existence: {Key}", key);
            return false;
        }
    }

    /// <inheritdoc />
    public async Task RefreshAsync(string key, CancellationToken cancellationToken = default)
    {
        var fullKey = GetFullKey(key);

        try
        {
            await _cache.RefreshAsync(fullKey, cancellationToken);
            _logger.LogDebug("Cache refreshed for key: {Key}", key);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error refreshing cache key: {Key}", key);
        }
    }

    private string GetFullKey(string key)
    {
        return string.IsNullOrEmpty(_options.KeyPrefix)
            ? key
            : $"{_options.KeyPrefix}:{key}";
    }

    private DistributedCacheEntryOptions CreateDistributedCacheEntryOptions(CacheEntryOptions? options)
    {
        var entryOptions = new DistributedCacheEntryOptions();

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
