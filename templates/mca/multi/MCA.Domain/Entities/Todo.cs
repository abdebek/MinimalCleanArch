using MinimalCleanArch.Domain.Entities;
#if (UseMessaging)
using MinimalCleanArch.Domain.Events;
using MCA.Domain.Events;
#endif

namespace MCA.Domain.Entities;

#if (UseMessaging)
public class Todo : BaseSoftDeleteEntity, IHasDomainEvents
#else
public class Todo : BaseSoftDeleteEntity
#endif
{
#if (UseMessaging)
    private readonly DomainEventCollection _domainEvents = new();
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.DomainEvents;
#endif

    public string Title { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public bool IsCompleted { get; private set; }
    public int Priority { get; private set; }
    public DateTime? DueDate { get; private set; }
    public DateTime? DeletedAt { get; private set; }

    private Todo() { }

    public Todo(string title, string? description = null, int priority = 0, DateTime? dueDate = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        Title = title;
        Description = description;
        Priority = priority;
        DueDate = dueDate;
        IsCompleted = false;
        CreatedAt = DateTime.UtcNow;
        LastModifiedAt = DateTime.UtcNow;
#if (UseMessaging)
        _domainEvents.RaiseDomainEvent(new TodoCreatedEvent { EntityId = Id, Title = title });
#endif
    }

    public void Update(string title, string? description, int priority, DateTime? dueDate)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        Title = title;
        Description = description;
        Priority = priority;
        DueDate = dueDate;
        LastModifiedAt = DateTime.UtcNow;
#if (UseMessaging)
        _domainEvents.RaiseDomainEvent(new TodoUpdatedEvent { EntityId = Id, Title = title });
#endif
    }

    public void MarkAsCompleted()
    {
        if (IsCompleted) return;
        IsCompleted = true;
        LastModifiedAt = DateTime.UtcNow;
#if (UseMessaging)
        _domainEvents.RaiseDomainEvent(new TodoCompletedEvent { EntityId = Id, Title = Title });
#endif
    }

    public void MarkAsIncomplete()
    {
        IsCompleted = false;
        LastModifiedAt = DateTime.UtcNow;
    }

    public void Delete()
    {
        IsDeleted = true;
        DeletedAt = DateTime.UtcNow;
        LastModifiedAt = DateTime.UtcNow;
#if (UseMessaging)
        _domainEvents.RaiseDomainEvent(new TodoDeletedEvent { EntityId = Id, Title = Title });
#endif
    }

#if (UseMessaging)
    public void ClearDomainEvents() => _domainEvents.ClearDomainEvents();
#endif
}
