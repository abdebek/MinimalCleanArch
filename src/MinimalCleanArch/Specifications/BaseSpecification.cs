using System.Linq.Expressions;

namespace MinimalCleanArch.Specifications
{
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
        
        /// <summary>
        /// Gets a value indicating whether the query should be tracked by the EF Core change tracker
        /// </summary>
        public bool AsNoTracking { get; private set; }
        
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
        /// Configures the specification to use no tracking queries
        /// </summary>
        protected void UseNoTracking()
        {
            AsNoTracking = true;
        }

        /// <summary>
        /// Configures the specification for count-only queries
        /// </summary>
        protected void ForCountOnly()
        {
            IsCountOnly = true;
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
            var parameter = Expression.Parameter(typeof(T));

            var leftVisitor = new ReplaceExpressionVisitor(expr1.Parameters[0], parameter);
            var left = leftVisitor.Visit(expr1.Body);

            var rightVisitor = new ReplaceExpressionVisitor(expr2.Parameters[0], parameter);
            var right = rightVisitor.Visit(expr2.Body);

            return Expression.Lambda<Func<T, bool>>(
                Expression.AndAlso(left!, right!), parameter);
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
}
