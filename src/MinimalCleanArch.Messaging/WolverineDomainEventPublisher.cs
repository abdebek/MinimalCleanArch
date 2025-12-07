using MinimalCleanArch.Domain.Events;
using Wolverine;

namespace MinimalCleanArch.Messaging;

/// <summary>
/// Wolverine-based implementation of <see cref="IDomainEventPublisher"/>.
/// </summary>
public class WolverineDomainEventPublisher : IDomainEventPublisher
{
    private readonly IMessageBus _messageBus;

    /// <summary>
    /// Initializes a new instance of the <see cref="WolverineDomainEventPublisher"/> class.
    /// </summary>
    /// <param name="messageBus">The Wolverine message bus.</param>
    public WolverineDomainEventPublisher(IMessageBus messageBus)
    {
        _messageBus = messageBus;
    }

    /// <inheritdoc />
    public async Task PublishAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        await _messageBus.PublishAsync(domainEvent);
    }

    /// <inheritdoc />
    public async Task PublishAsync(IEnumerable<IDomainEvent> domainEvents, CancellationToken cancellationToken = default)
    {
        foreach (var domainEvent in domainEvents)
        {
            await _messageBus.PublishAsync(domainEvent);
        }
    }

    /// <inheritdoc />
    public async Task ScheduleAsync(IDomainEvent domainEvent, DateTimeOffset scheduledTime, CancellationToken cancellationToken = default)
    {
        await _messageBus.ScheduleAsync(domainEvent, scheduledTime);
    }

    /// <inheritdoc />
    public async Task ScheduleAsync(IDomainEvent domainEvent, TimeSpan delay, CancellationToken cancellationToken = default)
    {
        await _messageBus.ScheduleAsync(domainEvent, delay);
    }
}
