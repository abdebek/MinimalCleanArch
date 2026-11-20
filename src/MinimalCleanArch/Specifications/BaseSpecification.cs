using System.Linq.Expressions;

namespace MinimalCleanArch.Specifications;

/// <summary>
/// Base implementation of <see cref="ISpecification{T}"/>
/// </summary>
/// <typeparam name="T">The type of entity this specification applies to</typeparam>
public abstract class BaseSpecification<T> : ISpecification<T>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BaseSpecification{T}"/> class
    /// </summary>
    protected BaseSpecification()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BaseSpecification{T}"/> class with a filter expression
    /// </summary>
    /// <param name="criteria">The filter expression</param>
    protected BaseSpecification(Expression<Func<T, bool>> criteria)
    {
        Criteria = criteria;
    }

    /// <summary>
    /// Gets the filter expression
    /// </summary>
    public Expression<Func<T, bool>>? Criteria { get; private set; }

    /// <summary>
    /// Gets the include expressions for eager loading related entities
    /// </summary>
    public List<Expression<Func<T, object>>> Includes { get; } = new();

    /// <summary>
    /// Gets the string-based include expressions for eager loading related entities
    /// </summary>
    public List<string> IncludeStrings { get; } = new();

    /// <summary>
    /// Gets the order by expression
    /// </summary>
    public Expression<Func<T, object>>? OrderBy { get; private set; }

    /// <summary>
    /// Gets the order by descending expression
    /// </summary>
    public Expression<Func<T, object>>? OrderByDescending { get; private set; }

    /// <summary>
    /// Gets the additional order by expressions
    /// </summary>
    public List<(Expression<Func<T, object>> KeySelector, bool Descending)> ThenBys { get; } = new();

    /// <summary>
    /// Gets the skip value for pagination
    /// </summary>
    public int? Skip { get; private set; }

    /// <summary>
    /// Gets the take value for pagination
    /// </summary>
    public int? Take { get; private set; }

    /// <summary>
    /// Gets a value indicating whether to include soft deleted entities
    /// </summary>
    public bool IgnoreSoftDelete { get; private set; }

    private QueryTrackingMode? _trackingMode;

    /// <summary>
    /// Gets the tracking mode for the query.
    /// Regular specifications default to <see cref="QueryTrackingMode.Default"/>, following EF Core's
    /// (host-configurable) tracking default. Count-only specifications default to
    /// <see cref="QueryTrackingMode.NoTracking"/> unless <see cref="UseTracking"/> is called explicitly.
    /// </summary>
    public QueryTrackingMode TrackingMode =>
        _trackingMode ?? (IsCountOnly ? QueryTrackingMode.NoTracking : QueryTrackingMode.Default);

    /// <summary>
    /// Gets a value indicating whether the query should be executed without change tracking.
    /// </summary>
    public bool AsNoTracking => TrackingMode == QueryTrackingMode.NoTracking;

    /// <summary>
    /// Gets a value indicating whether includes should be loaded with split query behavior.
    /// </summary>
    public bool AsSplitQuery { get; private set; }

    /// <summary>
    /// Gets a value indicating whether all global query filters should be ignored.
    /// </summary>
    public bool IgnoreQueryFilters { get; private set; }

    /// <summary>
    /// Gets a value indicating whether this specification is for a count-only query
    /// </summary>
    public bool IsCountOnly { get; private set; }

    /// <summary>
    /// Adds a filter expression to the specification
    /// </summary>
    /// <param name="criteria">The filter expression</param>
    protected void AddCriteria(Expression<Func<T, bool>> criteria)
    {
        Criteria = Criteria != null
            ? Criteria.AndAlso(criteria)
            : criteria;
    }

    /// <summary>
    /// Sets the filter expression, replacing any existing criteria.
    /// Useful when composing multiple specifications with a different operator.
    /// </summary>
    /// <param name="criteria">The filter expression</param>
    protected void SetCriteria(Expression<Func<T, bool>> criteria)
    {
        Criteria = criteria;
    }

    /// <summary>
    /// Adds an include expression for eager loading a related entity
    /// </summary>
    /// <param name="includeExpression">The include expression</param>
    protected void AddInclude(Expression<Func<T, object>> includeExpression)
    {
        Includes.Add(includeExpression);
    }

    /// <summary>
    /// Adds a string-based include expression for eager loading a related entity
    /// </summary>
    /// <param name="includeString">The include string</param>
    protected void AddInclude(string includeString)
    {
        IncludeStrings.Add(includeString);
    }

    /// <summary>
    /// Adds an order by expression
    /// </summary>
    /// <param name="orderByExpression">The order by expression</param>
    protected void ApplyOrderBy(Expression<Func<T, object>> orderByExpression)
    {
        OrderBy = orderByExpression;
    }

    /// <summary>
    /// Adds an order by descending expression
    /// </summary>
    /// <param name="orderByDescendingExpression">The order by descending expression</param>
    protected void ApplyOrderByDescending(Expression<Func<T, object>> orderByDescendingExpression)
    {
        OrderByDescending = orderByDescendingExpression;
    }

    /// <summary>
    /// Adds an additional order by expression
    /// </summary>
    /// <param name="thenByExpression">The then by expression</param>
    protected void ApplyThenBy(Expression<Func<T, object>> thenByExpression)
    {
        ThenBys.Add((thenByExpression, false));
    }

    /// <summary>
    /// Adds an additional order by descending expression
    /// </summary>
    /// <param name="thenByDescendingExpression">The then by descending expression</param>
    protected void ApplyThenByDescending(Expression<Func<T, object>> thenByDescendingExpression)
    {
        ThenBys.Add((thenByDescendingExpression, true));
    }

    /// <summary>
    /// Adds pagination to the specification
    /// </summary>
    /// <param name="skip">The number of elements to skip</param>
    /// <param name="take">The number of elements to take</param>
    protected void ApplyPaging(int skip, int take)
    {
        Skip = skip;
        Take = take;
    }

    /// <summary>
    /// Configures the specification to include soft deleted entities
    /// </summary>
    protected void IgnoreSoftDeleteFilter()
    {
        IgnoreSoftDelete = true;
    }

    /// <summary>
    /// Configures the specification to execute without change tracking,
    /// overriding the host's tracking default.
    /// </summary>
    protected void UseNoTracking()
    {
        _trackingMode = QueryTrackingMode.NoTracking;
    }

    /// <summary>
    /// Configures the specification to execute with change tracking enabled,
    /// overriding both a host-level no-tracking default and the count-only no-tracking default.
    /// </summary>
    protected void UseTracking()
    {
        _trackingMode = QueryTrackingMode.Tracking;
    }

    /// <summary>
    /// Configures the specification to use split query behavior for include graphs.
    /// </summary>
    protected void UseSplitQuery()
    {
        AsSplitQuery = true;
    }

    /// <summary>
    /// Configures the specification to ignore all EF Core global query filters.
    /// </summary>
    protected void IgnoreAllQueryFilters()
    {
        IgnoreQueryFilters = true;
    }

    /// <summary>
    /// Configures the specification for count-only queries. Count-only specifications
    /// default to <see cref="QueryTrackingMode.NoTracking"/> unless <see cref="UseTracking"/> is called explicitly.
    /// </summary>
    protected void ForCountOnly()
    {
        IsCountOnly = true;
    }
}

