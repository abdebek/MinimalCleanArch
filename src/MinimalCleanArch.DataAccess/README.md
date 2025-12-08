# MinimalCleanArch.DataAccess

Entity Framework Core implementation for MinimalCleanArch (repositories, unit of work, specifications, DbContext helpers).

## Version
- 0.1.6 (net9.0). Works with `MinimalCleanArch` 0.1.6 and companion packages.

## What's included
- `DbContextBase` and `IdentityDbContextBase` with auditing/soft-delete support.
- `Repository<TEntity,TKey>` and `UnitOfWork` implementations.
- `SpecificationEvaluator` to translate specifications to EF queries.
- DI extensions to register repositories/unit of work.

## Usage
```csharp
builder.Services.AddDbContext<AppDbContext>(opt => opt.UseSqlite("Data Source=app.db"));
builder.Services.AddScoped<DbContext>(sp => sp.GetRequiredService<AppDbContext>());
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped(typeof(IRepository<,>), typeof(Repository<,>));
```

When using a locally built package, add a `nuget.config` pointing to your local feed (e.g., `artifacts/nuget`) before restoring.
