namespace MinimalCleanArch.Domain.Entities;

/// <summary>
/// Base interface for all entities with a specific key type
/// </summary>
/// <typeparam name="TKey">The type of the entity's key</typeparam>
public interface IEntity<TKey> where TKey : IEquatable<TKey>
{
    /// <summary>
    /// Gets or sets the unique identifier for this entity
    /// </summary>
    TKey Id { get; set; }
}
