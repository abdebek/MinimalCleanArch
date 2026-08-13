# Context: Persistence

**Boundary:** NuGet `MinimalCleanArch.DataAccess` plus each app's DbContext.  
**Kind:** Infrastructure.  
**Database:** the consumer's single relational store.  
**UI:** none.

## What it owns

| Type | Path | Role |
|---|---|---|
| `DbContextBase` | `src/MinimalCleanArch.DataAccess/DbContextBase.cs` | Soft delete filter, `ApplyAuditInfo` |
| `IdentityDbContextBase<...>` | `IdentityDbContextBase.cs` | Same + Identity index filters |
| `Repository<TEntity,TKey>` | `Repositories/Repository.cs` | Generic EF repository |
| `UnitOfWork` | `Repositories/UnitOfWork.cs` | Save + Begin/Commit/Rollback |
| `SpecificationEvaluator<T>` | `Specifications/SpecificationEvaluator.cs` | Spec → `IQueryable` |
| `AddMinimalCleanArch<TContext>` | `Extensions/ServiceCollectionExtensions.cs` | DI |

Consumer contexts:

| App | Type | Path |
|---|---|---|
| Sample | `ApplicationDbContext` | `samples/MinimalCleanArch.Sample/Infrastructure/Data/ApplicationDbContext.cs` |
| Template | `AppDbContext` | `templates/mca/single/Infrastructure/Data/AppDbContext.cs` (multi: `MCA.Infrastructure/Data/`) |
| Template design-time | `AppDbContextFactory` | same folder |
| Template startup schema | `DatabaseInitializer` | same folder |

## Aggregates

Persistence does not own aggregates. One `DbContext` maps Todo, Identity, `AuditLog`, and OpenIddict together. EF does not prevent a handler from loading and mutating any set of entities in one `SaveChanges`.

## Key behaviors

- `Repository.DeleteAsync` sets `IsDeleted` on `ISoftDelete`, otherwise `DbSet.Remove`.
- `GetByIdAsync` uses `FirstOrDefaultAsync(e => e.Id.Equals(id))`, so global filters apply (deleted rows are hidden).
- `UnitOfWork.BeginTransactionAsync` always uses the provider default isolation. Non-`ReadCommitted` values are logged to Debug and ignored.
- Sample `GetCurrentUserId` falls back to `"system"`.
- Template `AppDbContext` sets `UseQueryTrackingBehavior(NoTracking)` on the host options. Writes still attach via `UpdateAsync` (`EntityState.Modified`).

## Hosts

No dedicated host. The API process owns the context. Aspire injects `mca` (sample) or `appdb` (generated).

## Integration

| Talks to | How |
|---|---|
| Toolkit core | Implements `IRepository`, `IUnitOfWork` |
| Audit | `UseAuditInterceptor` on the same options |
| Messaging | `UseDomainEventPublishing` on the same options (template only) |
| Security | `modelBuilder.UseEncryption` (sample only today) |

Startup seed in the sample: `AddDatabaseSeeding` → `DatabaseMigrationSeeder`, `RoleSeeder`, `UserSeeder`. Template: `DatabaseInitializer.InitializeAsync` from `Database:*` config.
