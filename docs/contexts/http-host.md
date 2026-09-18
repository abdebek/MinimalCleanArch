# Context: HTTP host

**Boundary:** `MinimalCleanArch.Extensions` + `MinimalCleanArch.Validation`, plus each app's `Program.cs` and endpoint classes.  
**Kind:** Presentation / composition.  
**Database:** none of its own.  
**UI:** Scalar/OpenAPI in Development. No first party web UI.

## What it owns

| Area | Types | Path |
|---|---|---|
| Bootstrap | `AddMinimalCleanArchApi`, `AddMinimalCleanArchExtensions` | `src/MinimalCleanArch.Extensions/Extensions/ServiceCollectionExtensions.cs` |
| Pipeline | `UseMinimalCleanArchApiDefaults` | `ApplicationBuilderExtensions.cs` |
| Results | `MatchHttp`, `ToProblem` | `ResultHttpExtensions.cs` |
| Validation | `WithValidation<T>`, `ValidateAsync` | `ValidationHttpExtensions.cs`, `Filters/ValidationFilter.cs` |
| Errors | `ErrorHandlingMiddleware`, `MinimalCleanArchProblemDetailsFactory` | `Middlewares/`, `Errors/` |
| Execution | `HttpExecutionContext` | `Execution/HttpExecutionContext.cs` |
| Rate limiting | `AddMinimalCleanArchRateLimiting` | `RateLimiting/` |
| Health | `AddMinimalCleanArchHealthChecks`, `StartupHealthCheck` | `HealthChecks/`, `MarkStartupComplete` |
| Cache | `ICacheService`, `CachedRepositoryDecorator` | `Caching/` |
| Telemetry | `AddMinimalCleanArchTelemetry` | `Telemetry/` |
| Versioning | `AddMinimalCleanArchApiVersioning` | `Versioning/` |
| Logging | `AddSerilogLogging` | `Logging/` |
| Seeding host | `DatabaseSeederHostedService` | `Hosting/` |
| Validator scan | `AddValidationFromAssemblyContaining<T>` | `src/MinimalCleanArch.Validation/` |

`ValidationBehavior<TRequest,TResponse>` is unused. Prefer Extensions filters.

## Commands / queries / hosts

This context does not define business commands. It maps HTTP to whatever the app chose:

| Host | Endpoints |
|---|---|
| Sample | `MapTodoEndpoints`, `MapUserEndpoints`, `MapIdentityApi<User>`, health, Scalar |
| Template | `MapTodoEndpoints` (includes optional `POST /api/todos/{id}/restore`), optional `MapAuthEndpoints`, `MapOpenIddictEndpoints`, `MapOAuthEndpoints`, `MapStorageEndpoints`, `MapExternalAuthEndpoints`, `--realtime` `MapMinimalCleanArchRealtime`, `--features` `RequireFeature` on `/api/todos/export`. `--recommended` / `--versioning` register `AddMinimalCleanArchApiVersioning` |

## UI

Scalar at `/scalar/v1` when `IsDevelopment()`. Template auth walkthrough uses Scalar password flow against `/connect/token`.

## Integration

Depends on toolkit `Result`/`Error`/`IExecutionContext`. Does not reference DataAccess. Messaging can replace `IExecutionContext` with `MessagingExecutionContext` after the HTTP registration.
