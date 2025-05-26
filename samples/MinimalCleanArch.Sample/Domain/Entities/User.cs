using Microsoft.AspNetCore.Identity;
using MinimalCleanArch.Domain.Entities;
using MinimalCleanArch.Security.Encryption;

namespace MinimalCleanArch.Sample.Domain.Entities;

/// <summary>
/// Custom Identity user entity
/// </summary>
public class User : IdentityUser, IAuditableEntity, ISoftDelete
{
    /// <summary>
    /// Gets or sets the full name
    /// </summary>
    public string? FullName { get; set; }

    /// <summary>
    /// Gets or sets the date of birth
    /// </summary>
    public DateTime? DateOfBirth { get; set; }

    /// <summary>
    /// Gets or sets encrypted personal information
    /// </summary>
    [Encrypted]
    public string? PersonalNotes { get; set; }

    // IAuditableEntity implementation
    public DateTime CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? LastModifiedAt { get; set; }
    public string? LastModifiedBy { get; set; }

    // ISoftDelete implementation
    public bool IsDeleted { get; set; }
}
