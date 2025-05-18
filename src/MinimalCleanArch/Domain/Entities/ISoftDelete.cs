namespace MinimalCleanArch.Domain.Entities;

/// <summary>
/// Interface for entities that support soft delete
/// </summary>
public interface ISoftDelete
{
    /// <summary>
    /// Gets or sets a value indicating whether this entity is deleted
    /// </summary>
    bool IsDeleted { get; set; }
}
