using MCA.Domain.Events;

namespace MCA.Application.Handlers;

#if (UseMessaging)
public class TodoEventHandlers
{
    public Task Handle(TodoCreatedEvent message, CancellationToken cancellationToken = default)
    {
        // Handle Todo created event (e.g., send notification, log)
        return Task.CompletedTask;
    }

    public Task Handle(TodoUpdatedEvent message, CancellationToken cancellationToken = default)
    {
        // Handle Todo updated event
        return Task.CompletedTask;
    }

    public Task Handle(TodoCompletedEvent message, CancellationToken cancellationToken = default)
    {
        // Handle Todo completed event
        return Task.CompletedTask;
    }

    public Task Handle(TodoDeletedEvent message, CancellationToken cancellationToken = default)
    {
        // Handle Todo deleted event
        return Task.CompletedTask;
    }
}
#endif
