# 04. How to find things

Lead with the recipe. Then use the naming table.

Decide which tree you are in first:

| You are changing | Tree |
|---|---|
| The published packages | `src/` |
| The in-repo demo | `samples/MinimalCleanArch.Sample/` |
| What `dotnet new mca` generates | `templates/mca/single/` and `templates/mca/multi/` (keep them in sync) |
| Local orchestration of the sample | `samples/MinimalCleanArch.Aspire/` |
| Package versions | `eng/Directory.Packages.props` and matching pins in `templates/mca/**/*.csproj` |

Template files use `#if (UseMessaging)` style symbols. A missing feature is often a preprocessor block, not a missing file.

## Recipes

| I need to change | Start here |
|---|---|
| Todo HTTP contract (sample) | `samples/MinimalCleanArch.Sample/API/Endpoints/TodoEndpoints.cs` then `API/Models/TodoModels.cs`, `API/Validators/TodoValidators.cs` |
| Todo HTTP contract (template) | `templates/mca/single/Endpoints/TodoEndpoints.cs` (or `multi/MCA.Api/Endpoints/`) then `Application/Commands/TodoCommands.cs`, `Application/DTOs/TodoDtos.cs`, `Application/Validation/TodoValidators.cs` |
| Todo business rules | Sample or template `Domain/Entities/Todo.cs`. Sample throws `DomainException`. Template uses `ArgumentException` for title. |
| Todo query filters | Sample: `Infrastructure/Specifications/TodoSpecifications.cs`. Single project template: `Application/Specifications/TodoSpecifications.cs`. Multi project template has no specification types; `GetAllTodosQuery` loads everything. |
| Todo persistence mapping | Sample: `TodoConfiguration` in `Infrastructure/Data/ApplicationDbContext.cs`. Template: `AppDbContext.OnModelCreating`. |
| Todo repository extras | Template only: `ITodoRepository` / `TodoRepository`. `GetByPriorityAsync` is unused by handlers. Sample uses generic `IRepository<Todo>`. |
| Todo use case (create/update/complete/delete) | Template: `Application/Handlers/TodoCommandHandler.cs`. Sample: methods on `TodoEndpoints` (no handler). |
| Todo domain events | `Domain/Events/TodoEvents.cs`. Handlers: sample `Application/Handlers/TodoEventHandlers.cs`, template `Application/Handlers/TodoEventHandler.cs` (log only). |
| User register/login (sample) | `API/Endpoints/UserEndpoints.cs` and `MapIdentityApi<User>()` in `Program.cs`. Model: `Domain/Entities/User.cs`. |
| Auth register/login/password (template) | `Endpoints/AuthEndpoints.cs`, `Application/Commands/AuthCommands.cs`, matching `*Handler.cs`, `Application/Identity/ApplicationUser.cs`. |
| OpenIddict / OAuth demo | `Endpoints/OpenIddictEndpoints.cs`, `OAuthEndpoints.cs`, `Infrastructure/Configuration/IdentityServiceExtensions.cs`. Development only extras: `ExternalAuthEndpoints.cs`. |
| Email after register | Template `AuthEventHandler` when messaging is on; otherwise `RegisterUserHandler` sends inline. Implementation: `Infrastructure/Services/EmailService.cs`. |
| Audit capture | `src/MinimalCleanArch.Audit/Interceptors/AuditSaveChangesInterceptor.cs`. Wire: `AddAuditLogging` + `UseAuditInterceptor`. Query: `IAuditLogService`. |
| Encryption | Mark `[Encrypted]` (`src/MinimalCleanArch.Security/Encryption/EncryptedAttribute.cs`). Register `AddEncryption` or `AddDataProtectionEncryptionForDevelopment`. Call `modelBuilder.UseEncryption` (sample does; template DbContext does not). |
| Blob signed URLs | Template `Endpoints/StorageEndpoints.cs`. Contract: `IBlobStorage` in `src/MinimalCleanArch.Storage/IBlobStorage.cs`. |
| HTTP error shape | `ErrorHandlingMiddleware`, `MinimalCleanArchProblemDetailsFactory`, `Result.MatchHttp`. |
| Correlation / tenant / user stamps | `IExecutionContext`. HTTP: `HttpExecutionContext`. Messaging: `MessagingExecutionContext`. Options: `ExecutionContextOptions`. |
| Soft delete behavior | `Repository.DeleteAsync` and `DbContextBase` query filter. Template also has `Todo.Delete()`. |
| Add a new MCA package API | Package folder under `src/`, then the sample `Program.cs`, then both template `Program.cs` files, then `templates/README.md`. |
| Template flag behavior | `templates/README.md` then the `#if` symbols in `templates/mca/`. |
| Aspire connection names | Sample AppHost uses `mca` and `redis`. Generated AppHost uses `appdb` and `redis`. |

## Naming conventions

| Kind | Convention | Examples |
|---|---|---|
| Entity | Noun, `BaseSoftDeleteEntity` or Identity user | `Todo`, `User`, `ApplicationUser` |
| Domain event | `{Entity}{PastTense}Event` record | `TodoCreatedEvent` |
| Command | `{Verb}{Entity}Command` record | `CreateTodoCommand`, `RegisterUserCommand` |
| Query | `{Get...}Query` record | `GetTodosQuery`, `GetTodoByIdQuery` |
| Handler | `{Feature}CommandHandler` or `{Feature}Handler` | `TodoCommandHandler`, `RegisterUserHandler` |
| Event handler | `{Feature}EventHandler(s)` | `TodoEventHandler`, `AuthEventHandler` |
| Specification | `{Entity}{Purpose}Specification` | `TodoFilterSpecification` |
| Validator | `{CommandOrRequest}Validator` | `CreateTodoCommandValidator` |
| Endpoint class | `{Feature}Endpoints.Map{Feature}Endpoints` | `TodoEndpoints` |
| Repository port | `I{Entity}Repository` (template only) | `ITodoRepository` |
| DI entry | `AddMinimalCleanArch*` | `AddMinimalCleanArchApi`, `AddMinimalCleanArchMessaging` |
| Middleware entry | `UseMinimalCleanArch*` | `UseMinimalCleanArchApiDefaults` |

Wolverine finds handlers by method name `Handle` (and cancellation token). No `IRequestHandler` interface.

## Where not to look

| Path | Why |
|---|---|
| `ValidationBehavior<TRequest,TResponse>` | Exists, unused by hosts. |
| `GetAllTodosQuery` in `--single-project` | Declared in `templates/mca/single/Application/Commands/TodoCommands.cs`. No `Handle` method and no endpoint. The multi project template **does** use it for `GET /api/todos`. |
| `ITodoRepository.GetByPriorityAsync` | Implemented, no caller in handlers. |
| `TodoReminderEvent` | Sample type + handler. Nothing calls `ScheduleAsync`. |
| `docs/MinimalCleanArch.Docs/Program.cs` | Prints Hello World. |
| `bin/`, `obj/`, `temp/`, `artifacts/` | Build and scratch output. |

## Next

[05. Lifecycles](05-lifecycles.md)
