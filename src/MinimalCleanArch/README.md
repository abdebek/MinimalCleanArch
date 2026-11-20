# MinimalCleanArch (Core)

Core primitives for Clean Architecture: entities, repositories, specifications, result pattern, and common types.

## Version
- Current stable: 0.2.1-preview (net11.0).

## Why Use It
- define entities, repository contracts, specifications, and result types without taking a dependency on EF Core, HTTP, or messaging frameworks
- keep business rules in the domain layer while allowing infrastructure packages to plug in later
- share a stable contract layer across application, infrastructure, and API projects

## When to Use It
- use it in every MCA-based solution
- put domain entities, repository abstractions, specifications, and shared operation results here
- choose this package alone when you want clean domain modeling without committing yet to EF Core or Minimal API helpers

## Dependency Direction
- Depends on: no other MCA package
- Typically referenced by: domain, application, infrastructure, and API projects
- Safe direction: every other MCA package can depend on this package
- Avoid: putting EF Core, HTTP, logging, or transport-specific concerns in projects that only reference this package

## Contents
- Domain entities: `IEntity<TKey>`, `BaseEntity<TKey>`, `BaseAuditableEntity`, `BaseSoftDeleteEntity`, `IAuditableEntity`, `ISoftDelete`, `ITenantEntity`.
- Common types: C# 15 non-boxing `[Union]` structs `Result`/`Result<T>` (`Success` / `T` / `Error`), immutable `Error` (status code + `Metadata`), `TryGetValue`, `GetValueOrThrow`, `Match`/`Map`/`Bind`.
- Repositories: `IRepository<TEntity, TKey>`, `IUnitOfWork` (`IDisposable` + `IAsyncDisposable`).
- Specifications: `ISpecification<T>`, `ISpecification<T, TResult>` (SQL projections), `BaseSpecification<T>`, `BaseSpecification<T, TResult>`, composable `And/Or/Not`, `InMemorySpecificationEvaluator`, and query flags (`TrackingMode`, `AsNoTracking`, `AsSplitQuery`, `IgnoreQueryFilters`, `IsCountOnly`).

## Design Guidance
- Keep domain entities, repository contracts, result types, and specifications here.
- Do not put EF Core, HTTP, or transport concerns in projects that depend only on this package.
- Let application, infrastructure, and API layers build on this package instead of bypassing it with ad hoc shared abstractions.

## Usage
```bash
dotnet add package MinimalCleanArch --version 0.2.1-preview
```

Use `BaseAuditableEntity`/`BaseSoftDeleteEntity` for entities that need auditing and soft delete. Use `Result`/`Result<T>` with native C# 15 exhaustive pattern matching:

```csharp
Result<TodoResponse> result = await handler.Handle(query, ct);

return result switch
{
    TodoResponse todo => Results.Ok(todo),
    Error error => error.ToProblem(httpContext)
};
```

### Projection specification example
```csharp
public sealed class ActiveTodoSummarySpec : BaseSpecification<Todo, TodoSummary>
{
    public ActiveTodoSummarySpec() : base(t => !t.IsCompleted)
    {
        ApplyOrderByDescending(t => t.Priority);
        ApplySelector(t => new TodoSummary(t.Id, t.Title, t.Priority));
    }
}
```

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


