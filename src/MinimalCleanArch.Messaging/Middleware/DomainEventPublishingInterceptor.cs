using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using MinimalCleanArch.Domain.Events;
using Wolverine;

namespace MinimalCleanArch.Messaging.Middleware;

/// <summary>
/// EF Core interceptor that automatically publishes domain events after SaveChanges.
/// </summary>
public class DomainEventPublishingInterceptor : SaveChangesInterceptor
{
    private readonly IMessageBus _messageBus;

    /// <summary>
    /// Initializes a new instance of the <see cref="DomainEventPublishingInterceptor"/> class.
    /// </summary>
    /// <param name="messageBus">The Wolverine message bus.</param>
    public DomainEventPublishingInterceptor(IMessageBus messageBus)
    {
        _messageBus = messageBus;
    }

    /// <inheritdoc />
    public override async ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is not null)
        {
            await PublishDomainEventsAsync(eventData.Context, cancellationToken);
        }

        return await base.SavedChangesAsync(eventData, result, cancellationToken);
    }

    /// <inheritdoc />
    public override int SavedChanges(
        SaveChangesCompletedEventData eventData,
        int result)
    {
        if (eventData.Context is not null)
        {
            // Synchronous version - we need to block here
            PublishDomainEventsAsync(eventData.Context, CancellationToken.None)
                .GetAwaiter()
                .GetResult();
        }

        return base.SavedChanges(eventData, result);
    }

    private async Task PublishDomainEventsAsync(DbContext context, CancellationToken cancellationToken)
    {
        // Get all entities that have domain events
        var entitiesWithEvents = context.ChangeTracker
            .Entries<IHasDomainEvents>()
            .Where(e => e.Entity.DomainEvents.Count > 0)
            .Select(e => e.Entity)
            .ToList();

        // Collect all domain events
        var domainEvents = entitiesWithEvents
            .SelectMany(e => e.DomainEvents)
            .ToList();

        // Clear events from entities (they've been collected)
        foreach (var entity in entitiesWithEvents)
        {
            entity.ClearDomainEvents();
        }

        // Publish all events through Wolverine
        foreach (var domainEvent in domainEvents)
        {
            await _messageBus.PublishAsync(domainEvent);
        }
    }
}
