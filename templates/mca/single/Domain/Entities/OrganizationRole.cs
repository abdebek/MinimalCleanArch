#if (UseAuth && UseMultiTenant)
using MinimalCleanArch.Domain.Entities;

namespace MCA.Domain.Entities;

/// <summary>
/// Org-scoped role row (data, not Identity seed constants).
/// </summary>
public class OrganizationRole : BaseEntity<Guid>, ITenantEntity
{
    public string Name { get; private set; } = string.Empty;
    public string TenantId { get; set; } = string.Empty;

    private OrganizationRole()
    {
    }

    public OrganizationRole(string name, string tenantId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        Id = Guid.NewGuid();
        Name = name.Trim();
        TenantId = tenantId;
    }
}
#endif
