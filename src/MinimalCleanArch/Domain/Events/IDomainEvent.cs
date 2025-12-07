namespace MinimalCleanArch.Domain.Events;

/// <summary>
/// Marker interface for domain events.
/// Domain events represent something that happened in the domain that domain experts care about.
/// </summary>
public interface IDomainEvent
{
    /// <summary>
    /// Gets the unique identifier for this event instance.
    /// </summary>
    Guid EventId { get; }

    /// <summary>
    /// Gets the timestamp when the event occurred.
    /// </summary>
    DateTimeOffset OccurredAt { get; }
}

/// <summary>
/// Base class for domain events providing common functionality.
/// </summary>
public abstract record DomainEvent : IDomainEvent
{
    /// <inheritdoc />
    public Guid EventId { get; } = Guid.NewGuid();

    /// <inheritdoc />
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// Domain event that includes the entity ID that triggered it.
/// </summary>
/// <typeparam name="TKey">The type of the entity key.</typeparam>
public abstract record EntityDomainEvent<TKey> : DomainEvent
    where TKey : IEquatable<TKey>
{
    /// <summary>
    /// Gets the ID of the entity that triggered this event.
    /// </summary>
    public required TKey EntityId { get; init; }
}
