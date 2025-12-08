using MinimalCleanArch.Domain;
#if (UseMessaging)
using MinimalCleanArch.Domain.Events;
using MCA.Domain.Events;
#endif

namespace MCA.Domain.Entities;

/// <summary>
/// Sample Todo entity demonstrating domain-driven design patterns.
/// </summary>
#if (UseMessaging)
public class Todo : Entity<int>, ISoftDelete, IHasDomainEvents
#else
public class Todo : Entity<int>, ISoftDelete
#endif
{
#if (UseMessaging)
    private readonly List<IDomainEvent> _domainEvents = new();
#endif

    public string Title { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public bool IsCompleted { get; private set; }
    public int Priority { get; private set; }
    public DateTime? DueDate { get; private set; }
    public bool IsDeleted { get; private set; }
    public DateTime? DeletedAt { get; private set; }

#if (UseMessaging)
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();
#endif

    private Todo() { } // EF Core constructor

    public Todo(string title, string? description = null, int priority = 0, DateTime? dueDate = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);

        Title = title;
        Description = description;
        Priority = priority;
        DueDate = dueDate;
        IsCompleted = false;

#if (UseMessaging)
        _domainEvents.Add(new TodoCreatedEvent { EntityId = Id, Title = title });
#endif
    }

    public void Update(string title, string? description, int priority, DateTime? dueDate)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);

        Title = title;
        Description = description;
        Priority = priority;
        DueDate = dueDate;

#if (UseMessaging)
        _domainEvents.Add(new TodoUpdatedEvent { EntityId = Id, Title = title });
#endif
    }

    public void MarkAsCompleted()
    {
        if (IsCompleted) return;

        IsCompleted = true;

#if (UseMessaging)
        _domainEvents.Add(new TodoCompletedEvent { EntityId = Id, Title = Title });
#endif
    }

    public void MarkAsIncomplete()
    {
        IsCompleted = false;
    }

    public void Delete()
    {
        IsDeleted = true;
        DeletedAt = DateTime.UtcNow;

#if (UseMessaging)
        _domainEvents.Add(new TodoDeletedEvent { EntityId = Id, Title = Title });
#endif
    }

#if (UseMessaging)
    public void ClearDomainEvents() => _domainEvents.Clear();
#endif
}
