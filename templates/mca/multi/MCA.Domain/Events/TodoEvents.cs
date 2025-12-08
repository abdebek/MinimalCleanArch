using MinimalCleanArch.Domain.Events;

namespace MCA.Domain.Events;

/// <summary>
/// Event raised when a new Todo is created.
/// </summary>
public record TodoCreatedEvent : EntityDomainEvent<int>
{
    public required string Title { get; init; }
}

/// <summary>
/// Event raised when a Todo is updated.
/// </summary>
public record TodoUpdatedEvent : EntityDomainEvent<int>
{
    public required string Title { get; init; }
}

/// <summary>
/// Event raised when a Todo is marked as completed.
/// </summary>
public record TodoCompletedEvent : EntityDomainEvent<int>
{
    public required string Title { get; init; }
}

/// <summary>
/// Event raised when a Todo is deleted.
/// </summary>
public record TodoDeletedEvent : EntityDomainEvent<int>
{
    public required string Title { get; init; }
}
