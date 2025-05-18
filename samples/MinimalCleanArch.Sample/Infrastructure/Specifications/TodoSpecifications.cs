using MinimalCleanArch.Sample.Domain.Entities;
using MinimalCleanArch.Specifications;

namespace MinimalCleanArch.Sample.Infrastructure.Specifications;

/// <summary>
/// Specification for filtering todos
/// </summary>
public class TodoFilterSpecification : BaseSpecification<Todo>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TodoFilterSpecification"/> class
    /// </summary>
    /// <param name="searchTerm">The search term</param>
    /// <param name="isCompleted">Filter by completion status</param>
    /// <param name="dueBefore">Filter by due date (before)</param>
    /// <param name="dueAfter">Filter by due date (after)</param>
    /// <param name="priority">Filter by priority</param>
    public TodoFilterSpecification(
        string? searchTerm = null,
        bool? isCompleted = null,
        DateTime? dueBefore = null,
        DateTime? dueAfter = null,
        int? priority = null)
    {
        // Apply filter for search term
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            searchTerm = searchTerm.ToLower();
            AddCriteria(t => t.Title.ToLower().Contains(searchTerm) ||
                           t.Description.ToLower().Contains(searchTerm));
        }

        // Apply filter for completion status
        if (isCompleted.HasValue)
        {
            AddCriteria(t => t.IsCompleted == isCompleted.Value);
        }

        // Apply filter for due date
        if (dueBefore.HasValue)
        {
            AddCriteria(t => t.DueDate.HasValue && t.DueDate.Value <= dueBefore.Value);
        }

        if (dueAfter.HasValue)
        {
            AddCriteria(t => t.DueDate.HasValue && t.DueDate.Value >= dueAfter.Value);
        }

        // Apply filter for priority
        if (priority.HasValue)
        {
            AddCriteria(t => t.Priority == priority.Value);
        }

        // Apply default ordering
        ApplyOrderBy(t => t.Priority);
        ApplyThenByDescending(t => t.DueDate ?? DateTime.MaxValue);
    }
}

/// <summary>
/// Specification for getting a todo by ID
/// </summary>
public class TodoByIdSpecification : BaseSpecification<Todo>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TodoByIdSpecification"/> class
    /// </summary>
    /// <param name="id">The todo ID</param>
    public TodoByIdSpecification(int id)
        : base(t => t.Id == id)
    {
    }
}

/// <summary>
/// Specification for getting todos with paging
/// </summary>
public class TodoPaginatedSpecification : BaseSpecification<Todo>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TodoPaginatedSpecification"/> class
    /// </summary>
    /// <param name="pageSize">The page size</param>
    /// <param name="pageIndex">The page index</param>
    /// <param name="filter">The filter specification</param>
    public TodoPaginatedSpecification(int pageSize, int pageIndex, TodoFilterSpecification filter)
    {
        if (filter.Criteria != null)
        {
            AddCriteria(filter.Criteria);
        }

        ApplyPaging((pageIndex - 1) * pageSize, pageSize);
        
        // Apply ordering from filter
        if (filter.OrderBy != null)
        {
            ApplyOrderBy(filter.OrderBy);
        }

        if (filter.OrderByDescending != null)
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
    }
}
