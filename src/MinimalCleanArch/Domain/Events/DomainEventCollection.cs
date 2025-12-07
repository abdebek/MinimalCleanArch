namespace MinimalCleanArch.Domain.Events;

/// <summary>
/// A collection that manages domain events for an entity.
/// Use this when you can't inherit from EntityWithEvents.
/// </summary>
public class DomainEventCollection : IHasDomainEvents
{
    private readonly List<IDomainEvent> _domainEvents = new();

    /// <inheritdoc />
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    /// <summary>
    /// Raises a domain event.
    /// </summary>
    /// <param name="domainEvent">The event to raise.</param>
    public void RaiseDomainEvent(IDomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }

    /// <inheritdoc />
    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }
}
