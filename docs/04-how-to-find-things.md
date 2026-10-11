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
| Todo repository extras | Template only: `ITodoRepository` / `TodoRepository`. Unused by handlers: single-project `GetByPriorityAsync`; multi-project `GetIncompleteByPriorityAsync` and `GetDueBeforeAsync`. Sample uses generic `IRepository<Todo>`. |
| Todo use case (create/update/complete/delete/restore) | Template: `Application/Handlers/TodoCommandHandler.cs`. Sample: methods on `TodoEndpoints` (no handler; no restore). |
| Todo domain events | `Domain/Events/TodoEvents.cs`. Handlers: sample `Application/Handlers/TodoEventHandlers.cs`, template `Application/Handlers/TodoEventHandler.cs` (log only). |
| User register/login (sample) | `API/Endpoints/UserEndpoints.cs` and `MapIdentityApi<User>()` in `Program.cs`. Model: `Domain/Entities/User.cs`. |
| Auth register/login/password (template) | `Endpoints/AuthEndpoints.cs`, `Application/Commands/AuthCommands.cs`, matching `*Handler.cs`, `Application/Identity/ApplicationUser.cs`. |
| Refresh tokens | Template `--auth` only: `AllowRefreshTokenFlow` + `POST /connect/token` `grant_type=refresh_token` in `OpenIddictEndpoints`. Logout/revoke: `/connect/logout`, `/connect/revoke`. Lifetimes: `OpenIddict:TokenLifetimes`. Lifecycle: [05. Lifecycles](05-lifecycles.md). Sample has no refresh grant. |
| Multi-tenant row isolation | Flag `--multitenant` (with `--auth`). `ITenantEntity` + EF filter in `DbContextBase`; claim `tenant_id` from `CustomClaimsPrincipalFactory`. Default is EF filter, not Postgres RLS. |
| Organizations / invitations | `--auth --multitenant`: `OrganizationEndpoints` (`/api/organizations`), `OrganizationCommandHandler`, invite-by-code. Roles are `OrganizationRole` rows, not `Roles.Admin` constants. |
| OpenIddict / OAuth demo | Template `--auth`: `Endpoints/OpenIddictEndpoints.cs`, `OAuthEndpoints.cs`; `IdentityServiceExtensions` is single `Infrastructure/Configuration/` or multi `MCA.Api/Configuration/` (not in the sample). External login: `ExternalAuthEndpoints.cs` + `Authentication:{Google|Microsoft|GitHub}` secrets. |
| Email after register | Template `AuthEventHandler` when messaging is on; otherwise `RegisterUserHandler` sends inline. `IEmailService` in Application; transport `MinimalCleanArch.Email` (`AddEmail`). |
| Audit capture | `src/MinimalCleanArch.Audit/Interceptors/AuditSaveChangesInterceptor.cs`. Wire: `AddAuditLogging` + `UseAuditInterceptor`. Query: `IAuditLogService`. |
| Encryption | Mark `[Encrypted]` (`src/MinimalCleanArch.Security/Encryption/EncryptedAttribute.cs`). Register `AddEncryption` or `AddDataProtectionEncryptionForDevelopment`. Call `modelBuilder.UseEncryption` (sample does; template DbContext does not). |
| Blob signed URLs | Template `Endpoints/StorageEndpoints.cs`. Contract: `IBlobStorage` in `src/MinimalCleanArch.Storage/IBlobStorage.cs`. |
| HTTP error shape | `ErrorHandlingMiddleware`, `MinimalCleanArchProblemDetailsFactory`, `Result.MatchHttp`. |
| API versioning | Sample: `AddMinimalCleanArchApiVersioning()` in `Program.cs`. Template: same call when `--recommended`, `--versioning`, or `--all`. Implementation: `src/MinimalCleanArch.Extensions/Versioning/ApiVersioningExtensions.cs`. |
| Correlation / tenant / user stamps | `IExecutionContext`. HTTP: `HttpExecutionContext`. Messaging: `MessagingExecutionContext`. Options: `ExecutionContextOptions`. |
| Soft delete behavior | `Repository.DeleteAsync` and `DbContextBase` query filter. Template also has `Todo.Delete()`. |
| Soft-delete restore | Template `POST /api/todos/{id}/restore` (`RestoreTodoCommand`, `Todo.Restore()`). Optional; Admin role when `--auth`. Sample has no restore. |
| Background jobs | Package `src/MinimalCleanArch.Jobs`. Template `--jobs` / `--messaging`: `Application/Jobs/PurgeSoftDeletedTodos.cs`, `AddJobs` in Program.cs. Wolverine: `AddWolverineJobs`. |
| Realtime | Package `src/MinimalCleanArch.Realtime` (`IRealtimePublisher`). SignalR adapter: `AddMinimalCleanArchRealtime` / `MapMinimalCleanArchRealtime` in Extensions. Template `--realtime` / `--all`: hub `/hubs/realtime`; Todo writes publish channel `todos`. |
| Feature flags | Package `src/MinimalCleanArch.Features` (`IFeatureGate`). HTTP: `RequireFeature` in Extensions. Template `--features` / `--all`: `GET /api/todos/export` gated by `Features:Flags:todo-export`. |
| Add a new MCA package API | Package folder under `src/`, then the sample `Program.cs`, then both template `Program.cs` files, then `templates/README.md`. |
| Template flag behavior | `templates/README.md` then the `#if` symbols in `templates/mca/`. |
| Client app folders | `--frontend` → `apps/web` (OIDC PKCE in `src/lib/auth`); `--mobile` → `apps/mobile`. Not in `--all`. API-only generate must not create `apps/`. Run API: `dotnet run --project src/{Name}.Api`. |
| TS OIDC PKCE client | `--frontend`: `templates/mca/apps/web/src/lib/auth`. Against `--auth` API: public client `mca-spa-client`, redirect `http://localhost:4321/callback` or `:3000/callback`. |
| Aspire connection names | Sample AppHost uses `mca` and `redis`. Generated AppHost uses `appdb` and `redis`. |
| How packages/templates are validated in CI | `.github/workflows/nuget.yml` (build/test) then reusable `.github/workflows/validate-templates.yml`. Local: `scripts/validate-templates.sh`. |
| How packages/templates are published | Tag `v*` or dispatch `.github/workflows/publish-nuget.yml` (OIDC Trusted Publishing). `nuget.yml` can also push with `NUGET_API_KEY`. See [07. Bootstrap product — CI](07-bootstrap-product.md#ci-validate-and-publish). |

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
| `ITodoRepository.GetByPriorityAsync` | Single-project only (`templates/mca/single`). Implemented, no handler caller. |
| `ITodoRepository.GetIncompleteByPriorityAsync`, `GetDueBeforeAsync` | Multi-project only (`templates/mca/multi`). Implemented, no handler caller. |
| `TodoReminderEvent` | Sample type + handler. Nothing calls `ScheduleAsync`. |
| `docs/MinimalCleanArch.Docs/Program.cs` | Prints Hello World. |
| `bin/`, `obj/`, `temp/`, `artifacts/` | Build and scratch output. |

## Next

[05. Lifecycles](05-lifecycles.md)
