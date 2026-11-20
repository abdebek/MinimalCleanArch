using System.Linq.Expressions;
using MinimalCleanArch.Domain.Entities;
using MinimalCleanArch.Execution;
using MinimalCleanArch.Repositories;
using MinimalCleanArch.Specifications;

namespace MinimalCleanArch.Extensions.Caching;

/// <summary>
/// Decorator that adds tenant-aware caching to any repository implementation.
/// </summary>
/// <typeparam name="TEntity">The entity type.</typeparam>
/// <typeparam name="TKey">The entity key type.</typeparam>
public class CachedRepositoryDecorator<TEntity, TKey> : IRepository<TEntity, TKey>
    where TEntity : IEntity<TKey>
    where TKey : IEquatable<TKey>
{
    private static readonly bool IsTenantScoped = typeof(ITenantEntity).IsAssignableFrom(typeof(TEntity));

    private readonly IRepository<TEntity, TKey> _innerRepository;
    private readonly ICacheService _cache;
    private readonly CacheEntryOptions _cacheOptions;
    private readonly IExecutionContext? _executionContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="CachedRepositoryDecorator{TEntity, TKey}"/> class.
    /// </summary>
    /// <param name="innerRepository">The repository to decorate.</param>
    /// <param name="cache">The cache service.</param>
    /// <param name="cacheOptions">Optional cache entry options.</param>
    /// <param name="executionContext">Optional execution context for tenant-scoped cache keys.</param>
    public CachedRepositoryDecorator(
        IRepository<TEntity, TKey> innerRepository,
        ICacheService cache,
        CacheEntryOptions? cacheOptions = null,
        IExecutionContext? executionContext = null)
    {
        _innerRepository = innerRepository ?? throw new ArgumentNullException(nameof(innerRepository));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _cacheOptions = cacheOptions ?? CacheEntryOptions.Medium;
        _executionContext = executionContext;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<TEntity>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        if (ShouldBypassCache())
        {
            return await _innerRepository.GetAllAsync(cancellationToken);
        }

        var key = BuildListKey("all");

        return await _cache.GetOrCreateAsync(
            key,
            async ct => (await _innerRepository.GetAllAsync(ct)).ToList(),
            _cacheOptions,
            cancellationToken) ?? new List<TEntity>();
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<TEntity>> GetAsync(
        Expression<Func<TEntity, bool>> filter,
        CancellationToken cancellationToken = default)
    {
        // Don't cache filtered queries by default - they could be too specific
        return _innerRepository.GetAsync(filter, cancellationToken);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<TEntity>> GetAsync(
        ISpecification<TEntity> specification,
        CancellationToken cancellationToken = default)
    {
        // Don't cache specification queries by default - they could be too specific
        return _innerRepository.GetAsync(specification, cancellationToken);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<TResult>> GetAsync<TResult>(
        ISpecification<TEntity, TResult> specification,
        CancellationToken cancellationToken = default)
    {
        return _innerRepository.GetAsync(specification, cancellationToken);
    }

    /// <inheritdoc />
    public Task<bool> AnyAsync(
        Expression<Func<TEntity, bool>> filter,
        CancellationToken cancellationToken = default)
    {
        return _innerRepository.AnyAsync(filter, cancellationToken);
    }

    /// <inheritdoc />
    public Task<bool> AnyAsync(
        ISpecification<TEntity> specification,
        CancellationToken cancellationToken = default)
    {
        return _innerRepository.AnyAsync(specification, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<TEntity?> GetByIdAsync(TKey id, CancellationToken cancellationToken = default)
    {
        if (ShouldBypassCache())
        {
            return await _innerRepository.GetByIdAsync(id, cancellationToken);
        }

        var key = BuildEntityKey(id);

        return await _cache.GetOrCreateAsync(
            key,
            ct => _innerRepository.GetByIdAsync(id, ct)!,
            _cacheOptions,
            cancellationToken);
    }

    /// <inheritdoc />
    public Task<TEntity?> GetFirstAsync(
        Expression<Func<TEntity, bool>> filter,
        CancellationToken cancellationToken = default)
    {
        return _innerRepository.GetFirstAsync(filter, cancellationToken);
    }

    /// <inheritdoc />
    public Task<TEntity?> GetFirstAsync(
        ISpecification<TEntity> specification,
        CancellationToken cancellationToken = default)
    {
        return _innerRepository.GetFirstAsync(specification, cancellationToken);
    }

    /// <inheritdoc />
    public Task<TResult?> GetFirstAsync<TResult>(
        ISpecification<TEntity, TResult> specification,
        CancellationToken cancellationToken = default)
    {
        return _innerRepository.GetFirstAsync(specification, cancellationToken);
    }

    /// <inheritdoc />
    public Task<TEntity?> SingleOrDefaultAsync(
        Expression<Func<TEntity, bool>> filter,
        CancellationToken cancellationToken = default)
    {
        return _innerRepository.SingleOrDefaultAsync(filter, cancellationToken);
    }

    /// <inheritdoc />
    public Task<TEntity?> SingleOrDefaultAsync(
        ISpecification<TEntity> specification,
        CancellationToken cancellationToken = default)
    {
        return _innerRepository.SingleOrDefaultAsync(specification, cancellationToken);
    }

    /// <inheritdoc />
    public Task<int> CountAsync(
        Expression<Func<TEntity, bool>>? filter = null,
        CancellationToken cancellationToken = default)
    {
        return _innerRepository.CountAsync(filter, cancellationToken);
    }

    /// <inheritdoc />
    public Task<int> CountAsync(
        ISpecification<TEntity> specification,
        CancellationToken cancellationToken = default)
    {
        return _innerRepository.CountAsync(specification, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<TEntity> AddAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        var result = await _innerRepository.AddAsync(entity, cancellationToken);

        await InvalidateListCacheAsync(cancellationToken);

        return result;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<TEntity>> AddRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default)
    {
        var result = await _innerRepository.AddRangeAsync(entities, cancellationToken);

        await InvalidateListCacheAsync(cancellationToken);

        return result;
    }

    /// <inheritdoc />
    public async Task<TEntity> UpdateAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        var result = await _innerRepository.UpdateAsync(entity, cancellationToken);

        await InvalidateEntityCacheAsync(entity.Id, cancellationToken);
        await InvalidateListCacheAsync(cancellationToken);

        return result;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<TEntity>> UpdateRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default)
    {
        var entityList = entities.ToList();
        var result = await _innerRepository.UpdateRangeAsync(entityList, cancellationToken);

        foreach (var entity in entityList)
        {
            await InvalidateEntityCacheAsync(entity.Id, cancellationToken);
        }
        await InvalidateListCacheAsync(cancellationToken);

        return result;
    }

    /// <inheritdoc />
    public async Task DeleteAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        await _innerRepository.DeleteAsync(entity, cancellationToken);

        await InvalidateEntityCacheAsync(entity.Id, cancellationToken);
        await InvalidateListCacheAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task DeleteAsync(TKey id, CancellationToken cancellationToken = default)
    {
        await _innerRepository.DeleteAsync(id, cancellationToken);

        await InvalidateEntityCacheAsync(id, cancellationToken);
        await InvalidateListCacheAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task DeleteRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default)
    {
        var entityList = entities.ToList();
        await _innerRepository.DeleteRangeAsync(entityList, cancellationToken);

        foreach (var entity in entityList)
        {
            await InvalidateEntityCacheAsync(entity.Id, cancellationToken);
        }
        await InvalidateListCacheAsync(cancellationToken);
    }

    private bool ShouldBypassCache() =>
        IsTenantScoped && string.IsNullOrWhiteSpace(_executionContext?.TenantId);

    private string? GetTenantPrefix()
    {
        var tenantId = _executionContext?.TenantId;
        return string.IsNullOrWhiteSpace(tenantId) ? null : $"tenant:{tenantId}";
    }

    private string BuildEntityKey(TKey id)
    {
        var baseKey = CacheKeyExtensions.EntityKey<TEntity>(id);
        var tenantPrefix = GetTenantPrefix();
        return tenantPrefix is null ? baseKey : $"{tenantPrefix}:{baseKey}";
    }

    private string BuildListKey(string suffix)
    {
        var baseKey = CacheKeyExtensions.EntityListKey<TEntity>(suffix);
        var tenantPrefix = GetTenantPrefix();
        return tenantPrefix is null ? baseKey : $"{tenantPrefix}:{baseKey}";
    }

    private Task InvalidateEntityCacheAsync(TKey id, CancellationToken cancellationToken)
    {
        if (ShouldBypassCache())
        {
            return Task.CompletedTask;
        }

        return _cache.RemoveAsync(BuildEntityKey(id), cancellationToken);
    }

    private async Task InvalidateListCacheAsync(CancellationToken cancellationToken)
    {
        if (ShouldBypassCache())
        {
            return;
        }

        // Remove the exact "all" key first so IDistributedCache providers (Redis, SQL)
        // invalidate GetAllAsync() even though they do not support pattern removal.
        await _cache.RemoveAsync(BuildListKey("all"), cancellationToken);
        await _cache.RemoveByPatternAsync(BuildListKey("*"), cancellationToken);
    }
}

/// <summary>
/// Factory for creating cached repository decorators.
/// </summary>
public interface ICachedRepositoryFactory
{
    /// <summary>
    /// Creates a cached repository decorator for the specified repository.
    /// </summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <typeparam name="TKey">The key type.</typeparam>
    /// <param name="repository">The repository to decorate.</param>
    /// <param name="cacheOptions">Optional cache options.</param>
    /// <returns>A cached repository decorator.</returns>
    IRepository<TEntity, TKey> Create<TEntity, TKey>(
        IRepository<TEntity, TKey> repository,
        CacheEntryOptions? cacheOptions = null)
        where TEntity : IEntity<TKey>
        where TKey : IEquatable<TKey>;
}

/// <summary>
/// Default implementation of <see cref="ICachedRepositoryFactory"/>.
/// </summary>
public class CachedRepositoryFactory : ICachedRepositoryFactory
{
    private readonly ICacheService _cache;
    private readonly IExecutionContext? _executionContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="CachedRepositoryFactory"/> class.
    /// </summary>
    /// <param name="cache">The cache service.</param>
    /// <param name="executionContext">Optional execution context for tenant-scoped caching.</param>
    public CachedRepositoryFactory(ICacheService cache, IExecutionContext? executionContext = null)
    {
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _executionContext = executionContext;
    }

    /// <inheritdoc />
    public IRepository<TEntity, TKey> Create<TEntity, TKey>(
        IRepository<TEntity, TKey> repository,
        CacheEntryOptions? cacheOptions = null)
        where TEntity : IEntity<TKey>
        where TKey : IEquatable<TKey>
    {
        return new CachedRepositoryDecorator<TEntity, TKey>(repository, _cache, cacheOptions, _executionContext);
    }
}
