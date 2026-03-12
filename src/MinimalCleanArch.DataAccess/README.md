# MinimalCleanArch.DataAccess

Entity Framework Core implementation for MinimalCleanArch (repositories, unit of work, specifications, DbContext helpers).

## Version
- 0.1.17 (stable, net9.0, net10.0). Works with `MinimalCleanArch` 0.1.17 and companion packages.

## What's included
- `DbContextBase` and `IdentityDbContextBase` with auditing/soft-delete support.
- `Repository<TEntity,TKey>` and `UnitOfWork` implementations.
- `SpecificationEvaluator` to translate specifications (including composed `And/Or/Not`) to EF Core queries and honor `IsCountOnly`, `AsSplitQuery`, and `IgnoreQueryFilters`.
- DI extensions to register repositories/unit of work.
- Common repository query methods such as `AnyAsync`, `SingleOrDefaultAsync`, and `CountAsync(ISpecification<T>)`.
- optional execution-context-aware base constructors for user and tenant-aware stamping

## Usage
```csharp
builder.Services.AddDbContext<AppDbContext>(opt => opt.UseSqlite("Data Source=app.db"));
builder.Services.AddScoped<DbContext>(sp => sp.GetRequiredService<AppDbContext>());
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped(typeof(IRepository<,>), typeof(Repository<,>));
```

If you need a custom unit of work and access to `IServiceProvider` while configuring the DbContext:

```csharp
builder.Services.AddMinimalCleanArch<AppDbContext, AppDbContext>((sp, options) =>
{
    options.UseSqlite("Data Source=app.db");
});
```

Recommended DbContext base usage:

```csharp
public sealed class AppDbContext : DbContextBase
{
    public AppDbContext(
        DbContextOptions<AppDbContext> options,
        IExecutionContext? executionContext = null)
        : base(options, executionContext)
    {
    }
}
```

Use the constructor overload that accepts `IExecutionContext` when you want audit stamping to flow from the current HTTP request or message-handler scope without overriding `GetCurrentUserId()`.

### Recommended specification usage
```csharp
public sealed class IncompleteHighPrioritySpec : BaseSpecification<Todo>
{
    public IncompleteHighPrioritySpec()
    {
        AddCriteria(t => !t.IsCompleted && t.Priority >= 3);
        ApplyOrderByDescending(t => t.Priority);
        UseNoTracking();
    }
}

// Compose and run
var dueToday = new DueTodaySpec();
var spec = new IncompleteHighPrioritySpec().And(dueToday);

var todos = await repository.GetAsync(spec, cancellationToken);
var hasAny = await repository.AnyAsync(spec, cancellationToken);
var total = await repository.CountAsync(spec, cancellationToken);

public sealed class DueTodaySpec : BaseSpecification<Todo>
{
    public DueTodaySpec() : base(t => t.DueDate != null && t.DueDate.Value.Date == DateTime.UtcNow.Date)
    {
    }
}
```

Use specifications through `IRepository<TEntity, TKey>` in application code. Treat `SpecificationEvaluator` as infrastructure-level plumbing for repository implementations and advanced EF integration points.

When using a locally built package, add a `nuget.config` pointing to your local feed (e.g., `artifacts/nuget`) before restoring.

