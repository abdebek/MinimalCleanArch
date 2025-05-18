using System.Linq.Expressions;
using MinimalCleanArch.Domain.Entities;
using MinimalCleanArch.Specifications;

namespace MinimalCleanArch.Repositories
{
    /// <summary>
    /// Generic repository interface for entities with a specific key type
    /// </summary>
    /// <typeparam name="TEntity">The type of the entity</typeparam>
    /// <typeparam name="TKey">The type of the entity's key</typeparam>
    public interface IRepository<TEntity, TKey> 
        where TEntity : IEntity<TKey> 
        where TKey : IEquatable<TKey>
    {
        /// <summary>
        /// Gets all entities
        /// </summary>
        /// <param name="cancellationToken">A token to observe while waiting for the task to complete</param>
        /// <returns>A task that represents the asynchronous operation</returns>
        Task<IReadOnlyList<TEntity>> GetAllAsync(CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Gets entities based on the specified filter
        /// </summary>
        /// <param name="filter">The filter expression</param>
        /// <param name="cancellationToken">A token to observe while waiting for the task to complete</param>
        /// <returns>A task that represents the asynchronous operation</returns>
        Task<IReadOnlyList<TEntity>> GetAsync(
            Expression<Func<TEntity, bool>> filter,
            CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Gets entities using a specification
        /// </summary>
        /// <param name="specification">The specification</param>
        /// <param name="cancellationToken">A token to observe while waiting for the task to complete</param>
        /// <returns>A task that represents the asynchronous operation</returns>
        Task<IReadOnlyList<TEntity>> GetAsync(
            ISpecification<TEntity> specification,
            CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Gets a single entity by its key
        /// </summary>
        /// <param name="id">The entity's key</param>
        /// <param name="cancellationToken">A token to observe while waiting for the task to complete</param>
        /// <returns>A task that represents the asynchronous operation</returns>
        Task<TEntity?> GetByIdAsync(TKey id, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Gets the first entity that matches the filter
        /// </summary>
        /// <param name="filter">The filter expression</param>
        /// <param name="cancellationToken">A token to observe while waiting for the task to complete</param>
        /// <returns>A task that represents the asynchronous operation</returns>
        Task<TEntity?> GetFirstAsync(
            Expression<Func<TEntity, bool>> filter,
            CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Gets the first entity that matches the specification
        /// </summary>
        /// <param name="specification">The specification</param>
        /// <param name="cancellationToken">A token to observe while waiting for the task to complete</param>
        /// <returns>A task that represents the asynchronous operation</returns>
        Task<TEntity?> GetFirstAsync(
            ISpecification<TEntity> specification,
            CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Counts entities that match the filter
        /// </summary>
        /// <param name="filter">The filter expression</param>
        /// <param name="cancellationToken">A token to observe while waiting for the task to complete</param>
        /// <returns>A task that represents the asynchronous operation</returns>
        Task<int> CountAsync(
            Expression<Func<TEntity, bool>>? filter = null,
            CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Adds an entity
        /// </summary>
        /// <param name="entity">The entity to add</param>
        /// <param name="cancellationToken">A token to observe while waiting for the task to complete</param>
        /// <returns>A task that represents the asynchronous operation</returns>
        Task<TEntity> AddAsync(TEntity entity, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Updates an entity
        /// </summary>
        /// <param name="entity">The entity to update</param>
        /// <param name="cancellationToken">A token to observe while waiting for the task to complete</param>
        /// <returns>A task that represents the asynchronous operation</returns>
        Task<TEntity> UpdateAsync(TEntity entity, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Deletes an entity
        /// </summary>
        /// <param name="entity">The entity to delete</param>
        /// <param name="cancellationToken">A token to observe while waiting for the task to complete</param>
        /// <returns>A task that represents the asynchronous operation</returns>
        Task DeleteAsync(TEntity entity, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Deletes an entity by its key
        /// </summary>
        /// <param name="id">The entity's key</param>
        /// <param name="cancellationToken">A token to observe while waiting for the task to complete</param>
        /// <returns>A task that represents the asynchronous operation</returns>
        Task DeleteAsync(TKey id, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Generic repository interface for entities with int key
    /// </summary>
    /// <typeparam name="TEntity">The type of the entity</typeparam>
    public interface IRepository<TEntity> : IRepository<TEntity, int> 
        where TEntity : IEntity<int>
    {
    }
}
