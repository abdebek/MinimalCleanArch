using MinimalCleanArch.Domain.Events;

namespace MCA.Domain.Events;

/// <summary>
/// Event raised after a new user is registered.
/// </summary>
public record UserRegisteredEvent : EntityDomainEvent<Guid>
{
    public required string Email { get; init; }
}
