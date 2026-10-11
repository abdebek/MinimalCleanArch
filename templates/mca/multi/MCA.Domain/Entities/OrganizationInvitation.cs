#if (UseAuth && UseMultiTenant)
using System.Security.Cryptography;
using MinimalCleanArch.Domain.Entities;

namespace MCA.Domain.Entities;

/// <summary>
/// Invite-by-code (optional email). Not <see cref="ITenantEntity"/> so join-by-code can look it up
/// before the invitee's tenant is switched.
/// </summary>
public class OrganizationInvitation : BaseEntity<Guid>
{
    public Guid OrganizationId { get; private set; }
    public string TenantId { get; private set; } = string.Empty;
    public string? Email { get; private set; }
    public string Role { get; private set; } = string.Empty;
    public string Code { get; private set; } = string.Empty;
    public DateTime ExpiresAt { get; private set; }
    public DateTime? AcceptedAt { get; private set; }
    public Guid? AcceptedByUserId { get; private set; }
    public Guid CreatedByUserId { get; private set; }

    private OrganizationInvitation()
    {
    }

    public bool IsPending => AcceptedAt is null && ExpiresAt > DateTime.UtcNow;

    public static OrganizationInvitation Create(
        Guid organizationId,
        string tenantId,
        string role,
        Guid createdByUserId,
        string? email = null,
        int expiresInDays = 7)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(role);
        if (expiresInDays <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(expiresInDays));
        }

        return new OrganizationInvitation
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            TenantId = tenantId,
            Email = string.IsNullOrWhiteSpace(email) ? null : email.Trim(),
            Role = role.Trim(),
            Code = Convert.ToHexString(RandomNumberGenerator.GetBytes(8)),
            ExpiresAt = DateTime.UtcNow.AddDays(expiresInDays),
            CreatedByUserId = createdByUserId
        };
    }

    public void Accept(Guid userId)
    {
        AcceptedAt = DateTime.UtcNow;
        AcceptedByUserId = userId;
    }
}
#endif
