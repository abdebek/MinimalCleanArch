# MinimalCleanArch (Core)

Core primitives for Clean Architecture: entities, repositories, specifications, result pattern, and common types.

## Version
-0.1.8-preview (net9.0). Base dependency for all other MinimalCleanArch packages.

## Contents
- Domain entities: `IEntity<TKey>`, `BaseEntity<TKey>`, `BaseAuditableEntity`, `BaseSoftDeleteEntity`, `IAuditableEntity`, `ISoftDelete`.
- Common types: `Result`/`Result<T>`, `Error`.
- Repositories: `IRepository<TEntity, TKey>`, `IUnitOfWork`.
- Specifications: `ISpecification<T>`, `BaseSpecification<T>`, and composable `And/Or/Not` helpers for richer queries.

## Usage
```bash
dotnet add package MinimalCleanArch --version0.1.8-preview
```

Use `BaseAuditableEntity`/`BaseSoftDeleteEntity` for entities that need auditing and soft delete. Use `Result`/`Result<T>` for typed operation results.

### Specification example
```csharp
// Build a filter
public sealed class TodoFilterSpec : BaseSpecification<Todo>
{
    public TodoFilterSpec(string? search, bool? isCompleted, int? priority)
    {
        if (!string.IsNullOrWhiteSpace(search))
        {
            AddCriteria(t => t.Title.Contains(search) || t.Description.Contains(search));
        }

        if (isCompleted is not null)
        {
            AddCriteria(t => t.IsCompleted == isCompleted);
        }

        if (priority is not null)
        {
            AddCriteria(t => t.Priority == priority);
        }

        ApplyOrderByDescending(t => t.Priority);
    }
}

// Compose at call-site
var highPriority = new TodoFilterSpec(null, false, 5);
var dueToday = new DueTodaySpec();
var todayHighPriority = highPriority.And(dueToday);

public sealed class DueTodaySpec : BaseSpecification<Todo>
{
    public DueTodaySpec() : base(t => t.DueDate == DateOnly.FromDateTime(DateTime.UtcNow))
    {
    }
}
```

When consuming locally built nupkgs, add a `nuget.config` pointing to your local feed (e.g., `artifacts/nuget`) before restoring.
