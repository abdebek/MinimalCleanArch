using System.Linq;
using System.Linq.Expressions;

namespace MinimalCleanArch.Specifications;

/// <summary>
/// Composite specification that combines two specifications with a logical AND.
/// </summary>
/// <typeparam name="T">Entity type</typeparam>
public sealed class AndSpecification<T> : BaseSpecification<T>
{
    public AndSpecification(ISpecification<T> left, ISpecification<T> right)
    {
        var combinedCriteria = CombineCriteria(left.Criteria, right.Criteria);
        if (combinedCriteria is not null)
        {
            SetCriteria(combinedCriteria);
        }

        CopyIncludes(left);
        CopyIncludes(right);

        CopyOrdering(left, right);
        CopyPaging(left, right);
        CopyFlags(left, right);
    }

    private static Expression<Func<T, bool>>? CombineCriteria(
        Expression<Func<T, bool>>? left,
        Expression<Func<T, bool>>? right)
    {
        if (left is null && right is null) return null;
        if (left is null) return right;
        if (right is null) return left;

        return left.AndAlso(right);
    }

    private void CopyIncludes(ISpecification<T> source)
    {
        foreach (var include in source.Includes)
        {
            AddInclude(include);
        }

        foreach (var includeString in source.IncludeStrings)
        {
            AddInclude(includeString);
        }
    }

    private void CopyOrdering(ISpecification<T> left, ISpecification<T> right)
    {
        var orderBy = left.OrderBy ?? right.OrderBy;
        var orderByDescending = left.OrderByDescending ?? right.OrderByDescending;

        if (orderBy is not null)
        {
            ApplyOrderBy(orderBy);
        }
        else if (orderByDescending is not null)
        {
            ApplyOrderByDescending(orderByDescending);
        }

        foreach (var thenBy in left.ThenBys.Concat(right.ThenBys))
        {
            if (thenBy.Descending)
            {
                ApplyThenByDescending(thenBy.KeySelector);
            }
            else
            {
                ApplyThenBy(thenBy.KeySelector);
            }
        }
    }

    private void CopyPaging(ISpecification<T> left, ISpecification<T> right)
    {
        var skip = left.Skip ?? right.Skip;
        var take = left.Take ?? right.Take;

        if (skip.HasValue || take.HasValue)
        {
            ApplyPaging(skip ?? 0, take ?? int.MaxValue);
        }
    }

    private void CopyFlags(ISpecification<T> left, ISpecification<T> right)
    {
        if (left.IgnoreSoftDelete || right.IgnoreSoftDelete)
        {
            IgnoreSoftDeleteFilter();
        }

        if (left.AsNoTracking || right.AsNoTracking)
        {
            UseNoTracking();
        }

        if (left.IsCountOnly || right.IsCountOnly)
        {
            ForCountOnly();
        }
    }
}

/// <summary>
/// Composite specification that combines two specifications with a logical OR.
/// </summary>
/// <typeparam name="T">Entity type</typeparam>
public sealed class OrSpecification<T> : BaseSpecification<T>
{
    public OrSpecification(ISpecification<T> left, ISpecification<T> right)
    {
        var combinedCriteria = CombineCriteria(left.Criteria, right.Criteria);
        if (combinedCriteria is not null)
        {
            SetCriteria(combinedCriteria);
        }

        CopyIncludes(left);
        CopyIncludes(right);
        CopyOrdering(left, right);
        CopyPaging(left, right);
        CopyFlags(left, right);
    }

    private static Expression<Func<T, bool>>? CombineCriteria(
        Expression<Func<T, bool>>? left,
        Expression<Func<T, bool>>? right)
    {
        if (left is null && right is null) return null;
        if (left is null) return right;
        if (right is null) return left;

        return left.OrElse(right);
    }

    private void CopyIncludes(ISpecification<T> source)
    {
        foreach (var include in source.Includes)
        {
            AddInclude(include);
        }

        foreach (var includeString in source.IncludeStrings)
        {
            AddInclude(includeString);
        }
    }

    private void CopyOrdering(ISpecification<T> left, ISpecification<T> right)
    {
        var orderBy = left.OrderBy ?? right.OrderBy;
        var orderByDescending = left.OrderByDescending ?? right.OrderByDescending;

        if (orderBy is not null)
        {
            ApplyOrderBy(orderBy);
        }
        else if (orderByDescending is not null)
        {
            ApplyOrderByDescending(orderByDescending);
        }

        foreach (var thenBy in left.ThenBys.Concat(right.ThenBys))
        {
            if (thenBy.Descending)
            {
                ApplyThenByDescending(thenBy.KeySelector);
            }
            else
            {
                ApplyThenBy(thenBy.KeySelector);
            }
        }
    }

    private void CopyPaging(ISpecification<T> left, ISpecification<T> right)
    {
        var skip = left.Skip ?? right.Skip;
        var take = left.Take ?? right.Take;

        if (skip.HasValue || take.HasValue)
        {
            ApplyPaging(skip ?? 0, take ?? int.MaxValue);
        }
    }

    private void CopyFlags(ISpecification<T> left, ISpecification<T> right)
    {
        if (left.IgnoreSoftDelete || right.IgnoreSoftDelete)
        {
            IgnoreSoftDeleteFilter();
        }

        if (left.AsNoTracking || right.AsNoTracking)
        {
            UseNoTracking();
        }

        if (left.IsCountOnly || right.IsCountOnly)
        {
            ForCountOnly();
        }
    }
}

/// <summary>
/// Composite specification that negates another specification.
/// </summary>
/// <typeparam name="T">Entity type</typeparam>
public sealed class NotSpecification<T> : BaseSpecification<T>
{
    public NotSpecification(ISpecification<T> specification)
    {
        if (specification.Criteria is not null)
        {
            SetCriteria(specification.Criteria.Not());
        }

        CopyIncludes(specification);
        CopyOrdering(specification);
        CopyPaging(specification);
        CopyFlags(specification);
    }

    private void CopyIncludes(ISpecification<T> source)
    {
        foreach (var include in source.Includes)
        {
            AddInclude(include);
        }

        foreach (var includeString in source.IncludeStrings)
        {
            AddInclude(includeString);
        }
    }

    private void CopyOrdering(ISpecification<T> source)
    {
        if (source.OrderBy is not null)
        {
            ApplyOrderBy(source.OrderBy);
        }
        else if (source.OrderByDescending is not null)
        {
            ApplyOrderByDescending(source.OrderByDescending);
        }

        foreach (var thenBy in source.ThenBys)
        {
            if (thenBy.Descending)
            {
                ApplyThenByDescending(thenBy.KeySelector);
            }
            else
            {
                ApplyThenBy(thenBy.KeySelector);
            }
        }
    }

    private void CopyPaging(ISpecification<T> source)
    {
        if (source.Skip.HasValue || source.Take.HasValue)
        {
            ApplyPaging(source.Skip ?? 0, source.Take ?? int.MaxValue);
        }
    }

    private void CopyFlags(ISpecification<T> source)
    {
        if (source.IgnoreSoftDelete)
        {
            IgnoreSoftDeleteFilter();
        }

        if (source.AsNoTracking)
        {
            UseNoTracking();
        }

        if (source.IsCountOnly)
        {
            ForCountOnly();
        }
    }
}
