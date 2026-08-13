# 03. Ubiquitous language

Terms below mean what this codebase implements, not textbook DDD. Each row cites the type and a path.

## Toolkit primitives

| Term | Meaning here | Type | Path |
|---|---|---|---|
| Entity | Row with an `Id`. Equality is Id based. | `IEntity<TKey>`, `BaseEntity<TKey>` | `src/MinimalCleanArch/Domain/Entities/` |
| Auditable entity | Created/modified stamps. Public setters. Stamped in `SaveChanges`, not by domain methods. | `IAuditableEntity`, `BaseAuditableEntity<TKey>` | same |
| Soft delete | `IsDeleted` flag plus a global EF filter. | `ISoftDelete`, `BaseSoftDeleteEntity<TKey>` | same |
| Domain event | Marker with `EventId` and `OccurredAt`. | `IDomainEvent`, `DomainEvent`, `EntityDomainEvent<TKey>` | `src/MinimalCleanArch/Domain/Events/` |
| Has domain events | In memory list on an entity. Cleared after publish. | `IHasDomainEvents`, `EntityWithEvents`, `DomainEventCollection` | same |
| Domain exception | Exception wrapping `Error`. Mapped to ProblemDetails. | `DomainException` | `src/MinimalCleanArch/Domain/Exceptions/DomainException.cs` |
| Result | Success or `Error`. Not an exception. | `Result`, `Result<TValue>` | `src/MinimalCleanArch/Common/Result.cs` |
| Error | Code, message, `ErrorType`, HTTP status, metadata. | `Error`, `ErrorType` | same file |
| Repository | Generic persistence port. Not aggregate specific. | `IRepository<TEntity, TKey>` | `src/MinimalCleanArch/Repositories/IRepository.cs` |
| Unit of work | `SaveChanges` plus optional EF transactions. Isolation other than ReadCommitted is ignored. | `IUnitOfWork`, `UnitOfWork` | `src/MinimalCleanArch/Repositories/IUnitOfWork.cs`, `src/MinimalCleanArch.DataAccess/Repositories/UnitOfWork.cs` |
| Specification | Query object: criteria, includes, paging, tracking flags. | `ISpecification<T>`, `BaseSpecification<T>` | `src/MinimalCleanArch/Specifications/` |
| Execution context | User, tenant, correlation, IP, user agent for the current scope. | `IExecutionContext` | `src/MinimalCleanArch/Execution/IExecutionContext.cs` |

## Persistence and host

| Term | Meaning here | Type | Path |
|---|---|---|---|
| DbContext base | Soft delete filter + audit stamping. | `DbContextBase` | `src/MinimalCleanArch.DataAccess/DbContextBase.cs` |
| Identity DbContext base | Same plus ASP.NET Identity. | `IdentityDbContextBase<...>` | `src/MinimalCleanArch.DataAccess/IdentityDbContextBase.cs` |
| Specification evaluator | Turns a spec into `IQueryable`. | `SpecificationEvaluator<T>` | `src/MinimalCleanArch.DataAccess/Specifications/SpecificationEvaluator.cs` |
| MatchHttp | Maps `Result` to `IResult` ProblemDetails. | `ResultHttpExtensions.MatchHttp` | `src/MinimalCleanArch.Extensions/Extensions/ResultHttpExtensions.cs` |
| ValidateAsync / WithValidation | FluentValidation at the endpoint. | `ValidationHttpExtensions`, `ValidationFilter<T>` | `src/MinimalCleanArch.Extensions/Extensions/`, `Filters/` |

## Messaging and audit

