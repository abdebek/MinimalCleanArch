using System.Linq.Expressions;
using MinimalCleanArch.Specifications;

namespace MinimalCleanArch.DataAccess.Specifications;

/// <summary>
/// Internal specification wrapping an ad-hoc filter expression so that the repository
/// query helpers route through <see cref="SpecificationEvaluator{T}"/> instead of
/// composing <c>DbSet</c> queries directly. Tracking behavior follows EF Core's
/// (host-configurable) default.
/// </summary>
/// <typeparam name="TEntity">The type of the entity</typeparam>
internal sealed class FilterSpecification<TEntity> : BaseSpecification<TEntity>
    where TEntity : class
{
    /// <summary>
    /// Creates a filter-less specification (matches all entities)
    /// </summary>
    public FilterSpecification()
    {
    }

    /// <summary>
    /// Creates a specification with the given filter expression
    /// </summary>
    /// <param name="filter">The filter expression</param>
    public FilterSpecification(Expression<Func<TEntity, bool>> filter) : base(filter)
    {
    }
}

/// <summary>
/// Internal count-only specification used by the repository count helpers. Inherits
/// the count-only no-tracking default from <see cref="BaseSpecification{T}"/>.
/// </summary>
/// <typeparam name="TEntity">The type of the entity</typeparam>
internal sealed class CountOnlySpecification<TEntity> : BaseSpecification<TEntity>
    where TEntity : class
{
    /// <summary>
    /// Creates a count-only specification with an optional filter expression
    /// </summary>
    /// <param name="filter">The optional filter expression</param>
    public CountOnlySpecification(Expression<Func<TEntity, bool>>? filter = null)
    {
        if (filter is not null)
        {
            AddCriteria(filter);
        }

        ForCountOnly();
    }
}
