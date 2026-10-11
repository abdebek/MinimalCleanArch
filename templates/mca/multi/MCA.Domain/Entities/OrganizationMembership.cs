#if (UseAuth && UseMultiTenant)
using MinimalCleanArch.Domain.Entities;

namespace MCA.Domain.Entities;

/// <summary>
/// User membership in an organization. <see cref="Role"/> is the org-role name stored as data.
/// </summary>
public class OrganizationMembership : BaseEntity<Guid>, ITenantEntity
{
    public Guid OrganizationId { get; private set; }
    public Guid UserId { get; private set; }
    public string Role { get; private set; } = string.Empty;
    public string TenantId { get; set; } = string.Empty;

    private OrganizationMembership()
    {
    }

    public OrganizationMembership(Guid organizationId, Guid userId, string role, string tenantId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(role);
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        Id = Guid.NewGuid();
        OrganizationId = organizationId;
        UserId = userId;
        Role = role.Trim();
        TenantId = tenantId;
    }
}
#endif
