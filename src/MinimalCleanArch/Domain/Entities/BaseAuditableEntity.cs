namespace MinimalCleanArch.Domain.Entities;

/// <summary>
/// Base implementation of <see cref="IAuditableEntity"/> with a specific key type
/// </summary>
/// <typeparam name="TKey">The type of the entity's key</typeparam>
public abstract class BaseAuditableEntity<TKey> : BaseEntity<TKey>, IAuditableEntity 
    where TKey : IEquatable<TKey>
{
    /// <summary>
    /// Gets or sets the date and time when this entity was created
    /// </summary>
    public DateTime CreatedAt { get; set; }
    
    /// <summary>
    /// Gets or sets the user who created this entity
    /// </summary>
    public string? CreatedBy { get; set; }
    
    /// <summary>
    /// Gets or sets the date and time when this entity was last modified
    /// </summary>
    public DateTime? LastModifiedAt { get; set; }
    
    /// <summary>
    /// Gets or sets the user who last modified this entity
    /// </summary>
    public string? LastModifiedBy { get; set; }
}

/// <summary>
/// Base implementation of <see cref="IAuditableEntity"/> with int key
/// </summary>
public abstract class BaseAuditableEntity : BaseAuditableEntity<int>
{
}
