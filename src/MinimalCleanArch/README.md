# MinimalCleanArch (Core)

Core primitives for Clean Architecture: entities, repositories, specifications, result pattern, and common types.

## Version
- 0.1.6 (net9.0). Base dependency for all other MinimalCleanArch packages.

## Contents
- Domain entities: `IEntity<TKey>`, `BaseEntity<TKey>`, `BaseAuditableEntity`, `BaseSoftDeleteEntity`, `IAuditableEntity`, `ISoftDelete`.
- Common types: `Result`/`Result<T>`, `Error`.
- Repositories: `IRepository<TEntity, TKey>`, `IUnitOfWork`.
- Specifications: `ISpecification<T>`, base helpers.

## Usage
```bash
dotnet add package MinimalCleanArch --version 0.1.6
```

Use `BaseAuditableEntity`/`BaseSoftDeleteEntity` for entities that need auditing and soft delete. Use `Result`/`Result<T>` for typed operation results.

When consuming locally built nupkgs, add a `nuget.config` pointing to your local feed (e.g., `artifacts/nuget`) before restoring.
