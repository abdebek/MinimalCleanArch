using MinimalCleanArch.Domain.Events;

namespace MinimalCleanArch.Messaging;

/// <summary>
/// Service for publishing domain events.
/// </summary>
public interface IDomainEventPublisher
{
    /// <summary>
    /// Publishes a domain event immediately.
    /// </summary>
    /// <param name="domainEvent">The event to publish.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task PublishAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default);

    /// <summary>
    /// Publishes multiple domain events immediately.
    /// </summary>
    /// <param name="domainEvents">The events to publish.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task PublishAsync(IEnumerable<IDomainEvent> domainEvents, CancellationToken cancellationToken = default);

    /// <summary>
    /// Schedules a domain event for future delivery.
    /// </summary>
    /// <param name="domainEvent">The event to schedule.</param>
    /// <param name="scheduledTime">When to deliver the event.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task ScheduleAsync(IDomainEvent domainEvent, DateTimeOffset scheduledTime, CancellationToken cancellationToken = default);

    /// <summary>
    /// Schedules a domain event for delivery after a delay.
    /// </summary>
    /// <param name="domainEvent">The event to schedule.</param>
    /// <param name="delay">The delay before delivery.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task ScheduleAsync(IDomainEvent domainEvent, TimeSpan delay, CancellationToken cancellationToken = default);
}
