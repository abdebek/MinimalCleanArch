using Microsoft.Extensions.Logging;
using MinimalCleanArch.Sample.Domain.Events;

namespace MinimalCleanArch.Sample.Application.Handlers;

/// <summary>
/// Wolverine handlers for Todo domain events.
/// Wolverine discovers these by convention - no interface implementation required.
/// </summary>
public class TodoEventHandlers
{
    private readonly ILogger<TodoEventHandlers> _logger;

    public TodoEventHandlers(ILogger<TodoEventHandlers> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Handles the TodoCreatedEvent.
    /// This could send notifications, update search indexes, etc.
    /// </summary>
    public async Task Handle(TodoCreatedEvent @event)
    {
        _logger.LogInformation(
            "Todo created: {TodoId} - '{Title}' with priority {Priority}",
            @event.EntityId,
            @event.Title,
            @event.Priority);

        // Simulate async work (e.g., sending notification)
        await Task.Delay(10);

        // In a real app, you might:
        // - Send an email notification
        // - Update a search index
        // - Publish to external systems
        // - Update denormalized views
    }

    /// <summary>
    /// Handles the TodoCompletedEvent.
    /// </summary>
    public async Task Handle(TodoCompletedEvent @event)
    {
        _logger.LogInformation(
            "Todo completed: {TodoId} - '{Title}' at {CompletedAt}",
            @event.EntityId,
            @event.Title,
            @event.CompletedAt);

        await Task.Delay(10);

        // In a real app, you might:
        // - Update user statistics
        // - Award gamification points
        // - Archive the todo
    }

    /// <summary>
    /// Handles the TodoUpdatedEvent.
    /// </summary>
    public async Task Handle(TodoUpdatedEvent @event)
    {
        _logger.LogInformation(
            "Todo updated: {TodoId} - '{Title}' with priority {Priority}",
            @event.EntityId,
            @event.Title,
            @event.Priority);

        await Task.Delay(10);
    }

    /// <summary>
    /// Handles the TodoDeletedEvent.
    /// </summary>
    public async Task Handle(TodoDeletedEvent @event)
    {
        _logger.LogInformation(
            "Todo deleted: {TodoId} - '{Title}'",
            @event.EntityId,
            @event.Title);

        await Task.Delay(10);

        // In a real app, you might:
        // - Remove from search index
        // - Clean up related data
        // - Update statistics
    }

    /// <summary>
    /// Handles the TodoReminderEvent (scheduled events).
    /// </summary>
    public async Task Handle(TodoReminderEvent @event)
    {
        _logger.LogInformation(
            "Todo reminder: {TodoId} - '{Title}' is due on {DueDate} with priority {Priority}",
            @event.EntityId,
            @event.Title,
            @event.DueDate,
            @event.Priority);

        await Task.Delay(10);

        // In a real app, you might:
        // - Send push notification
        // - Send email reminder
        // - Update task status
    }
}
