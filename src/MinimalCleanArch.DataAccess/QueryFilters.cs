using Microsoft.EntityFrameworkCore;

namespace MinimalCleanArch.DataAccess;

/// <summary>
/// Names for EF Core query filters. On EF 10+, <see cref="IgnoreSoftDelete{T}"/> disables
/// only the soft-delete filter so tenant isolation stays in place.
/// </summary>
public static class QueryFilters
{
    public const string SoftDelete = "SoftDelete";
    public const string Tenant = "Tenant";

    /// <summary>
    /// Include soft-deleted rows. Tenant filter remains on EF 10+; on EF 9 this
    /// falls back to <see cref="EntityFrameworkQueryableExtensions.IgnoreQueryFilters{T}(IQueryable{T})"/>
    /// and callers must re-apply tenant scope.
    /// </summary>
    public static IQueryable<T> IgnoreSoftDelete<T>(this IQueryable<T> source)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(source);
#if NET10_0_OR_GREATER
        return source.IgnoreQueryFilters([SoftDelete]);
#else
        return source.IgnoreQueryFilters();
#endif
    }
}
