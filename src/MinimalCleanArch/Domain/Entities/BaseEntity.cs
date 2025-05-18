namespace MinimalCleanArch.Domain.Entities;

/// <summary>
/// Base implementation of <see cref="IEntity{TKey}"/>
/// </summary>
/// <typeparam name="TKey">The type of the entity's key</typeparam>
public abstract class BaseEntity<TKey> : IEntity<TKey> where TKey : IEquatable<TKey>
{
    /// <summary>
    /// Gets or sets the unique identifier for this entity
    /// </summary>
    public TKey Id { get; set; } = default!;
    
    /// <summary>
    /// Determines whether the specified entity is equal to the current entity
    /// </summary>
    public override bool Equals(object? obj)
    {
        if (obj is null)
            return false;

        if (ReferenceEquals(this, obj))
            return true;

        if (obj.GetType() != GetType())
            return false;

        if (obj is not BaseEntity<TKey> other)
            return false;

        if (Id is null || other.Id is null)
            return false;

        return Id.Equals(other.Id);
    }

    /// <summary>
    /// Returns a hash code for this entity
    /// </summary>
    public override int GetHashCode()
    {
        return Id?.GetHashCode() ?? 0;
    }

    /// <summary>
    /// Compares two entities for equality
    /// </summary>
    public static bool operator ==(BaseEntity<TKey>? left, BaseEntity<TKey>? right)
    {
        if (left is null && right is null)
            return true;

        if (left is null || right is null)
            return false;

        return left.Equals(right);
    }

    /// <summary>
    /// Compares two entities for inequality
    /// </summary>
    public static bool operator !=(BaseEntity<TKey>? left, BaseEntity<TKey>? right)
    {
        return !(left == right);
    }
}

/// <summary>
/// Base implementation of <see cref="IEntity{TKey}"/> with int key
/// </summary>
public abstract class BaseEntity : BaseEntity<int>
{
}
