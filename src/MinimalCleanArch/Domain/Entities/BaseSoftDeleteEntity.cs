namespace MinimalCleanArch.Domain.Entities;

/// <summary>
/// Base implementation of <see cref="ISoftDelete"/> with a specific key type
/// </summary>
/// <typeparam name="TKey">The type of the entity's key</typeparam>
public abstract class BaseSoftDeleteEntity<TKey> : BaseAuditableEntity<TKey>, ISoftDelete 
    where TKey : IEquatable<TKey>
{
    /// <summary>
    /// Gets or sets a value indicating whether this entity is deleted
    /// </summary>
    public bool IsDeleted { get; set; }
}

/// <summary>
/// Base implementation of <see cref="ISoftDelete"/> with int key
/// </summary>
public abstract class BaseSoftDeleteEntity : BaseSoftDeleteEntity<int>
{
}
