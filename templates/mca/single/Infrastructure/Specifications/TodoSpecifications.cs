using MCA.Domain.Entities;
using MinimalCleanArch.Specifications;

namespace MCA.Infrastructure.Specifications;

/// <summary>
/// Specification for filtering todos by search, completion state, priority, and due date.
/// </summary>
public sealed class TodoFilterSpecification : BaseSpecification<Todo>
{
    public TodoFilterSpecification(
        string? searchTerm = null,
        bool? isCompleted = null,
        DateTime? dueBefore = null,
        DateTime? dueAfter = null,
        int? priority = null)
    {
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var lowered = searchTerm.Trim().ToLowerInvariant();
            AddCriteria(t =>
                t.Title.ToLower().Contains(lowered) ||
                (t.Description != null && t.Description.ToLower().Contains(lowered)));
        }

        if (isCompleted.HasValue)
        {
            AddCriteria(t => t.IsCompleted == isCompleted.Value);
        }

        if (dueBefore.HasValue)
        {
            var date = dueBefore.Value.Date;
            AddCriteria(t => t.DueDate.HasValue && t.DueDate.Value.Date <= date);
        }

        if (dueAfter.HasValue)
        {
            var date = dueAfter.Value.Date;
            AddCriteria(t => t.DueDate.HasValue && t.DueDate.Value.Date >= date);
        }

        if (priority.HasValue)
        {
            AddCriteria(t => t.Priority == priority.Value);
        }

        // Default ordering: priority desc, then earliest due date first
        ApplyOrderByDescending(t => t.Priority);
        ApplyThenBy(t => t.DueDate ?? DateTime.MaxValue);
        UseNoTracking();
    }
}

/// <summary>
/// Specification to fetch a todo by id.
/// </summary>
public sealed class TodoByIdSpecification : BaseSpecification<Todo>
{
    public TodoByIdSpecification(int id) : base(t => t.Id == id)
    {
        UseNoTracking();
    }
}

/// <summary>
/// Specification for applying pagination on top of an existing filter specification.
/// </summary>
public sealed class TodoPaginatedSpecification : BaseSpecification<Todo>
{
    public TodoPaginatedSpecification(int pageSize, int pageIndex, TodoFilterSpecification filter)
    {
        if (filter.Criteria is not null)
        {
            AddCriteria(filter.Criteria);
        }

        var skip = (pageIndex - 1) * pageSize;
        ApplyPaging(skip, pageSize);

        if (filter.OrderBy is not null)
        {
            ApplyOrderBy(filter.OrderBy);
        }
        else if (filter.OrderByDescending is not null)
        {
            ApplyOrderByDescending(filter.OrderByDescending);
        }

        foreach (var thenBy in filter.ThenBys)
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

        if (filter.AsNoTracking)
        {
            UseNoTracking();
        }
    }
}
