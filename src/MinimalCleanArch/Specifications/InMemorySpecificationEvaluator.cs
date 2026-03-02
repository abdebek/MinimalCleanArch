using System.Linq;

namespace MinimalCleanArch.Specifications;

/// <summary>
/// Evaluates specifications against in-memory collections.
/// Useful for fast unit tests that do not require EF Core.
/// </summary>
public static class InMemorySpecificationEvaluator
{
    /// <summary>
    /// Applies the specification to an in-memory collection.
    /// </summary>
    public static IReadOnlyList<T> Evaluate<T>(IEnumerable<T> source, ISpecification<T> specification)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(specification);

        var query = source.AsQueryable();

        if (specification.Criteria is not null)
        {
            query = query.Where(specification.Criteria);
        }

        if (specification.IsCountOnly)
        {
            return query.ToList();
        }

        if (specification.OrderBy is not null)
        {
            query = query.OrderBy(specification.OrderBy);
        }
        else if (specification.OrderByDescending is not null)
        {
            query = query.OrderByDescending(specification.OrderByDescending);
        }

        foreach (var thenBy in specification.ThenBys)
        {
            if (query is IOrderedQueryable<T> ordered)
            {
                query = thenBy.Descending
                    ? ordered.ThenByDescending(thenBy.KeySelector)
                    : ordered.ThenBy(thenBy.KeySelector);
            }
        }

        if (specification.Skip.HasValue)
        {
            query = query.Skip(specification.Skip.Value);
        }

        if (specification.Take.HasValue)
        {
            query = query.Take(specification.Take.Value);
        }

        return query.ToList();
    }

    /// <summary>
    /// Checks whether a single entity satisfies the specification's criteria.
    /// </summary>
    public static bool IsSatisfiedBy<T>(T entity, ISpecification<T> specification)
    {
        ArgumentNullException.ThrowIfNull(specification);
        return specification.Criteria?.Compile()(entity) ?? true;
    }
}
