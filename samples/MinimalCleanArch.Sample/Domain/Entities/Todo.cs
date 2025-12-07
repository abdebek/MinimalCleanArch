using MinimalCleanArch.Domain.Entities;
using MinimalCleanArch.Domain.Events;
using MinimalCleanArch.Domain.Exceptions;
using MinimalCleanArch.Sample.Domain.Events;
using MinimalCleanArch.Security.Encryption;

namespace MinimalCleanArch.Sample.Domain.Entities;

/// <summary>
/// Todo entity with business rules and domain events.
/// </summary>
public class Todo : BaseSoftDeleteEntity, IHasDomainEvents
{
    private readonly DomainEventCollection _domainEvents = new();

    /// <inheritdoc />
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.DomainEvents;

    /// <inheritdoc />
    public void ClearDomainEvents() => _domainEvents.ClearDomainEvents();
    /// <summary>
    /// Gets or sets the title
    /// </summary>
    public string Title { get; private set; } = string.Empty;

    /// <summary>
    /// Gets or sets the description, which is encrypted in the database
    /// </summary>
    [Encrypted]
    public string Description { get; private set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether this todo is completed
    /// </summary>
    public bool IsCompleted { get; private set; }

    /// <summary>
    /// Gets or sets the priority
    /// </summary>
    public int Priority { get; private set; }

    /// <summary>
    /// Gets or sets the due date
    /// </summary>
    public DateTime? DueDate { get; private set; }

    /// <summary>
    /// Private constructor for EF Core
    /// </summary>
    private Todo()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Todo"/> class
    /// </summary>
    /// <param name="title">The title</param>
    /// <param name="description">The description</param>
    /// <param name="priority">The priority</param>
    /// <param name="dueDate">The due date</param>
    public Todo(string title, string description, int priority = 0, DateTime? dueDate = null)
    {
        SetTitle(title);
        SetDescription(description);
        SetPriority(priority);
        SetDueDate(dueDate);
        IsCompleted = false;

        // Raise domain event
        _domainEvents.RaiseDomainEvent(new TodoCreatedEvent
        {
            EntityId = Id,
            Title = title,
            Priority = priority
        });
    }

    public void Update(string title, string description, int priority, DateTime? dueDate)
    {
        SetTitle(title);
        SetDescription(description);
        SetPriority(priority);
        SetDueDate(dueDate);

        // Raise domain event
        _domainEvents.RaiseDomainEvent(new TodoUpdatedEvent
        {
            EntityId = Id,
            Title = title,
            Priority = priority
        });
    }

    /// <summary>
    /// Sets the title
    /// </summary>
    /// <param name="title">The title</param>
    /// <exception cref="DomainException">Thrown when the title is invalid</exception>
    public void SetTitle(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new DomainException("Title cannot be empty");
        }

        if (title.Length > 100)
        {
            throw new DomainException("Title cannot be longer than 100 characters");
        }

        Title = title;
    }

    /// <summary>
    /// Sets the description
    /// </summary>
    /// <param name="description">The description</param>
    public void SetDescription(string description)
    {
        Description = description ?? string.Empty;
    }

    /// <summary>
    /// Sets the priority
    /// </summary>
    /// <param name="priority">The priority</param>
    /// <exception cref="DomainException">Thrown when the priority is invalid</exception>
    public void SetPriority(int priority)
    {
        if (priority < 0 || priority > 5)
        {
            throw new DomainException("Priority must be between 0 and 5");
        }

        Priority = priority;
    }

    /// <summary>
    /// Sets the due date
    /// </summary>
    /// <param name="dueDate">The due date</param>
    public void SetDueDate(DateTime? dueDate)
    {
        DueDate = dueDate;
    }

    /// <summary>
    /// Marks this todo as completed
    /// </summary>
    public void MarkAsCompleted()
    {
        IsCompleted = true;

        // Raise domain event
        _domainEvents.RaiseDomainEvent(new TodoCompletedEvent
        {
            EntityId = Id,
            Title = Title
        });
    }

    /// <summary>
    /// Marks this todo as not completed
    /// </summary>
    public void MarkAsNotCompleted()
    {
        IsCompleted = false;
    }
}
