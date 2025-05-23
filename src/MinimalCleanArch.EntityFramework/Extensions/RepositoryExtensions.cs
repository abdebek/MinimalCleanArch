using MinimalCleanArch.Domain.Entities;
using MinimalCleanArch.Repositories;

//namespace MinimalCleanArch.EntityFramework.Extensions;
namespace MinimalCleanArch.Extensions;

/// <summary>
/// Extension methods for repositories to provide backward compatibility with immediate save behavior
/// </summary>
public static class RepositoryExtensions
{
    /// <summary>
    /// Adds an entity and immediately saves changes to the database
    /// </summary>
    /// <typeparam name="TEntity">The type of the entity</typeparam>
    /// <typeparam name="TKey">The type of the entity's key</typeparam>
    /// <param name="repository">The repository</param>
    /// <param name="unitOfWork">The unit of work</param>
    /// <param name="entity">The entity to add</param>
    /// <param name="cancellationToken">A token to observe while waiting for the task to complete</param>
    /// <returns>A task that represents the asynchronous operation</returns>
    public static async Task<TEntity> AddAndSaveAsync<TEntity, TKey>(
        this IRepository<TEntity, TKey> repository,
        IUnitOfWork unitOfWork,
        TEntity entity,
        CancellationToken cancellationToken = default)
        where TEntity : IEntity<TKey>
        where TKey : IEquatable<TKey>
    {
        var result = await repository.AddAsync(entity, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return result;
    }

    /// <summary>
    /// Updates an entity and immediately saves changes to the database
    /// </summary>
    /// <typeparam name="TEntity">The type of the entity</typeparam>
    /// <typeparam name="TKey">The type of the entity's key</typeparam>
    /// <param name="repository">The repository</param>
    /// <param name="unitOfWork">The unit of work</param>
    /// <param name="entity">The entity to update</param>
    /// <param name="cancellationToken">A token to observe while waiting for the task to complete</param>
    /// <returns>A task that represents the asynchronous operation</returns>
    public static async Task<TEntity> UpdateAndSaveAsync<TEntity, TKey>(
        this IRepository<TEntity, TKey> repository,
        IUnitOfWork unitOfWork,
        TEntity entity,
        CancellationToken cancellationToken = default)
        where TEntity : IEntity<TKey>
        where TKey : IEquatable<TKey>
    {
        var result = await repository.UpdateAsync(entity, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return result;
    }

    /// <summary>
    /// Deletes an entity and immediately saves changes to the database
    /// </summary>
    /// <typeparam name="TEntity">The type of the entity</typeparam>
    /// <typeparam name="TKey">The type of the entity's key</typeparam>
    /// <param name="repository">The repository</param>
    /// <param name="unitOfWork">The unit of work</param>
    /// <param name="entity">The entity to delete</param>
    /// <param name="cancellationToken">A token to observe while waiting for the task to complete</param>
    /// <returns>A task that represents the asynchronous operation</returns>
    public static async Task DeleteAndSaveAsync<TEntity, TKey>(
        this IRepository<TEntity, TKey> repository,
        IUnitOfWork unitOfWork,
        TEntity entity,
        CancellationToken cancellationToken = default)
        where TEntity : IEntity<TKey>
        where TKey : IEquatable<TKey>
    {
        await repository.DeleteAsync(entity, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Deletes an entity by ID and immediately saves changes to the database
    /// </summary>
    /// <typeparam name="TEntity">The type of the entity</typeparam>
    /// <typeparam name="TKey">The type of the entity's key</typeparam>
    /// <param name="repository">The repository</param>
    /// <param name="unitOfWork">The unit of work</param>
    /// <param name="id">The entity's key</param>
    /// <param name="cancellationToken">A token to observe while waiting for the task to complete</param>
    /// <returns>A task that represents the asynchronous operation</returns>
    public static async Task DeleteAndSaveAsync<TEntity, TKey>(
        this IRepository<TEntity, TKey> repository,
        IUnitOfWork unitOfWork,
        TKey id,
        CancellationToken cancellationToken = default)
        where TEntity : IEntity<TKey>
        where TKey : IEquatable<TKey>
    {
        await repository.DeleteAsync(id, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Adds multiple entities and immediately saves changes to the database
    /// </summary>
    /// <typeparam name="TEntity">The type of the entity</typeparam>
    /// <typeparam name="TKey">The type of the entity's key</typeparam>
    /// <param name="repository">The repository</param>
    /// <param name="unitOfWork">The unit of work</param>
    /// <param name="entities">The entities to add</param>
    /// <param name="cancellationToken">A token to observe while waiting for the task to complete</param>
    /// <returns>A task that represents the asynchronous operation</returns>
    public static async Task<IEnumerable<TEntity>> AddRangeAndSaveAsync<TEntity, TKey>(
        this IRepository<TEntity, TKey> repository,
        IUnitOfWork unitOfWork,
        IEnumerable<TEntity> entities,
        CancellationToken cancellationToken = default)
        where TEntity : IEntity<TKey>
        where TKey : IEquatable<TKey>
    {
        var result = await repository.AddRangeAsync(entities, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return result;
    }

    // Convenience methods for repositories with int keys
    
    /// <summary>
    /// Adds an entity and immediately saves changes to the database
    /// </summary>
    /// <typeparam name="TEntity">The type of the entity</typeparam>
    /// <param name="repository">The repository</param>
    /// <param name="unitOfWork">The unit of work</param>
    /// <param name="entity">The entity to add</param>
    /// <param name="cancellationToken">A token to observe while waiting for the task to complete</param>
    /// <returns>A task that represents the asynchronous operation</returns>
    public static Task<TEntity> AddAndSaveAsync<TEntity>(
        this IRepository<TEntity> repository,
        IUnitOfWork unitOfWork,
        TEntity entity,
        CancellationToken cancellationToken = default)
        where TEntity : IEntity<int>
    {
        return AddAndSaveAsync<TEntity, int>(repository, unitOfWork, entity, cancellationToken);
    }

    /// <summary>
    /// Updates an entity and immediately saves changes to the database
    /// </summary>
    /// <typeparam name="TEntity">The type of the entity</typeparam>
    /// <param name="repository">The repository</param>
    /// <param name="unitOfWork">The unit of work</param>
    /// <param name="entity">The entity to update</param>
    /// <param name="cancellationToken">A token to observe while waiting for the task to complete</param>
    /// <returns>A task that represents the asynchronous operation</returns>
    public static Task<TEntity> UpdateAndSaveAsync<TEntity>(
        this IRepository<TEntity> repository,
        IUnitOfWork unitOfWork,
        TEntity entity,
        CancellationToken cancellationToken = default)
        where TEntity : IEntity<int>
    {
        return UpdateAndSaveAsync<TEntity, int>(repository, unitOfWork, entity, cancellationToken);
    }

    /// <summary>
    /// Deletes an entity and immediately saves changes to the database
    /// </summary>
    /// <typeparam name="TEntity">The type of the entity</typeparam>
    /// <param name="repository">The repository</param>
    /// <param name="unitOfWork">The unit of work</param>
    /// <param name="entity">The entity to delete</param>
    /// <param name="cancellationToken">A token to observe while waiting for the task to complete</param>
    /// <returns>A task that represents the asynchronous operation</returns>
    public static Task DeleteAndSaveAsync<TEntity>(
        this IRepository<TEntity> repository,
        IUnitOfWork unitOfWork,
        TEntity entity,
        CancellationToken cancellationToken = default)
        where TEntity : IEntity<int>
    {
        return DeleteAndSaveAsync<TEntity, int>(repository, unitOfWork, entity, cancellationToken);
    }
}
