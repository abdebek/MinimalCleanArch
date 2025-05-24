using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using MinimalCleanArch.Domain.Entities;
using MinimalCleanArch.EntityFramework.Specifications;
using MinimalCleanArch.Repositories;
using MinimalCleanArch.Specifications;

namespace MinimalCleanArch.EntityFramework.Repositories;

/// <summary>
/// Generic repository implementation using Entity Framework Core
/// </summary>
/// <typeparam name="TEntity">The type of the entity</typeparam>
/// <typeparam name="TKey">The type of the entity's key</typeparam>
public class Repository<TEntity, TKey> : IRepository<TEntity, TKey> 
    where TEntity : class, IEntity<TKey> 
    where TKey : IEquatable<TKey>
{
    /// <summary>
    /// The database context
    /// </summary>
    protected readonly DbContext DbContext;

    /// <summary>
    /// The entity set
    /// </summary>
    protected readonly DbSet<TEntity> DbSet;

    /// <summary>
    /// Initializes a new instance of the <see cref="Repository{TEntity,TKey}"/> class
    /// </summary>
    /// <param name="dbContext">The database context</param>
    public Repository(DbContext dbContext)
    {
        DbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        DbSet = dbContext.Set<TEntity>();
    }

    /// <summary>
    /// Gets all entities
    /// </summary>
    /// <param name="cancellationToken">A token to observe while waiting for the task to complete</param>
    /// <returns>A task that represents the asynchronous operation</returns>
    public virtual async Task<IReadOnlyList<TEntity>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await DbSet.ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Gets entities based on the specified filter
    /// </summary>
    /// <param name="filter">The filter expression</param>
    /// <param name="cancellationToken">A token to observe while waiting for the task to complete</param>
    /// <returns>A task that represents the asynchronous operation</returns>
    public virtual async Task<IReadOnlyList<TEntity>> GetAsync(
        Expression<Func<TEntity, bool>> filter,
        CancellationToken cancellationToken = default)
    {
        return await DbSet.Where(filter).ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Gets entities using a specification
    /// </summary>
    /// <param name="specification">The specification</param>
    /// <param name="cancellationToken">A token to observe while waiting for the task to complete</param>
    /// <returns>A task that represents the asynchronous operation</returns>
    public virtual async Task<IReadOnlyList<TEntity>> GetAsync(
        ISpecification<TEntity> specification,
        CancellationToken cancellationToken = default)
    {
        return await ApplySpecification(specification).ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Gets a single entity by its key
    /// </summary>
    /// <param name="id">The entity's key</param>
    /// <param name="cancellationToken">A token to observe while waiting for the task to complete</param>
    /// <returns>A task that represents the asynchronous operation</returns>
    public virtual async Task<TEntity?> GetByIdAsync(TKey id, CancellationToken cancellationToken = default)
    {
        return await DbSet.FirstOrDefaultAsync(e => e.Id.Equals(id), cancellationToken);
    }

    /// <summary>
    /// Gets the first entity that matches the filter
    /// </summary>
    /// <param name="filter">The filter expression</param>
    /// <param name="cancellationToken">A token to observe while waiting for the task to complete</param>
    /// <returns>A task that represents the asynchronous operation</returns>
    public virtual async Task<TEntity?> GetFirstAsync(
        Expression<Func<TEntity, bool>> filter,
        CancellationToken cancellationToken = default)
    {
        return await DbSet.FirstOrDefaultAsync(filter, cancellationToken);
    }

    /// <summary>
    /// Gets the first entity that matches the specification
    /// </summary>
    /// <param name="specification">The specification</param>
    /// <param name="cancellationToken">A token to observe while waiting for the task to complete</param>
    /// <returns>A task that represents the asynchronous operation</returns>
    public virtual async Task<TEntity?> GetFirstAsync(
        ISpecification<TEntity> specification,
        CancellationToken cancellationToken = default)
    {
        return await ApplySpecification(specification).FirstOrDefaultAsync(cancellationToken);
    }

    /// <summary>
    /// Counts entities that match the filter
    /// </summary>
    /// <param name="filter">The filter expression</param>
    /// <param name="cancellationToken">A token to observe while waiting for the task to complete</param>
    /// <returns>A task that represents the asynchronous operation</returns>
    public virtual async Task<int> CountAsync(
        Expression<Func<TEntity, bool>>? filter = null,
        CancellationToken cancellationToken = default)
    {
        if (filter == null)
            return await DbSet.CountAsync(cancellationToken);
        return await DbSet.CountAsync(filter, cancellationToken);
    }

    /// <summary>
    /// Adds an entity to the context (does not save to database)
    /// </summary>
    /// <param name="entity">The entity to add</param>
    /// <param name="cancellationToken">A token to observe while waiting for the task to complete</param>
    /// <returns>A task that represents the asynchronous operation</returns>
    public virtual async Task<TEntity> AddAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        var entityEntry = await DbSet.AddAsync(entity, cancellationToken);
        return entityEntry.Entity;
    }

    /// <summary>
    /// Adds multiple entities to the context (does not save to database)
    /// </summary>
    /// <param name="entities">The entities to add</param>
    /// <param name="cancellationToken">A token to observe while waiting for the task to complete</param>
    /// <returns>A task that represents the asynchronous operation</returns>
    public virtual async Task<IEnumerable<TEntity>> AddRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default)
    {
        var entitiesList = entities.ToList();
        await DbSet.AddRangeAsync(entitiesList, cancellationToken);
        return entitiesList;
    }

    /// <summary>
    /// Updates an entity in the context (does not save to database)
    /// </summary>
    /// <param name="entity">The entity to update</param>
    /// <param name="cancellationToken">A token to observe while waiting for the task to complete</param>
    /// <returns>A task that represents the asynchronous operation</returns>
    public virtual Task<TEntity> UpdateAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        DbContext.Entry(entity).State = EntityState.Modified;
        return Task.FromResult(entity);
    }

    /// <summary>
    /// Updates multiple entities in the context (does not save to database)
    /// </summary>
    /// <param name="entities">The entities to update</param>
    /// <param name="cancellationToken">A token to observe while waiting for the task to complete</param>
    /// <returns>A task that represents the asynchronous operation</returns>
    public virtual Task<IEnumerable<TEntity>> UpdateRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default)
    {
        var entitiesList = entities.ToList();
        foreach (var entity in entitiesList)
        {
            DbContext.Entry(entity).State = EntityState.Modified;
        }
        return Task.FromResult<IEnumerable<TEntity>>(entitiesList);
    }

    /// <summary>
    /// Marks an entity for deletion (does not save to database)
    /// </summary>
    /// <param name="entity">The entity to delete</param>
    /// <param name="cancellationToken">A token to observe while waiting for the task to complete</param>
    /// <returns>A task that represents the asynchronous operation</returns>
    public virtual Task DeleteAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        if (entity is ISoftDelete softDeleteEntity)
        {
            softDeleteEntity.IsDeleted = true;
            DbContext.Entry(entity).State = EntityState.Modified;
        }
        else
        {
            DbSet.Remove(entity);
        }
        return Task.CompletedTask;
    }

    /// <summary>
    /// Marks an entity for deletion by its key (does not save to database)
    /// </summary>
    /// <param name="id">The entity's key</param>
    /// <param name="cancellationToken">A token to observe while waiting for the task to complete</param>
    /// <returns>A task that represents the asynchronous operation</returns>
    public virtual async Task DeleteAsync(TKey id, CancellationToken cancellationToken = default)
    {
        var entity = await GetByIdAsync(id, cancellationToken);
        if (entity != null)
        {
            await DeleteAsync(entity, cancellationToken);
        }
    }

    /// <summary>
    /// Marks multiple entities for deletion (does not save to database)
    /// </summary>
    /// <param name="entities">The entities to delete</param>
    /// <param name="cancellationToken">A token to observe while waiting for the task to complete</param>
    /// <returns>A task that represents the asynchronous operation</returns>
    public virtual Task DeleteRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default)
    {
        var entitiesList = entities.ToList();
        
        foreach (var entity in entitiesList)
        {
            if (entity is ISoftDelete softDeleteEntity)
            {
                softDeleteEntity.IsDeleted = true;
                DbContext.Entry(entity).State = EntityState.Modified;
            }
            else
            {
                DbSet.Remove(entity);
            }
        }
        
        return Task.CompletedTask;
    }

    /// <summary>
    /// Applies a specification to an IQueryable
    /// </summary>
    /// <param name="specification">The specification to apply</param>
    /// <returns>The IQueryable with the specification applied</returns>
    protected virtual IQueryable<TEntity> ApplySpecification(ISpecification<TEntity> specification)
    {
        return SpecificationEvaluator<TEntity>.GetQuery(DbSet.AsQueryable(), specification);
    }
}

/// <summary>
/// Generic repository implementation using Entity Framework Core with int key
/// </summary>
/// <typeparam name="TEntity">The type of the entity</typeparam>
public class Repository<TEntity> : Repository<TEntity, int>, IRepository<TEntity>
    where TEntity : class, IEntity<int>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Repository{TEntity}"/> class
    /// </summary>
    /// <param name="dbContext">The database context</param>
    public Repository(DbContext dbContext) : base(dbContext)
    {
    }
}