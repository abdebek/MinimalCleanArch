namespace MinimalCleanArch.Domain.Events;

/// <summary>
/// Interface for entities that can raise domain events.
/// </summary>
public interface IHasDomainEvents
{
    /// <summary>
    /// Gets the domain events raised by this entity.
    /// </summary>
    IReadOnlyCollection<IDomainEvent> DomainEvents { get; }

    /// <summary>
    /// Clears all domain events from this entity.
    /// Called after events have been dispatched.
    /// </summary>
    void ClearDomainEvents();
}

/// <summary>
/// Base class for entities that can raise domain events.
/// </summary>
public abstract class EntityWithEvents : IHasDomainEvents
{
    private readonly List<IDomainEvent> _domainEvents = new();

    /// <inheritdoc />
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    /// <summary>
    /// Raises a domain event.
    /// </summary>
    /// <param name="domainEvent">The event to raise.</param>
    protected void RaiseDomainEvent(IDomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }

    /// <inheritdoc />
    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }
}