| Term | Meaning here | Type | Path |
|---|---|---|---|
| Domain event publisher | Wolverine `IMessageBus.PublishAsync`. | `IDomainEventPublisher`, `WolverineDomainEventPublisher` | `src/MinimalCleanArch.Messaging/` |
| Publishing interceptor | After `SaveChanges`, publish then `ClearDomainEvents`. | `DomainEventPublishingInterceptor` | `src/MinimalCleanArch.Messaging/Middleware/DomainEventPublishingInterceptor.cs` |
| Local queue | In-process Wolverine queue. Default name `domain-events`. | `MessagingOptions.LocalQueueName` | `src/MinimalCleanArch.Messaging/Extensions/MessagingOptions.cs` |
| Outbox | Wolverine SQL Server or Postgres persistence + EF transactions. Not a custom MCA table. | `AddMinimalCleanArchMessagingWithSqlServer/Postgres` | `src/MinimalCleanArch.Messaging/Extensions/MessagingExtensions.cs` |
| Audit log | Change row written in the same SaveChanges. | `AuditLog`, `AuditOperation` | `src/MinimalCleanArch.Audit/Entities/AuditLog.cs` |
| Encrypted property | EF value converter, not a domain value object. | `[Encrypted]`, `IEncryptionService` | `src/MinimalCleanArch.Security/Encryption/` |

## Consumer domain (Todo)

| Term | Meaning here | Sample | Template |
|---|---|---|---|
| Todo | Single entity treated as the aggregate. Private setters. Invariants on title and (sample) priority. | `samples/.../Domain/Entities/Todo.cs` | `templates/mca/single/Domain/Entities/Todo.cs` |
| Create | Constructor sets fields and may raise `TodoCreatedEvent`. | `new Todo(...)` | same |
| Update | Replaces title, description, priority, due date. | `Todo.Update` | same |
| Complete | Sets `IsCompleted`. Sample always raises completed event. Template returns immediately if already complete. | `MarkAsCompleted` | same name |
| Incomplete | Clears `IsCompleted`. Sample raises no event. | `MarkAsNotCompleted` | `MarkAsIncomplete` |
| Delete | Sample: repository sets `IsDeleted`. Template: `Todo.Delete()` sets `IsDeleted` and `DeletedAt`. | `IRepository.DeleteAsync` | `Todo.Delete` |
| Command / query | Template records only. Sample has none. | n/a | `CreateTodoCommand`, `GetTodosQuery`, ... in `Application/Commands/TodoCommands.cs` |

`TodoCreatedEvent.EntityId` is assigned in the constructor from `Id`. For `int` identity that is `0` until EF generates the key. The event is not rewritten after insert.

## Consumer domain (Identity)

| Term | Meaning here | Sample | Template |
|---|---|---|---|
| User | ASP.NET Identity user. Anemic public setters in the sample. | `User : IdentityUser` | `ApplicationUser : IdentityUser<Guid>` in **Application**, not Domain |
| Register | Creates the user. Sample assigns role `User`. Template may raise `UserRegisteredEvent`. | `UserEndpoints.RegisterUser` | `RegisterUserCommand` / `RegisterUserHandler` |
| Login | Sample: `SignInManager` plus Identity API `/login`. Template: `AuthLoginCommand` + cookie/OpenIddict. | `UserEndpoints.LoginUser`, `MapIdentityApi` | `AuthLoginHandler` |
| Role | String constants. | seeded `Admin`, `User` | `MCA.Domain.Constants.Roles` (`Admin`, `User`, `Manager`) |

`ApplicationUser` lives in `MCA.Application.Identity` so Domain stays free of `Microsoft.AspNetCore.Identity`. Architecture tests encode that rule.

## Words this repo does not use as types

| Phrase you might expect | What exists instead |
|---|---|
| Aggregate root | No `IAggregateRoot`. `Todo` is a single entity. EF does not own a graph. |
| Value object | No VO base. `Error` is the closest structured type. |
| Domain service | No such type. Rules sit on `Todo` or in handlers. |
| Integration event | Same `IDomainEvent` records go to Wolverine. No separate integration event type. |
| Bounded context project | Packages and layers, not one project per context. |
| Outbox table owned by MCA | Wolverine schema `wolverine` (configurable). |

## Next

[04. How to find things](04-how-to-find-things.md)
