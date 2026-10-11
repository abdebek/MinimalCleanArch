#if (UseAuth && UseMultiTenant)
using MinimalCleanArch.Domain.Entities;

namespace MCA.Domain.Entities;

/// <summary>
/// Tenant organization. <see cref="Id"/> matches the current user's tenant guid so existing
/// Todos stay in the same isolation scope when the org is created.
/// </summary>
public class Organization : BaseAuditableEntity<Guid>, ITenantEntity
{
    public string Name { get; private set; } = string.Empty;
    public string TenantId { get; set; } = string.Empty;

    private Organization()
    {
    }

    public Organization(Guid id, string name, string tenantId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        Id = id;
        Name = name.Trim();
        TenantId = tenantId;
        CreatedAt = DateTime.UtcNow;
        LastModifiedAt = CreatedAt;
    }
}
#endif
