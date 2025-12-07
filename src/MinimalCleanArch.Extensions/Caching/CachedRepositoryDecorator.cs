using MinimalCleanArch.Domain.Entities;
using MinimalCleanArch.Repositories;
using MinimalCleanArch.Specifications;
using System.Linq.Expressions;

namespace MinimalCleanArch.Extensions.Caching;

/// <summary>
/// Decorator that adds caching to any repository implementation.
/// </summary>
/// <typeparam name="TEntity">The entity type.</typeparam>
/// <typeparam name="TKey">The entity key type.</typeparam>
public class CachedRepositoryDecorator<TEntity, TKey> : IRepository<TEntity, TKey>
    where TEntity : IEntity<TKey>
    where TKey : IEquatable<TKey>
{
    private readonly IRepository<TEntity, TKey> _innerRepository;
    private readonly ICacheService _cache;
    private readonly CacheEntryOptions _cacheOptions;

    /// <summary>
    /// Initializes a new instance of the <see cref="CachedRepositoryDecorator{TEntity, TKey}"/> class.
    /// </summary>
    /// <param name="innerRepository">The repository to decorate.</param>
    /// <param name="cache">The cache service.</param>
    /// <param name="cacheOptions">Optional cache entry options.</param>
    public CachedRepositoryDecorator(
        IRepository<TEntity, TKey> innerRepository,
        ICacheService cache,
        CacheEntryOptions? cacheOptions = null)
    {
        _innerRepository = innerRepository;
        _cache = cache;
        _cacheOptions = cacheOptions ?? CacheEntryOptions.Medium;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<TEntity>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var key = CacheKeyExtensions.EntityListKey<TEntity>("all");

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
    public async Task<TEntity?> GetByIdAsync(TKey id, CancellationToken cancellationToken = default)
    {
        var key = CacheKeyExtensions.EntityKey<TEntity>(id);

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
        // Don't cache filtered queries by default
        return _innerRepository.GetFirstAsync(filter, cancellationToken);
    }

    /// <inheritdoc />
    public Task<TEntity?> GetFirstAsync(
        ISpecification<TEntity> specification,
        CancellationToken cancellationToken = default)
    {
        // Don't cache specification queries by default
        return _innerRepository.GetFirstAsync(specification, cancellationToken);
    }

    /// <inheritdoc />
    public Task<int> CountAsync(
        Expression<Func<TEntity, bool>>? filter = null,
        CancellationToken cancellationToken = default)
    {
        // Don't cache counts - they can change frequently
        return _innerRepository.CountAsync(filter, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<TEntity> AddAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        var result = await _innerRepository.AddAsync(entity, cancellationToken);

        // Invalidate list cache
        await InvalidateListCacheAsync(cancellationToken);

        return result;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<TEntity>> AddRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default)
    {
        var result = await _innerRepository.AddRangeAsync(entities, cancellationToken);

        // Invalidate list cache
        await InvalidateListCacheAsync(cancellationToken);

        return result;
    }

    /// <inheritdoc />
    public async Task<TEntity> UpdateAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        var result = await _innerRepository.UpdateAsync(entity, cancellationToken);

        // Invalidate entity and list cache
        await InvalidateEntityCacheAsync(entity.Id, cancellationToken);
        await InvalidateListCacheAsync(cancellationToken);

        return result;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<TEntity>> UpdateRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default)
    {
        var result = await _innerRepository.UpdateRangeAsync(entities, cancellationToken);

        // Invalidate entity and list cache for all entities
        foreach (var entity in entities)
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

        // Invalidate entity and list cache
        await InvalidateEntityCacheAsync(entity.Id, cancellationToken);
        await InvalidateListCacheAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task DeleteAsync(TKey id, CancellationToken cancellationToken = default)
    {
        await _innerRepository.DeleteAsync(id, cancellationToken);

        // Invalidate entity and list cache
        await InvalidateEntityCacheAsync(id, cancellationToken);
        await InvalidateListCacheAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task DeleteRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default)
    {
        await _innerRepository.DeleteRangeAsync(entities, cancellationToken);

        // Invalidate entity and list cache for all entities
        foreach (var entity in entities)
        {
            await InvalidateEntityCacheAsync(entity.Id, cancellationToken);
        }
        await InvalidateListCacheAsync(cancellationToken);
    }

    private Task InvalidateEntityCacheAsync(TKey id, CancellationToken cancellationToken)
    {
        var key = CacheKeyExtensions.EntityKey<TEntity>(id);
        return _cache.RemoveAsync(key, cancellationToken);
    }

    private Task InvalidateListCacheAsync(CancellationToken cancellationToken)
    {
        var pattern = CacheKeyExtensions.EntityListKey<TEntity>("*");
        return _cache.RemoveByPatternAsync(pattern, cancellationToken);
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

    /// <summary>
    /// Initializes a new instance of the <see cref="CachedRepositoryFactory"/> class.
    /// </summary>
    /// <param name="cache">The cache service.</param>
    public CachedRepositoryFactory(ICacheService cache)
    {
        _cache = cache;
    }

    /// <inheritdoc />
    public IRepository<TEntity, TKey> Create<TEntity, TKey>(
        IRepository<TEntity, TKey> repository,
        CacheEntryOptions? cacheOptions = null)
        where TEntity : IEntity<TKey>
        where TKey : IEquatable<TKey>
    {
        return new CachedRepositoryDecorator<TEntity, TKey>(repository, _cache, cacheOptions);
    }
}
