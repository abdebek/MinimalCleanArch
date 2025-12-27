# MinimalCleanArch.DataAccess

Entity Framework Core implementation for MinimalCleanArch (repositories, unit of work, specifications, DbContext helpers).

## Version
-0.1.9-preview (net9.0, net10.0). Works with `MinimalCleanArch`0.1.9-preview and companion packages.

## What's included
- `DbContextBase` and `IdentityDbContextBase` with auditing/soft-delete support.
- `Repository<TEntity,TKey>` and `UnitOfWork` implementations.
- `SpecificationEvaluator` to translate specifications (including composed `And/Or/Not`) to EF Core queries and honor `IsCountOnly`.
- DI extensions to register repositories/unit of work.

## Usage
```csharp
builder.Services.AddDbContext<AppDbContext>(opt => opt.UseSqlite("Data Source=app.db"));
builder.Services.AddScoped<DbContext>(sp => sp.GetRequiredService<AppDbContext>());
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped(typeof(IRepository<,>), typeof(Repository<,>));
```

### Using specifications with EF Core
```csharp
// Define a spec
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

var todos = await SpecificationEvaluator<Todo>
    .GetQuery(dbContext.Set<Todo>(), spec)
    .ToListAsync(cancellationToken);

public sealed class DueTodaySpec : BaseSpecification<Todo>
{
    public DueTodaySpec() : base(t => t.DueDate != null && t.DueDate.Value.Date == DateTime.UtcNow.Date)
    {
    }
}
```

When using a locally built package, add a `nuget.config` pointing to your local feed (e.g., `artifacts/nuget`) before restoring.
