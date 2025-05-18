using System.Linq.Expressions;

namespace MinimalCleanArch.Specifications;

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
    /// Gets a value indicating whether the query should be tracked by the EF Core change tracker
    /// </summary>
    bool AsNoTracking { get; }
    
    /// <summary>
    /// Gets a value indicating whether this specification is for a count-only query
    /// </summary>
    bool IsCountOnly { get; }
}
