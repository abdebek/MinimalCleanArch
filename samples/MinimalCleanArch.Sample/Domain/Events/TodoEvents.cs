using MinimalCleanArch.Domain.Events;

namespace MinimalCleanArch.Sample.Domain.Events;

/// <summary>
/// Event raised when a new todo is created.
/// </summary>
public record TodoCreatedEvent : EntityDomainEvent<int>
{
    /// <summary>
    /// Gets the title of the created todo.
    /// </summary>
    public required string Title { get; init; }

    /// <summary>
    /// Gets the priority of the created todo.
    /// </summary>
    public int Priority { get; init; }
}

/// <summary>
/// Event raised when a todo is marked as completed.
/// </summary>
public record TodoCompletedEvent : EntityDomainEvent<int>
{
    /// <summary>
    /// Gets the title of the completed todo.
    /// </summary>
    public required string Title { get; init; }

    /// <summary>
    /// Gets when the todo was completed.
    /// </summary>
    public DateTimeOffset CompletedAt { get; init; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// Event raised when a todo is updated.
/// </summary>
public record TodoUpdatedEvent : EntityDomainEvent<int>
{
    /// <summary>
    /// Gets the new title of the todo.
    /// </summary>
    public required string Title { get; init; }

    /// <summary>
    /// Gets the new priority of the todo.
    /// </summary>
    public int Priority { get; init; }
}

/// <summary>
/// Event raised when a todo is deleted.
/// </summary>
public record TodoDeletedEvent : EntityDomainEvent<int>
{
    /// <summary>
    /// Gets the title of the deleted todo.
    /// </summary>
    public required string Title { get; init; }
}

/// <summary>
/// Event raised when a high-priority todo is created or becomes overdue.
/// This demonstrates scheduling events for later processing.
/// </summary>
public record TodoReminderEvent : EntityDomainEvent<int>
{
    /// <summary>
    /// Gets the title of the todo.
    /// </summary>
    public required string Title { get; init; }

    /// <summary>
    /// Gets the due date of the todo.
    /// </summary>
    public DateTime? DueDate { get; init; }

    /// <summary>
    /// Gets the priority of the todo.
    /// </summary>
    public int Priority { get; init; }
}