/// <summary>
/// Base implementation of <see cref="ISpecification{T, TResult}"/> for database-level projections.
/// </summary>
/// <typeparam name="T">The source entity type.</typeparam>
/// <typeparam name="TResult">The projected result type.</typeparam>
public abstract class BaseSpecification<T, TResult> : BaseSpecification<T>, ISpecification<T, TResult>
{
    private Expression<Func<T, TResult>>? _selector;

    /// <summary>
    /// Initializes a new instance of the <see cref="BaseSpecification{T, TResult}"/> class.
    /// </summary>
    protected BaseSpecification()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BaseSpecification{T, TResult}"/> class with a filter expression.
    /// </summary>
    /// <param name="criteria">The filter expression.</param>
    protected BaseSpecification(Expression<Func<T, bool>> criteria)
        : base(criteria)
    {
    }

    /// <inheritdoc />
    public Expression<Func<T, TResult>> Selector =>
        _selector ?? throw new InvalidOperationException(
            $"Projection specification '{GetType().Name}' must configure a selector via ApplySelector(...).");

    /// <summary>
    /// Configures the projection expression for this specification.
    /// </summary>
    /// <param name="selector">The projection expression.</param>
    protected void ApplySelector(Expression<Func<T, TResult>> selector)
    {
        _selector = selector ?? throw new ArgumentNullException(nameof(selector));
    }
}

