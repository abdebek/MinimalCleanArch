using MCA.Domain.Events;

namespace MCA.Application.Handlers;

#if (UseMessaging)
public class TodoEventHandler
{
    private readonly ILogger<TodoEventHandler> _logger;

    public TodoEventHandler(ILogger<TodoEventHandler> logger)
    {
        _logger = logger;
    }

    public Task Handle(TodoCreatedEvent message, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Todo created: {Id}", message.EntityId);
        return Task.CompletedTask;
    }

    public Task Handle(TodoUpdatedEvent message, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Todo updated: {Id}", message.EntityId);
        return Task.CompletedTask;
    }

    public Task Handle(TodoCompletedEvent message, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Todo completed: {Id}", message.EntityId);
        return Task.CompletedTask;
    }

    public Task Handle(TodoDeletedEvent message, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Todo deleted: {Id}", message.EntityId);
        return Task.CompletedTask;
    }
}
#endif
