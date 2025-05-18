namespace MinimalCleanArch.Domain.Entities;

/// <summary>
/// Interface for entities that support audit information
/// </summary>
public interface IAuditableEntity
{
    /// <summary>
    /// Gets or sets the date and time when this entity was created
    /// </summary>
    DateTime CreatedAt { get; set; }
    
    /// <summary>
    /// Gets or sets the user who created this entity
    /// </summary>
    string? CreatedBy { get; set; }
    
    /// <summary>
    /// Gets or sets the date and time when this entity was last modified
    /// </summary>
    DateTime? LastModifiedAt { get; set; }
    
    /// <summary>
    /// Gets or sets the user who last modified this entity
    /// </summary>
    string? LastModifiedBy { get; set; }
}
