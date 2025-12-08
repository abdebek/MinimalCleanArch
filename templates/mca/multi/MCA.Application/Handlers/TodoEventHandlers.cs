using MCA.Domain.Events;
using Microsoft.Extensions.Logging;

namespace MCA.Application.Handlers;

/// <summary>
/// Wolverine message handlers for Todo domain events.
/// Handlers are discovered by convention (static or instance Handle methods).
/// </summary>
public class TodoEventHandlers
{
    private readonly ILogger<TodoEventHandlers> _logger;

    public TodoEventHandlers(ILogger<TodoEventHandlers> logger)
    {
        _logger = logger;
    }

    public void Handle(TodoCreatedEvent @event)
    {
        _logger.LogInformation("Todo created: {TodoId} - {Title}", @event.EntityId, @event.Title);
        // Add your business logic here (e.g., send notifications, update read models)
    }

    public void Handle(TodoCompletedEvent @event)
    {
        _logger.LogInformation("Todo completed: {TodoId} - {Title}", @event.EntityId, @event.Title);
        // Add your business logic here
    }

    public void Handle(TodoUpdatedEvent @event)
    {
        _logger.LogInformation("Todo updated: {TodoId} - {Title}", @event.EntityId, @event.Title);
        // Add your business logic here
    }

    public void Handle(TodoDeletedEvent @event)
    {
        _logger.LogInformation("Todo deleted: {TodoId} - {Title}", @event.EntityId, @event.Title);
        // Add your business logic here
    }
}
