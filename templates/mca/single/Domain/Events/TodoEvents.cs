using MinimalCleanArch.Domain.Events;

namespace MCA.Domain.Events;

public class TodoCreatedEvent : DomainEvent
{
    public int EntityId { get; set; }
    public string Title { get; set; } = string.Empty;
}

public class TodoUpdatedEvent : DomainEvent
{
    public int EntityId { get; set; }
    public string Title { get; set; } = string.Empty;
}

public class TodoCompletedEvent : DomainEvent
{
    public int EntityId { get; set; }
    public string Title { get; set; } = string.Empty;
}

public class TodoDeletedEvent : DomainEvent
{
    public int EntityId { get; set; }
    public string Title { get; set; } = string.Empty;
}
