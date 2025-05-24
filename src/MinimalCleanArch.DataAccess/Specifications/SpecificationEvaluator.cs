using Microsoft.EntityFrameworkCore;
using MinimalCleanArch.Domain.Entities;
using MinimalCleanArch.Specifications;

namespace MinimalCleanArch.DataAccess.Specifications;

/// <summary>
/// Evaluates specifications and applies them to IQueryable
/// </summary>
/// <typeparam name="T">The type of entity this evaluator applies to</typeparam>
public static class SpecificationEvaluator<T> where T : class
{
    /// <summary>
    /// Gets a query with the specification applied
    /// </summary>
    /// <param name="inputQuery">The input query</param>
    /// <param name="specification">The specification to apply</param>
    /// <returns>The query with the specification applied</returns>
    public static IQueryable<T> GetQuery(IQueryable<T> inputQuery, ISpecification<T> specification)
    {
        var query = inputQuery;

        // Apply soft delete filter if needed
        if (typeof(ISoftDelete).IsAssignableFrom(typeof(T)) && specification.IgnoreSoftDelete)
        {
            // No need to filter on IsDeleted here since we're ignoring the filter
            query = query.IgnoreQueryFilters();
        }

        // Apply criteria
        if (specification.Criteria != null)
        {
            query = query.Where(specification.Criteria);
        }

        // Apply includes
        query = specification.Includes.Aggregate(query, (current, include) => current.Include(include));
        query = specification.IncludeStrings.Aggregate(query, (current, include) => current.Include(include));

        // Apply ordering
        if (specification.OrderBy != null)
        {
            query = query.OrderBy(specification.OrderBy);
        }
        else if (specification.OrderByDescending != null)
        {
            query = query.OrderByDescending(specification.OrderByDescending);
        }

        // Apply additional ordering
        foreach (var thenBy in specification.ThenBys)
        {
            query = thenBy.Descending
                ? ((IOrderedQueryable<T>)query).ThenByDescending(thenBy.KeySelector)
                : ((IOrderedQueryable<T>)query).ThenBy(thenBy.KeySelector);
        }

        // Apply paging
        if (specification.Skip.HasValue)
        {
            query = query.Skip(specification.Skip.Value);
        }

        if (specification.Take.HasValue)
        {
            query = query.Take(specification.Take.Value);
        }

        // Apply no tracking if specified
        if (specification.AsNoTracking)
        {
            query = query.AsNoTracking();
        }

        return query;
    }
}
