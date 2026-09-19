namespace MinimalCleanArch.DataAccess;

/// <summary>
/// Lets <see cref="QueryFilters.IgnoreSoftDelete{T}"/> re-apply tenant scope on EF 9,
/// where named filters do not exist.
/// </summary>
internal interface IQueryFilterTenantContext
{
    string? QueryFilterTenantId { get; }
}
