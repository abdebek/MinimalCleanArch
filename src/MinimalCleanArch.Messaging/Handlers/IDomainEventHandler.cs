using MinimalCleanArch.Domain.Events;

namespace MinimalCleanArch.Messaging.Handlers;

/// <summary>
/// Base interface for domain event handlers.
/// Note: Wolverine doesn't require implementing this interface, but it provides
/// a consistent pattern and makes handlers easier to discover.
/// </summary>
/// <typeparam name="TEvent">The type of domain event to handle.</typeparam>
public interface IDomainEventHandler<in TEvent>
    where TEvent : IDomainEvent
{
    /// <summary>
    /// Handles the domain event.
    /// </summary>
    /// <param name="domainEvent">The event to handle.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task HandleAsync(TEvent domainEvent, CancellationToken cancellationToken = default);
}
