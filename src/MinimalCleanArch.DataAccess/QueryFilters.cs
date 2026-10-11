using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using MinimalCleanArch.Domain.Entities;

namespace MinimalCleanArch.DataAccess;

/// <summary>
/// Query-filter names and helpers. Tenant isolation is kept on net9 and net10 when
/// including soft-deleted rows. Named filters are the EF 10 mechanism; net9 re-applies
/// the tenant predicate after <c>IgnoreQueryFilters()</c>.
/// </summary>
public static class QueryFilters
{
    public const string SoftDelete = "SoftDelete";
    public const string Tenant = "Tenant";

    /// <summary>
    /// Include soft-deleted rows without dropping tenant isolation.
    /// EF 10: disables only the named <see cref="SoftDelete"/> filter.
    /// EF 9: disables the combined filter, then re-applies tenant from the DbContext
    /// (fail-closed when TenantId is null).
    /// </summary>
    public static IQueryable<T> IgnoreSoftDelete<T>(this IQueryable<T> source)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(source);
#if NET10_0_OR_GREATER
        return source.IgnoreQueryFilters([SoftDelete]);
#else
        var query = source.IgnoreQueryFilters();
        if (!typeof(ITenantEntity).IsAssignableFrom(typeof(T)))
        {
            return query;
        }

        var tenantId = TryGetTenantId(source);
        var parameter = Expression.Parameter(typeof(T), "e");
        var property = Expression.Property(parameter, nameof(ITenantEntity.TenantId));
        Expression body = tenantId is null
            ? Expression.Constant(false)
            : Expression.Equal(property, Expression.Constant(tenantId, typeof(string)));
        return query.Where(Expression.Lambda<Func<T, bool>>(body, parameter));
#endif
    }

#if !NET10_0_OR_GREATER
    private static string? TryGetTenantId(IQueryable source)
    {
        var infrastructure = source as IInfrastructure<IServiceProvider>
            ?? source.Provider as IInfrastructure<IServiceProvider>;
        var current = infrastructure?.GetService<ICurrentDbContext>();
        return current?.Context is IQueryFilterTenantContext tenant
            ? tenant.QueryFilterTenantId
            : null;
    }
#endif
}
