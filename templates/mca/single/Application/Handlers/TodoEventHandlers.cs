using MCA.Domain.Events;
#if (UseMessaging)
using MinimalCleanArch.Messaging;
#endif

namespace MCA.Application.Handlers;

#if (UseMessaging)
public class TodoEventHandlers :
    IMessageHandler<TodoCreatedEvent>,
    IMessageHandler<TodoUpdatedEvent>,
    IMessageHandler<TodoCompletedEvent>,
    IMessageHandler<TodoDeletedEvent>
{
    public Task HandleAsync(TodoCreatedEvent message, CancellationToken cancellationToken)
    {
        // Handle Todo created event (e.g., send notification, log)
        return Task.CompletedTask;
    }

    public Task HandleAsync(TodoUpdatedEvent message, CancellationToken cancellationToken)
    {
        // Handle Todo updated event
        return Task.CompletedTask;
    }

    public Task HandleAsync(TodoCompletedEvent message, CancellationToken cancellationToken)
    {
        // Handle Todo completed event
        return Task.CompletedTask;
    }

    public Task HandleAsync(TodoDeletedEvent message, CancellationToken cancellationToken)
    {
        // Handle Todo deleted event
        return Task.CompletedTask;
    }
}
#endif
