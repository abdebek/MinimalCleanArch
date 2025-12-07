using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using MinimalCleanArch.Domain.Events;
using Wolverine;

namespace MinimalCleanArch.Messaging.Middleware;

/// <summary>
/// EF Core interceptor that automatically publishes domain events after SaveChanges.
/// Events are only cleared after successful publishing to prevent event loss.
/// </summary>
public class DomainEventPublishingInterceptor : SaveChangesInterceptor
{
    private readonly IMessageBus _messageBus;
    private readonly ILogger<DomainEventPublishingInterceptor> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DomainEventPublishingInterceptor"/> class.
    /// </summary>
    /// <param name="messageBus">The Wolverine message bus.</param>
    /// <param name="logger">The logger.</param>
    public DomainEventPublishingInterceptor(
        IMessageBus messageBus,
        ILogger<DomainEventPublishingInterceptor> logger)
    {
        _messageBus = messageBus ?? throw new ArgumentNullException(nameof(messageBus));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
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
        // Note: For sync SavedChanges, we queue events for later async processing
        // This avoids the sync-over-async anti-pattern that can cause deadlocks
        if (eventData.Context is not null)
        {
            var events = CollectDomainEvents(eventData.Context);
            if (events.Count > 0)
            {
                _logger.LogWarning(
                    "Sync SavedChanges called with {EventCount} domain events. " +
                    "Events will be published asynchronously. Consider using SaveChangesAsync for immediate publishing.",
                    events.Count);

                // Fire and forget - queue for async processing
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await PublishEventsAsync(events, CancellationToken.None);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to publish {EventCount} domain events from sync SavedChanges", events.Count);
                    }
                });
            }
        }

        return base.SavedChanges(eventData, result);
    }

    private async Task PublishDomainEventsAsync(DbContext context, CancellationToken cancellationToken)
    {
        var entitiesWithEvents = CollectEntitiesWithEvents(context);
        if (entitiesWithEvents.Count == 0) return;

        // Collect all domain events
        var domainEvents = entitiesWithEvents
            .SelectMany(e => e.DomainEvents)
            .ToList();

        if (domainEvents.Count == 0) return;

        _logger.LogDebug("Publishing {EventCount} domain events from {EntityCount} entities",
            domainEvents.Count, entitiesWithEvents.Count);

        try
        {
            // Publish all events
            await PublishEventsAsync(domainEvents, cancellationToken);

            // Only clear events AFTER successful publishing
            foreach (var entity in entitiesWithEvents)
            {
                entity.ClearDomainEvents();
            }

            _logger.LogDebug("Successfully published {EventCount} domain events", domainEvents.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish domain events. Events NOT cleared and may be retried.");
            throw;
        }
    }

    private async Task PublishEventsAsync(IReadOnlyList<IDomainEvent> events, CancellationToken cancellationToken)
    {
        // Publish concurrently for better performance
        var tasks = events.Select(e =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            return _messageBus.PublishAsync(e).AsTask();
        });

        await Task.WhenAll(tasks);
    }

    private static List<IHasDomainEvents> CollectEntitiesWithEvents(DbContext context)
    {
        return context.ChangeTracker
            .Entries<IHasDomainEvents>()
            .Where(e => e.Entity.DomainEvents.Count > 0)
            .Select(e => e.Entity)
            .ToList();
    }

    private static List<IDomainEvent> CollectDomainEvents(DbContext context)
    {
        var entities = CollectEntitiesWithEvents(context);
        var events = entities.SelectMany(e => e.DomainEvents).ToList();

        // Clear events after collecting (for sync path, we must clear before fire-and-forget)
        foreach (var entity in entities)
        {
            entity.ClearDomainEvents();
        }

        return events;
    }
}
