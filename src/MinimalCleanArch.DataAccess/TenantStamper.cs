using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using MinimalCleanArch.Domain.Entities;

namespace MinimalCleanArch.DataAccess;

/// <summary>
/// Stamps <see cref="ITenantEntity.TenantId"/> on insert from the current execution context.
/// Fail-closed: an insert without a tenant id throws.
/// </summary>
internal static class TenantStamper
{
    public static void Apply(ChangeTracker changeTracker, string? tenantId)
    {
        ArgumentNullException.ThrowIfNull(changeTracker);

        foreach (var entry in changeTracker.Entries())
        {
            if (entry.Entity is not ITenantEntity tenantEntity)
            {
                continue;
            }

            if (entry.State == EntityState.Added)
            {
                if (string.IsNullOrWhiteSpace(tenantEntity.TenantId))
                {
                    if (string.IsNullOrWhiteSpace(tenantId))
                    {
                        throw new InvalidOperationException(
                            $"Cannot persist {entry.Entity.GetType().Name} without a tenant. " +
                            "Set ITenantEntity.TenantId or IExecutionContext.TenantId.");
                    }

                    tenantEntity.TenantId = tenantId;
                }
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Property(nameof(ITenantEntity.TenantId)).IsModified = false;
            }
        }
    }
}
