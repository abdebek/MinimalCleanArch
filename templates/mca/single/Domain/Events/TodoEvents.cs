using MinimalCleanArch.Domain.Events;

namespace MCA.Domain.Events;

public record TodoCreatedEvent : EntityDomainEvent<int>
{
    public required string Title { get; init; }
}

public record TodoUpdatedEvent : EntityDomainEvent<int>
{
    public required string Title { get; init; }
}

public record TodoCompletedEvent : EntityDomainEvent<int>
{
    public required string Title { get; init; }
}

public record TodoDeletedEvent : EntityDomainEvent<int>
{
    public required string Title { get; init; }
}
