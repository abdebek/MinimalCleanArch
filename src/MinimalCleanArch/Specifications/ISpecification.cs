using System.Linq.Expressions;

namespace MinimalCleanArch.Specifications;

/// <summary>
/// Specifies how a specification query interacts with EF Core change tracking.
/// </summary>
public enum QueryTrackingMode
{
    /// <summary>
    /// Leave tracking untouched so the query follows the host's configured
    /// <c>UseQueryTrackingBehavior(...)</c> default.
    /// </summary>
    Default = 0,

    /// <summary>
    /// Execute the query without change tracking.
    /// </summary>
    NoTracking = 1,

    /// <summary>
    /// Execute the query with change tracking enabled, even if the host
    /// configured a global no-tracking default.
    /// </summary>
    Tracking = 2
}

/// <summary>
/// Specification pattern interface for querying data
/// </summary>
/// <typeparam name="T">The type of entity this specification applies to</typeparam>
public interface ISpecification<T>
{
    /// <summary>
    /// Gets the filter expression
    /// </summary>
    Expression<Func<T, bool>>? Criteria { get; }

    /// <summary>
    /// Gets the include expressions for eager loading related entities
    /// </summary>
    List<Expression<Func<T, object>>> Includes { get; }

    /// <summary>
    /// Gets the string-based include expressions for eager loading related entities
    /// </summary>
    List<string> IncludeStrings { get; }

    /// <summary>
    /// Gets the order by expression
    /// </summary>
    Expression<Func<T, object>>? OrderBy { get; }

    /// <summary>
    /// Gets the order by descending expression
    /// </summary>
    Expression<Func<T, object>>? OrderByDescending { get; }

    /// <summary>
    /// Gets the additional order by expressions
    /// </summary>
    List<(Expression<Func<T, object>> KeySelector, bool Descending)> ThenBys { get; }

    /// <summary>
    /// Gets the skip value for pagination
    /// </summary>
    int? Skip { get; }

    /// <summary>
    /// Gets the take value for pagination
    /// </summary>
    int? Take { get; }

    /// <summary>
    /// Gets a value indicating whether to include soft deleted entities
    /// </summary>
    bool IgnoreSoftDelete { get; }

    /// <summary>
    /// Gets a value indicating whether the query should be executed without EF Core change tracking.
    /// </summary>
    bool AsNoTracking { get; }

    /// <summary>
    /// Gets the tracking mode for the query.
    /// Defaults to <see cref="QueryTrackingMode.NoTracking"/> when <see cref="AsNoTracking"/> is <see langword="true"/>,
    /// otherwise <see cref="QueryTrackingMode.Default"/>.
    /// </summary>
    QueryTrackingMode TrackingMode => AsNoTracking
        ? QueryTrackingMode.NoTracking
        : QueryTrackingMode.Default;

    /// <summary>
    /// Gets a value indicating whether collection includes should be executed as split queries.
    /// </summary>
    bool AsSplitQuery { get; }

    /// <summary>
    /// Gets a value indicating whether all global query filters should be ignored.
    /// </summary>
    bool IgnoreQueryFilters { get; }

    /// <summary>
    /// Gets a value indicating whether this specification is for a count-only query
    /// </summary>
    bool IsCountOnly { get; }
}

/// <summary>
/// Projection specification interface that filters, orders, pages, and projects entities to <typeparamref name="TResult"/>.
/// </summary>
/// <typeparam name="T">The source entity type.</typeparam>
/// <typeparam name="TResult">The projected result type.</typeparam>
public interface ISpecification<T, TResult> : ISpecification<T>
{
    /// <summary>
    /// Gets the projection expression that maps each entity to <typeparamref name="TResult"/>.
    /// </summary>
    Expression<Func<T, TResult>> Selector { get; }
}
