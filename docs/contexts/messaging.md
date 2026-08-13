# Context: Messaging

**Boundary:** NuGet `MinimalCleanArch.Messaging`.  
**Kind:** Infrastructure. Optional.  
**Database:** none (in memory) or Wolverine schema (SQL Server / Postgres).  
**UI:** none.

## What it owns

| Type | Path |
|---|---|
| `IDomainEventPublisher` | `src/MinimalCleanArch.Messaging/IDomainEventPublisher.cs` |
| `WolverineDomainEventPublisher` | `WolverineDomainEventPublisher.cs` |
| `DomainEventPublishingInterceptor` | `Middleware/DomainEventPublishingInterceptor.cs` |
| `MessagingOptions` | `Extensions/MessagingOptions.cs` |
| `AddMinimalCleanArchMessaging*` | `Extensions/MessagingExtensions.cs` |
| `UseDomainEventPublishing` | `Extensions/DbContextExtensions.cs` |
| `MessagingExecutionContext` | `Execution/MessagingExecutionContext.cs` |

## Aggregates

None. It transports `IDomainEvent` records defined by Todo and Identity.

## Commands / queries / hosts

When `--messaging` is on, Wolverine also **invokes** application commands (`IMessageBus.InvokeAsync<Result<...>>`). That is an in process mediator, not a second host.

There is no worker project and no commented out worker to enable. Handlers run inside the API process.

Durable tables are created by Wolverine, not by MCA migrations.

## Integration

| Other context | How |
|---|---|
| Toolkit core | `IDomainEvent`, `IHasDomainEvents` |
| Persistence | Must call `UseDomainEventPublishing` or nothing publishes |
| HTTP host | Replaces `IExecutionContext` |
| Todo / Identity | Event types and `Handle` methods in Application |

## Honesty

- Sample registers messaging and never attaches the interceptor. See [06. Events](../06-events-and-side-effects.md).
- `ServiceLocationPolicy` defaults to `AllowedButWarn` so constructor injected handlers work under Wolverine 6.
- SQLite is in memory messaging only.
- Todo event handlers are log stubs.