/// <summary>
/// Extension methods for combining expressions
/// </summary>
internal static class ExpressionExtensions
{
    /// <summary>
    /// Combines two expressions with a logical AND operation
    /// </summary>
    /// <typeparam name="T">The type of the parameter in the expressions</typeparam>
    /// <param name="expr1">The first expression</param>
    /// <param name="expr2">The second expression</param>
    /// <returns>A new expression that combines the original expressions with a logical AND</returns>
    public static Expression<Func<T, bool>> AndAlso<T>(
        this Expression<Func<T, bool>> expr1,
        Expression<Func<T, bool>> expr2)
    {
        return Combine(expr1, expr2, Expression.AndAlso);
    }

    /// <summary>
    /// Combines two expressions with a logical OR operation
    /// </summary>
    /// <typeparam name="T">The type of the parameter in the expressions</typeparam>
    /// <param name="expr1">The first expression</param>
    /// <param name="expr2">The second expression</param>
    /// <returns>A new expression that combines the original expressions with a logical OR</returns>
    public static Expression<Func<T, bool>> OrElse<T>(
        this Expression<Func<T, bool>> expr1,
        Expression<Func<T, bool>> expr2)
    {
        return Combine(expr1, expr2, Expression.OrElse);
    }

    /// <summary>
    /// Negates an expression
    /// </summary>
    public static Expression<Func<T, bool>> Not<T>(
        this Expression<Func<T, bool>> expr)
    {
        var parameter = Expression.Parameter(typeof(T));
        var visitor = new ReplaceExpressionVisitor(expr.Parameters[0], parameter);
        var body = visitor.Visit(expr.Body);

        return Expression.Lambda<Func<T, bool>>(Expression.Not(body!), parameter);
    }

    private static Expression<Func<T, bool>> Combine<T>(
        Expression<Func<T, bool>> expr1,
        Expression<Func<T, bool>> expr2,
        Func<Expression, Expression, BinaryExpression> combiner)
    {
        var parameter = Expression.Parameter(typeof(T));

        var leftVisitor = new ReplaceExpressionVisitor(expr1.Parameters[0], parameter);
        var left = leftVisitor.Visit(expr1.Body);

        var rightVisitor = new ReplaceExpressionVisitor(expr2.Parameters[0], parameter);
        var right = rightVisitor.Visit(expr2.Body);

        return Expression.Lambda<Func<T, bool>>(combiner(left!, right!), parameter);
    }

    private class ReplaceExpressionVisitor : ExpressionVisitor
    {
        private readonly Expression _oldValue;
        private readonly Expression _newValue;

        public ReplaceExpressionVisitor(Expression oldValue, Expression newValue)
        {
            _oldValue = oldValue;
            _newValue = newValue;
        }

        public override Expression? Visit(Expression? node)
        {
            if (node == _oldValue)
                return _newValue;
            return base.Visit(node);
        }
    }
}
