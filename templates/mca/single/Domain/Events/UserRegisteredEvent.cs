using MinimalCleanArch.Domain.Events;

namespace MCA.Domain.Events;

public record UserRegisteredEvent : EntityDomainEvent<Guid>
{
    public required string Email { get; init; }
}
