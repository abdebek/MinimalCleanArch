# Context: Toolkit core

**Boundary:** NuGet package `MinimalCleanArch` (`src/MinimalCleanArch/`).  
**Kind:** Shared kernel of primitives. Not a business bounded context.  
**Database:** none.  
**UI:** none.

## What it owns

Contracts every other MCA package and every consumer domain may use.

| Area | Types | Path |
|---|---|---|
| Entities | `IEntity<TKey>`, `BaseEntity<TKey>`, `IAuditableEntity`, `BaseAuditableEntity<TKey>`, `ISoftDelete`, `BaseSoftDeleteEntity<TKey>` | `Domain/Entities/` |
| Events | `IDomainEvent`, `DomainEvent`, `EntityDomainEvent<TKey>`, `IHasDomainEvents`, `EntityWithEvents`, `DomainEventCollection` | `Domain/Events/` |
| Errors | `DomainException` | `Domain/Exceptions/` |
| Result | `Result`, `Result<TValue>`, `Error`, `ErrorType` | `Common/Result.cs` |
| Persistence ports | `IRepository<TEntity,TKey>`, `IRepository<TEntity>`, `IUnitOfWork` | `Repositories/` |
| Queries | `ISpecification<T>`, `BaseSpecification<T>`, `AndSpecification<T>`, `OrSpecification<T>`, `NotSpecification<T>`, `InMemorySpecificationEvaluator` | `Specifications/` |
| Scope | `IExecutionContext`, `ExecutionContextOptions`, `NullExecutionContext` | `Execution/` |

## Aggregates

None. `BaseEntity` is a persistable record with Id equality. It does not model an aggregate root. There is no `IAggregateRoot`.

`IRepository<TEntity,TKey>` is generic. The toolkit does not enforce that only roots are persisted.

## Honesty

- Anemic base classes: audit and soft delete fields are public get/set.
- `IRepository` default interface methods (`AnyAsync`, `CountAsync(spec)`, `SingleOrDefaultAsync`) load data then count in memory. `Repository<,>` overrides them with SQL.
- `Result` lives in namespace `MinimalCleanArch.Domain.Common` even though the file is `Common/Result.cs`.

## Integration

Downstream packages depend on this one. Consumer Domain projects should depend on this package only among MCA packages.

See [persistence](persistence.md) for the EF implementation of the ports.
