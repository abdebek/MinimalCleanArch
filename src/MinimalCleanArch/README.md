# MinimalCleanArch (Core)

Core primitives for Clean Architecture: entities, repositories, specifications, result pattern, and common types.

## Version
- 0.1.17 (stable, net9.0, net10.0). Base dependency for all other MinimalCleanArch packages.

## Contents
- Domain entities: `IEntity<TKey>`, `BaseEntity<TKey>`, `BaseAuditableEntity`, `BaseSoftDeleteEntity`, `IAuditableEntity`, `ISoftDelete`.
- Common types: `Result`/`Result<T>`, `Error` (status code + metadata support, with `Match`/`Bind` helpers).
- Repositories: `IRepository<TEntity, TKey>`, `IUnitOfWork`.
- Specifications: `ISpecification<T>`, `BaseSpecification<T>`, composable `And/Or/Not`, `InMemorySpecificationEvaluator`, and query flags (`AsNoTracking`, `AsSplitQuery`, `IgnoreQueryFilters`, `IsCountOnly`).

## When to use this package
- Use it in every MinimalCleanArch-based solution.
- Keep domain entities, repository contracts, result types, and specifications here.
- Do not put EF Core or HTTP concerns in projects that depend only on this package.

## Usage
```bash
dotnet add package MinimalCleanArch --version 0.1.17
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

Guidance:
- keep specifications focused on business filters and query shape
- compose specifications at the application boundary instead of duplicating predicates
- keep repository interfaces in the domain layer and implementations in infrastructure
- use `InMemorySpecificationEvaluator` only for tests or in-memory execution paths

When consuming locally built nupkgs, add a `nuget.config` pointing to your local feed (e.g., `artifacts/nuget`) before restoring.

