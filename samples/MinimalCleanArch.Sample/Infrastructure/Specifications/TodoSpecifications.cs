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
            var lowerSearchTerm = searchTerm.ToLower().Trim();
            AddCriteria(t => t.Title.ToLower().Contains(lowerSearchTerm) ||
                           t.Description.ToLower().Contains(lowerSearchTerm));
        }

        // Apply filter for completion status
        if (isCompleted.HasValue)
        {
            AddCriteria(t => t.IsCompleted == isCompleted.Value);
        }

        // Apply filter for due date (before)
        if (dueBefore.HasValue)
        {
            AddCriteria(t => t.DueDate.HasValue && t.DueDate.Value.Date <= dueBefore.Value.Date);
        }

        // Apply filter for due date (after)
        if (dueAfter.HasValue)
        {
            AddCriteria(t => t.DueDate.HasValue && t.DueDate.Value.Date >= dueAfter.Value.Date);
        }

        // Apply filter for priority
        if (priority.HasValue)
        {
            AddCriteria(t => t.Priority == priority.Value);
        }

        // Apply default ordering - priority ascending, then by due date descending (nulls last)
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
        // Validate pagination parameters
        if (pageSize <= 0)
            throw new ArgumentException("Page size must be greater than 0", nameof(pageSize));
        if (pageIndex <= 0)
            throw new ArgumentException("Page index must be greater than 0", nameof(pageIndex));

        // Copy criteria from filter specification
        if (filter.Criteria != null)
        {
            AddCriteria(filter.Criteria);
        }

        // Apply pagination
        var skip = (pageIndex - 1) * pageSize;
        ApplyPaging(skip, pageSize);
        
        // Apply ordering from filter specification
        if (filter.OrderBy != null)
        {
            ApplyOrderBy(filter.OrderBy);
        }

        if (filter.OrderByDescending != null)
        {
            ApplyOrderByDescending(filter.OrderByDescending);
        }

        // Apply additional ordering (ThenBy)
        foreach (var (keySelector, descending) in filter.ThenBys)
        {
            if (descending)
            {
                ApplyThenByDescending(keySelector);
            }
            else
            {
                ApplyThenBy(keySelector);
            }
        }
    }
}