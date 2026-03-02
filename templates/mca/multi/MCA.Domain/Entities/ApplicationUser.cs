using Microsoft.AspNetCore.Identity;
using MinimalCleanArch.Domain.Entities;
#if (UseMessaging)
using MCA.Domain.Events;
using MinimalCleanArch.Domain.Events;
#endif

namespace MCA.Domain.Entities;

public class ApplicationUser : IdentityUser<Guid>, IAuditableEntity
#if (UseMessaging)
    , IHasDomainEvents
#endif
{
#if (UseMessaging)
    private readonly DomainEventCollection _domainEvents = new();
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.DomainEvents;
#endif

    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? LastModifiedAt { get; set; }
    public string? LastModifiedBy { get; set; }

    public ApplicationUser() { }

    public ApplicationUser(string firstName, string lastName, string email, string? userName = null)
    {
        Id = Guid.NewGuid();
        FirstName = firstName;
        LastName = lastName;
        Email = email;
        UserName = userName ?? email;
    }

#if (UseMessaging)
    public void MarkAsRegistered()
    {
        _domainEvents.RaiseDomainEvent(new UserRegisteredEvent
        {
            EntityId = Id,
            Email = Email ?? string.Empty
        });
    }

    public void ClearDomainEvents() => _domainEvents.ClearDomainEvents();
#endif
}
