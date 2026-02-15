#if (UseAuth)
using Microsoft.AspNetCore.Identity;
using MinimalCleanArch.Domain.Entities;

namespace MCA.Domain.Entities;

public class ApplicationUser : IdentityUser<Guid>, IAuditableEntity
{
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
}
#endif
