using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using MinimalCleanArch.Domain.Entities;

namespace MinimalCleanArch.DataAccess;

/// <summary>
/// Combines soft-delete and tenant query filters. Tenant comparison uses a DbContext
/// property expression so EF re-evaluates <c>CurrentTenantId</c> per query (not at model compile).
/// Default isolation is the EF filter; Postgres RLS is not applied.
/// </summary>
internal static class QueryFilterConfiguration
{
    public static void Apply(ModelBuilder modelBuilder, Expression<Func<string?>> currentTenantId)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        ArgumentNullException.ThrowIfNull(currentTenantId);

        var currentTenant = currentTenantId.Body;

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (entityType.IsOwned())
            {
                continue;
            }

            var clr = entityType.ClrType;
            if (!clr.IsClass || clr.IsAbstract)
            {
                continue;
            }

            var isSoftDelete = typeof(ISoftDelete).IsAssignableFrom(clr);
            var isTenant = typeof(ITenantEntity).IsAssignableFrom(clr);
            if (!isSoftDelete && !isTenant)
            {
                continue;
            }

            var parameter = Expression.Parameter(clr, "e");
            Expression? body = null;

            if (isSoftDelete)
            {
                var isDeleted = Expression.Property(parameter, nameof(ISoftDelete.IsDeleted));
                body = Expression.Equal(isDeleted, Expression.Constant(false));
            }

            if (isTenant)
            {
                var tenantProperty = Expression.Property(parameter, nameof(ITenantEntity.TenantId));
                var hasTenant = Expression.NotEqual(
                    currentTenant,
                    Expression.Constant(null, typeof(string)));
                var matchesTenant = Expression.Equal(tenantProperty, currentTenant);
                var tenantFilter = Expression.AndAlso(hasTenant, matchesTenant);
                body = body is null ? tenantFilter : Expression.AndAlso(body, tenantFilter);
            }

            var lambda = Expression.Lambda(body!, parameter);
            modelBuilder.Entity(clr).HasQueryFilter(lambda);
        }
    }
}
