using Microsoft.EntityFrameworkCore;

namespace MinimalCleanArch.DataAccess;

/// <summary>
/// Query-filter names and helpers. Tenant isolation is kept when
/// including soft-deleted rows by using named filters.
/// </summary>
public static class QueryFilters
{
    public const string SoftDelete = "SoftDelete";
    public const string Tenant = "Tenant";

    /// <summary>
    /// Include soft-deleted rows without dropping tenant isolation by disabling
    /// only the named <see cref="SoftDelete"/> filter.
    /// </summary>
    public static IQueryable<T> IgnoreSoftDelete<T>(this IQueryable<T> source)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(source);
        return source.IgnoreQueryFilters([SoftDelete]);
    }
}
