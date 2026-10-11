using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using MinimalCleanArch.Domain.Entities;

namespace MinimalCleanArch.DataAccess;

/// <summary>
/// Applies soft-delete and tenant query filters. On EF 10+ they are named
/// (<see cref="QueryFilters.SoftDelete"/>, <see cref="QueryFilters.Tenant"/>) so
/// <see cref="QueryFilters.IgnoreSoftDelete{T}"/> can drop only soft-delete.
/// Tenant comparison uses a DbContext property expression so EF re-evaluates
/// <c>CurrentTenantId</c> per query (not at model compile). Postgres RLS is not applied.
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

            var builder = modelBuilder.Entity(clr);

#if NET10_0_OR_GREATER
            if (isSoftDelete)
            {
                builder.HasQueryFilter(QueryFilters.SoftDelete, SoftDeleteLambda(clr));
            }

            if (isTenant)
            {
                builder.HasQueryFilter(QueryFilters.Tenant, TenantLambda(clr, currentTenant));
            }
#else
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

            builder.HasQueryFilter(Expression.Lambda(body!, parameter));
#endif
        }
    }

    private static LambdaExpression SoftDeleteLambda(Type clr)
    {
        var parameter = Expression.Parameter(clr, "e");
        var isDeleted = Expression.Property(parameter, nameof(ISoftDelete.IsDeleted));
        var body = Expression.Equal(isDeleted, Expression.Constant(false));
        return Expression.Lambda(body, parameter);
    }

    private static LambdaExpression TenantLambda(Type clr, Expression currentTenant)
    {
        var parameter = Expression.Parameter(clr, "e");
        var tenantProperty = Expression.Property(parameter, nameof(ITenantEntity.TenantId));
        var hasTenant = Expression.NotEqual(
            currentTenant,
            Expression.Constant(null, typeof(string)));
        var matchesTenant = Expression.Equal(tenantProperty, currentTenant);
        return Expression.Lambda(Expression.AndAlso(hasTenant, matchesTenant), parameter);
    }
}
