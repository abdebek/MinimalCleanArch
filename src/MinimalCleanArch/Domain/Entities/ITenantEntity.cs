namespace MinimalCleanArch.Domain.Entities;

/// <summary>
/// Marks an entity as owned by a tenant. DataAccess applies a fail-closed EF query filter
/// (<c>TenantId == IExecutionContext.TenantId</c>) and stamps <see cref="TenantId"/> on insert.
/// </summary>
public interface ITenantEntity
{
    /// <summary>
    /// Gets or sets the tenant that owns this row.
    /// </summary>
    string TenantId { get; set; }
}
