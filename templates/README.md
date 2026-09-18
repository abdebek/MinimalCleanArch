# MinimalCleanArch Templates

Project templates for bootstrapping Clean Architecture APIs with MinimalCleanArch, using vertical-slice-style use-case organization inside clean dependency boundaries.

## Quick Start

Supported teaching path (multi-project, `--recommended --auth`):

```bash
dotnet new install MinimalCleanArch.Templates
dotnet new mca -n MyApp --recommended --auth
cd MyApp
dotnet run --project src/MyApp.Api
```

Open Scalar at `https://localhost:<port>/scalar/v1`. The in-repo sample is a package smoke demo, not this path. Do not copy `samples/MinimalCleanArch.Sample` endpoints that write the aggregate; generated apps use `TodoCommandHandler`.

Minimal API-only:

```bash
dotnet new mca -n MyApp
cd MyApp
dotnet run --project src/MyApp.Api
```

Single-project (optional shape, not the teaching default):

```bash
dotnet new mca -n MyApp --single-project --recommended --auth
cd MyApp
dotnet run
```

## What It Builds
- a Minimal API application that starts with MCA package boundaries already in place instead of leaving architecture decisions implicit
- either a layered multi-project solution or a pragmatic single-project application with the same conceptual separation
- an application where domain, application, infrastructure, and host concerns already follow the intended dependency direction
- optional capabilities such as auth, audit logging, messaging, caching, telemetry, and deployment scripts without hand-assembling the baseline
- Todo soft-delete restore at `POST /api/todos/{id}/restore` (optional; drop the route if you do not want undelete). With `--auth` it requires the Admin role

## Choosing a Shape
- **Teaching default:** `--recommended --auth` (multi-project). Production HTTP polish plus Identity/OpenIddict. This is the supported bootstrap.
- Default `dotnet new mca` (no flags): API-only multi-project, SQLite. Fine for a minimal host; not the teaching path.
- `--single-project`: same conceptual layers, one assembly. Optional, not the teaching default.
- `--all`: explore the full MCA stack, generated tests, and deployment workflows.

## Install

From NuGet:

```bash
dotnet new install MinimalCleanArch.Templates
```

From local packages:

```bash
dotnet new uninstall MinimalCleanArch.Templates
dotnet new install ./artifacts/packages
```

## Common Examples

```bash
# Default multi-project app
dotnet new mca -n OrderService

# Production-ready API
dotnet new mca -n OrderService --recommended --db sqlserver --docker

# Postgres + compose with an explicit database name
dotnet new mca -n Shop --db postgres --dbName shop --docker

# Full-featured app
dotnet new mca -n EnterpriseApp --all --db postgres --tests

# Secure API
dotnet new mca -n SecureApp --auth --db postgres

# Auth + tenant isolation (EF query filter; each user is their own tenant until orgs exist)
dotnet new mca -n Shop --auth --multitenant --tests

# Realtime Todo hub (SignalR /hubs/realtime)
dotnet new mca -n Shop --realtime --tests

# Feature flags (config-gated Todo export)
dotnet new mca -n Shop --features --tests

# Public API with rate limiting
dotnet new mca -n PublicApi --single-project --ratelimiting

# API + reserved client folders (layout only; official web/mobile scaffolds come later)
dotnet new mca -n Shop --frontend --mobile
# API still: dotnet run --project src/Shop.Api
```

For template flags, architecture details, auth notes, and deployment workflows, use the sections below after choosing a starting point.

## What Gets Scaffolded

Multi-project (default):

```text
MyApp/
|- MyApp.slnx
|- src/
|  |- MyApp.Domain/
|  |- MyApp.Application/
|  |- MyApp.Infrastructure/
|  |- MyApp.Api/
|- tests/                  # with --tests
|- apps/web/               # with --frontend (`src/lib/auth` OIDC PKCE)
|- apps/mobile/            # with --mobile (layout slot)
|- Dockerfile              # with --docker
|- docker-compose.yml      # with --docker
```

The four projects live under a filesystem `src/` directory. `{Name}.slnx` groups the same projects in a solution folder named `/src/`.

Single project:

```text
MyApp/
|- MyApp.csproj
|- Program.cs
|- Domain/
|- Application/
|- Infrastructure/
|- Endpoints/
```

## Try Auth + Scalar Password Flow (5 Minutes)

This is the quickest way to validate OpenIddict + user auth + global Bearer reuse in Scalar.

1. Scaffold and run:

```bash
dotnet new mca -n QuickAuth --single-project --auth --tests --mcaVersion 0.1.20-preview
cd QuickAuth
dotnet run
```

1. Open `https://localhost:<port>/scalar/v1`.

1. Register a user with `POST /api/auth/register`:

```json
{
  "email": "demo@example.com",
  "password": "TempPass!123",
  "firstName": "Demo",
  "lastName": "User"
}
```

1. Click `Authorize` in Scalar and use the preconfigured `oauth2` password flow:
- Username: `demo@example.com`
- Password: `TempPass!123`

1. Call an authenticated endpoint, for example `POST /api/auth/change-password`:

```json
{
  "currentPassword": "TempPass!123",
  "newPassword": "TempPass!456"
}
```

1. Optional: inspect claims with `GET /connect/userinfo`.

Notes:
- In Development, Scalar is preconfigured with OAuth2 password flow (`/connect/token`) and a preferred `oauth2` security scheme. Selected scopes include `offline_access`, so the token response includes a **refresh token**.
- The bearer token is persisted and automatically reused for secured requests.
- Refresh (not in Scalar’s password button): `POST /connect/token` with `grant_type=refresh_token`, the refresh token, and the web client id/secret. See [05. Lifecycles](../docs/05-lifecycles.md) (login → refresh → logout).
- Cookie `POST /api/auth/logout` does not revoke OpenIddict refresh tokens; use `POST /connect/logout` or `/connect/revoke`.

## Try Password Reset Email Quickly (SMTP or API)

`--auth` registers `MinimalCleanArch.Email` (`AddEmail`) for SMTP or HTTP API transport. Auth-specific confirm/reset templates stay in the generated app (`IEmailService`).

1. Configure `EmailSettings` in `appsettings.json` (or user-secrets):
- `Provider` (`Smtp` or `Api`)
- `SenderEmail`
- `AppBaseUrl`
- SMTP mode: `SmtpServer`, `Port`, `EnableSsl`, credentials if needed
- API mode: `Api:Endpoint`, optional `Api:ApiKey`, `Api:ApiKeyHeaderName`, `Api:ApiKeyPrefix`, `Api:Headers`

1. Example API-mode configuration:

```json
{
  "EmailSettings": {
    "Provider": "Api",
    "SenderEmail": "no-reply@example.com",
    "SenderName": "MCA",
    "AppBaseUrl": "https://localhost:5001",
    "Api": {
      "Endpoint": "https://your-email-api.example.com/send",
      "ApiKey": "your-api-key",
      "ApiKeyHeaderName": "Authorization",
      "ApiKeyPrefix": "Bearer",
      "Headers": {
        "X-Tenant": "demo"
      }
    }
  }
}
```

The API sender posts Cloudflare Email Sending–compatible JSON (camelCase, nulls omitted):

```json
{
  "from": { "address": "no-reply@example.com", "name": "MCA" },
  "to": "user@example.com",
  "subject": "Subject",
  "html": "<p>Body</p>",
  "text": "Body"
}
```

Cloudflare Email Sending example:

```json
{
  "EmailSettings": {
    "Provider": "Api",
    "SenderEmail": "no-reply@yourdomain.com",
    "SenderName": "MCA",
    "AppBaseUrl": "https://localhost:5001",
    "Api": {
      "Endpoint": "https://api.cloudflare.com/client/v4/accounts/<account_id>/email/routing/send",
      "ApiKey": "<api-token>",
      "ApiKeyHeaderName": "Authorization",
      "ApiKeyPrefix": "Bearer"
    }
  }
}
```

1. Trigger reset flow:
- `POST /api/auth/forgot-password` with an email.
- Response intentionally does not include reset token.

1. Use your email provider/local SMTP capture to obtain the link/token.

1. Complete reset via `POST /api/auth/reset-password`.

## Try Durable Messaging (Outbox) Quickly

Use SQL Server or PostgreSQL (SQLite is in-memory messaging only).

1. Start DB container:

SQL Server:

```bash
docker run -e "ACCEPT_EULA=Y" -e "MSSQL_SA_PASSWORD=StrongP!12asd" -p 1433:1433 --name sqlserver -d mcr.microsoft.com/mssql/server:2022-latest
```

PostgreSQL:

```bash
docker run -e "POSTGRES_USER=postgres" -e "POSTGRES_PASSWORD=postgres" -e "POSTGRES_DB=mca" -p 5432:5432 --name postgres -d postgres:16-alpine
```

1. Scaffold with messaging + DB provider:

```bash
dotnet new mca -n DurableApp --all --db postgres --tests
```

## Deployment Scripts (Generated App)

Generated `scripts/` depend on how you scaffold:

### Docker Compose / kind (`--docker` or `--all`, not with `--aspire`)

Recommended default: Docker Compose

PowerShell:

```powershell
pwsh ./scripts/deploy.ps1 -Target compose
pwsh ./scripts/compose-down.ps1 -RemoveVolumes
```

Bash:

```bash
./scripts/deploy.sh --target compose
./scripts/compose-down.sh --remove-volumes
```

Optional local Kubernetes smoke path (kind):

```bash
./scripts/deploy.sh --target kind --image-tag myapp:local
# or: pwsh ./scripts/deploy.ps1 -Target kind -ImageTag myapp:local
```

Notes:
- Compose is the fastest way to validate full local dependencies (API + DB + cache from `docker-compose.yml`).
- Compose uses `name: ${COMPOSE_PROJECT_NAME:-mca}`; generated compose scripts set `COMPOSE_PROJECT_NAME` from the app folder name.
- Shared helper: `scripts/smoke-test.sh` / `.ps1` (HTTP readiness poll).

### Aspire (`--aspire`)

Docker-compose assets and compose/kind/deploy scripts are **omitted**. Instead you get:

```bash
./scripts/run-apphost.sh
# or: pwsh ./scripts/run-apphost.ps1
# equivalent: dotnet run --project <Name>.AppHost
```

Also includes `scripts/smoke-test.*` for hitting the API once the host is up (use the Aspire dashboard URL / mapped ports).

`--aspire` and `--docker` are mutually exclusive (`--aspire` wins).
## Template Options

### Presets
| Option | Description |
|--------|-------------|
| `--recommended` | Includes: serilog, healthchecks, validation, security, caching, ratelimiting, versioning |
| `--all` | Includes: auth, messaging (jobs), realtime, features, audit, opentelemetry, storage, docker, tests (plus recommended set) |

### Project Structure
| Option | Default | Description |
|--------|---------|-------------|
| `--single-project` | false | Single project instead of multi-project solution |
| `--tests` | false | Include test projects |
| `--docker` | false | Include Dockerfile and docker-compose.yml (ignored when `--aspire` is set) |
| `--aspire` | false | Include .NET Aspire AppHost + ServiceDefaults for local orchestration |
| `--storage` | false | Include MinimalCleanArch.Storage (Azure Blob / Azurite signed URLs) |
| `--frontend` | false | Emit `apps/web` (Astro + OIDC PKCE). Pair with `--auth`. Not in `--all` |
| `--webFramework` | astro | `astro` or `tanstack` (Vite + React). Used with `--frontend` |
| `--mobile` | false | Emit `apps/mobile` Expo scaffold. Not in `--all` |
| `--controllers` | false | Host Todos with ASP.NET controllers instead of Minimal API endpoint helpers |

### How Options Affect Architecture
| Option | Main effect on generated solution |
|--------|----------------------------------|
| `--single-project` | Collapses layers into one project while keeping `Domain`, `Application`, `Infrastructure`, and endpoint folders separate by responsibility |
| `--tests` | Adds unit and integration test projects or test targets for the generated app |
| `--docker` | Adds container build and local deployment assets (`Dockerfile`, `docker-compose.yml`, generated `scripts/`) |
| `--frontend` | Adds `apps/web` (Astro pages + `src/lib/auth` PKCE). `--auth` seeds `mca-spa-client` |
| `--webFramework tanstack` | Same `apps/web` slot, Vite + React instead of Astro |
| `--mobile` | Adds Expo app (password grant + secure store) |
| `--controllers` | `MapControllers` + `TodoController`; does not call `MapTodoEndpoints` |
| `--recommended` | Enables common API-facing concerns such as logging, validation, health checks, security, caching, rate limiting, and API versioning |
| `--all` | Builds on `--recommended` and adds auth, messaging, jobs, realtime, features, audit, telemetry, storage, tests, and deployment assets |

### Features
| Option | Description |
|--------|-------------|
| `--serilog` | Structured logging with Serilog |
| `--healthchecks` | Health check endpoints |
| `--validation` | FluentValidation integration |
| `--auth` | OpenIddict auth (Identity + OAuth2/OIDC) |
| `--multitenant` | Row isolation via EF query filter on `ITenantEntity` (default; not Postgres RLS). Use with `--auth` so login issues a `tenant_id` claim and scaffolds org membership + invite-by-code |
| `--security` | Encryption, security headers, CORS |
| `--caching` | Register `ICacheService` (memory or Redis) and use it for Todo read-through |
| `--ratelimiting` | Global + endpoint-specific rate limiting with 429 ProblemDetails |
| `--versioning` | Register `AddMinimalCleanArchApiVersioning` (Asp.Versioning; same as the sample). Included in `--recommended` / `--all` |
| `--messaging` | Wolverine domain events (also enables `--jobs`) |
| `--jobs` | `IJobScheduler` (recurring + delayed). Sample: daily purge of soft-deleted Todos. Included with `--messaging` / `--all` |
| `--realtime` | `IRealtimePublisher` + SignalR hub `/hubs/realtime`. Todo writes publish on channel `todos`. Included with `--all` |
| `--features` | `IFeatureGate` (config-backed). `GET /api/todos/export` gated by `Features:Flags:todo-export`. Included with `--all` |
| `--audit` | Audit logging |
| `--opentelemetry` | Distributed tracing |
| `--storage` | Blob storage via `MinimalCleanArch.Storage` (Azure Blob / Azurite) |

### Feature-to-Layer Impact
| Feature | Generated layers most affected | What changes |
|--------|-------------------------------|-------------|
| `--validation` | `Application`, `Api` | Adds validators plus API-side validation registration |
| `--auth` | `Infrastructure`, `Api` | Adds Identity/OpenIddict persistence, auth endpoints, and security setup. Soft-delete restore (`POST /api/todos/{id}/restore`) becomes Admin-only |
| `--multitenant` | `Domain`, `Infrastructure`, `Api` | Todo implements `ITenantEntity`; users get a `tenant_id` claim; EF filter hides other tenants' rows; `--auth` adds `/api/organizations` (create, invite-by-code, join) with org roles as data |
| `--security` | `Infrastructure`, `Api` | Adds encryption/security registrations and HTTP security defaults |
| `--caching` | `Application`, `Api` | Registers `ICacheService`; Todo handlers read through it (not raw `IMemoryCache`) |
| `--versioning` | `Api` | Calls `AddMinimalCleanArchApiVersioning` (default v1, query/header/url readers) |
| `--messaging` | `Application`, `Infrastructure`, `Api` | Adds domain-event handlers/contracts plus Wolverine setup and transport wiring |
| `--jobs` | `Application`, `Infrastructure`, `Api` | Registers `AddJobs` + `PurgeSoftDeletedTodos`; hard-delete via `IgnoreQueryFilters`. `--messaging` adds `AddWolverineJobs` |
| `--realtime` | `Application`, `Api` | Registers SignalR hub `/hubs/realtime`; Todo writes publish via `IRealtimePublisher` |
| `--features` | `Api` | Registers `AddFeatures`; `GET /api/todos/export` uses `RequireFeature("todo-export")` |
| `--audit` | `Infrastructure`, `Api` | Adds audit persistence, interception, and registration |
| `--opentelemetry` | `Api` | Adds tracing/telemetry host configuration |
| `--storage` | `Api` (host) | Adds `IBlobStorage` registration, `/api/storage/*` signed URL endpoints, Azurite in compose when `--docker` |
| `--docker` | solution root / host assets | Adds container and deployment workflow assets, not domain rules |

### Database
| Option | Default | Description |
|--------|---------|-------------|
| `--db sqlite` | Yes | SQLite |
| `--db sqlserver` | | SQL Server |
| `--db postgres` | | PostgreSQL |
| `--dbName <name>` | application name (`-n`) | Database name for generated connection strings and compose (`POSTGRES_DB`, `Database=`). Omitted/empty uses the template name. |

### Versions
| Option | Default | Description |
|--------|---------|-------------|
| `--mcaVersion <version>` | 0.1.20-preview | MinimalCleanArch package version |
| `--framework <tfm>` | net10.0 | Target framework (`net9.0` or `net10.0`) |

## Architecture Overview

Generated apps follow a hybrid approach: Clean Architecture for dependency direction and DDD-style domain modeling, plus vertical-slice/CQRS-style handlers for use-case organization.

### Architectural Style

- Clean Architecture for dependency direction and framework isolation
- vertical-slice/CQRS-style organization for commands, queries, handlers, and endpoints
- not a classic “service layer per entity” template; the generated app is intended to group behavior around use cases

### Host Bootstrap (preferred MCA APIs)

When any API polish feature is enabled (`--validation`, `--security`, `--ratelimiting`, `--healthchecks`, `--caching`, `--serilog`, `--opentelemetry`, or via `--recommended` / `--all`), the generated host uses:

```csharp
// Service registration
builder.Services.AddMinimalCleanArchApi(options =>
{
    options.AddValidatorsFromAssemblyContaining<CreateTodoCommandValidator>();
    options.EnableRateLimiting = true;
    options.ConfigureRateLimiting = config =>
        builder.Configuration.GetSection("RateLimiting").Bind(config);
});

// Middleware pipeline (correlation ID → security headers → error handling → rate limiting)
app.UseMinimalCleanArchApiDefaults(pipeline =>
{
    pipeline.UseRateLimiting = true;
    pipeline.UseApiSecurityHeaders = true;
});
```

Feature blocks for database, auth, messaging, audit, OpenTelemetry exporters, and health-check probes remain explicit next to that bootstrap. Prefer these entry points over hand-wiring correlation middleware, problem details, validators, and rate limiting separately.

`MinimalCleanArch.Extensions` is referenced whenever those polish features are on so the host can call the preferred bootstrap APIs.

### Generated Dependency Direction

- `Domain` depends on nothing else in the generated solution (no ASP.NET Identity, EF, or Wolverine).
- `Application` depends on `Domain` (and Identity *stores* only when `--auth` is on, for `ApplicationUser` / `UserManager`).
- `Infrastructure` depends on `Application` and `Domain`.
- `Api` depends on `Application`, `Infrastructure`, and `Domain`.
- In single-project mode, folders stay separated by responsibility even though they compile into one project.
- HTTP, persistence, messaging, and encryption concerns stay out of `Domain`.

## What Stays Where
- `Domain`: business entities (e.g. `Todo`), invariants, repository contracts, domain events — no infrastructure frameworks and no ASP.NET Identity. There is no value-object base type.
- `Application`: commands, queries, **use-case handlers** (own the business orchestration), specifications, validators, and when `--auth` is on `ApplicationUser` under `Application/Identity`.
- `Infrastructure`: EF Core, OpenIddict wiring, repository implementations, email senders, encryption, caching implementations, and external integrations.
- `Api` or top-level host: endpoint mapping, middleware, auth policies, OpenAPI/Scalar, Wolverine host setup, service registration.

This is the main rule the template is trying to preserve: dependencies point inward toward the domain model, while frameworks and operational concerns stay at the edges.

### Layer Responsibilities

- `Domain`: entities, domain events, repository contracts, core rules. No infrastructure dependencies. No value-object base type.
- `Application`: commands/queries, handlers, and orchestration of use-cases using domain contracts.
- `Infrastructure`: EF Core, Identity/OpenIddict wiring, email providers, repository implementations, external integrations.
- `Api` (multi-project) or `Endpoints` + `Program.cs` (single-project): HTTP transport, endpoint mapping, auth policies, middleware.

### Dependency Direction

- `Domain` depends on nothing else.
- `Application` depends on `Domain`.
- `Infrastructure` depends on `Application` and `Domain`.
- `Api` depends on all required layers and composes the app at startup.

### Typical Request Flow

1. Endpoint receives HTTP request and maps payload to command/query.
2. Optional FluentValidation runs via `HttpContext.ValidateAsync(...)` (or `WithValidation<T>()` for body parameters).
3. Endpoint invokes the application handler (directly, or via Wolverine `IMessageBus` when `--messaging` is on).
4. Handler runs the use case through domain contracts/repositories and specifications.
5. Domain entities enforce invariants and may raise domain events.
6. Infrastructure persists state; messaging host publishes/handles events when enabled.
7. Endpoint maps `Result` / `Result<T>` with `MatchHttp` / `ToProblem` to RFC 7807 ProblemDetails.

## Aspire orchestration (`--aspire`)

When `--aspire` is set, the scaffold includes:

- `{Name}.AppHost` — starts Postgres or SQL Server (when `--db` is not sqlite), optional Redis (when caching is on), and the API
- `{Name}.ServiceDefaults` — OTLP telemetry, resilience, `/alive` + `/health/ready`

```bash
# Recommended happy path
dotnet new mca -n OrderService --recommended --aspire --db postgres
cd OrderService
dotnet run --project OrderService.AppHost
# or: ./scripts/run-apphost.sh
```

## Blob storage (`--storage`)

When `--storage` (or `--all`) is set:

- References `MinimalCleanArch.Storage`
- Registers `AddBlobStorage(configuration)` (section `BlobStorage`)
- `BlobStorage:Provider` = `Azure` (default, Azurite-compatible) or `R2` (Cloudflare R2)
- Maps `/api/storage/upload-url` and `/api/storage/download-url`
- With `--docker`, adds an **Azurite** service and wires the API connection string (Azure provider)

Local defaults use the Azurite/devstore connection (`UseDevelopmentStorage=true`). Override `BlobStorage:ConnectionString` / `ContainerName` for production Azure accounts, or set `Provider` to `R2` and fill `R2ServiceUrl`, `R2AccessKeyId`, `R2SecretAccessKey`, `R2BucketName` (optional `R2PublicBaseUrl`, `KeyPrefix`).

```bash
dotnet new mca -n MediaApi --recommended --storage --docker
# start Azurite + API via compose, then:
# POST /api/storage/upload-url  { "blobKey": "uploads/a.pdf", "contentType": "application/pdf", "byteLength": 1024 }
```

Connection names injected by AppHost (stable; not renamed with the project):

| Resource | Connection string name |
|----------|------------------------|
| Database | `appdb` |
| Redis (if caching) | `redis` |

Notes:
- Requires **Docker** for container resources.
- `--aspire` disables docker-compose generation (`--docker` / `--all` compose assets are omitted).
- Prefer `--db postgres` or `--db sqlserver` with Aspire; SQLite still works but without a DB container.
- When Aspire OTLP is present, console OpenTelemetry exporters from `--opentelemetry` are skipped to avoid double wiring.

## Database initialization

Generated apps initialize the schema at startup based on `Database:*` settings:

| Setting | Production default | Development default |
|--------|--------------------|---------------------|
| `Database:EnsureCreated` | `false` | `true` |
| `Database:ApplyMigrations` | `false` | `true` |

Behavior is the same for SQLite, SQL Server, and PostgreSQL (`DatabaseInitializer` has no SQLite special case):

- Both flags false: no auto-initialization.
- `Database:ApplyMigrations=true` on a relational provider: `MigrateAsync` when EF migrations exist; if none exist, Development (or `Database:EnsureCreated=true`) falls back to `EnsureCreated` with a warning.
- Only `Database:EnsureCreated=true`: `EnsureCreated`.
- Outside Development, `ApplyMigrations=true` with no migrations fails fast (no silent empty schema).
- Non-relational providers (EF Core InMemory in tests) skip `ApplyMigrations`.

Create migrations after scaffolding:

```bash
# Single-project
dotnet ef migrations add InitialCreate -o Infrastructure/Data/Migrations

# Multi-project
dotnet ef migrations add InitialCreate \
  --project src/MyApp.Infrastructure \
  --startup-project src/MyApp.Api \
  -o Data/Migrations
```

A design-time `AppDbContextFactory` is included for EF tools.

## Auth and Security Notes

- `--auth` automatically enables `--security`.
- CORS is config-driven via `Cors:AllowedOrigins`. Empty list: Development allows any origin; non-Development allows none (fail closed).
- `Encryption:Key` is empty in production `appsettings.json`. Development uses Data Protection helpers (or a demo key); outside Development a real key is required when `--security` is enabled—use user-secrets, environment variables, or a vault.
- Password reset endpoints do not return reset tokens in API responses.
- OAuth demo endpoints (`/oauth/demo/*`) and OpenIddict dev endpoints (`/dev/openiddict/*`) are mapped only in Development.
- Default demo/scalar client id is `OpenIddict:Clients:Web:ClientId` (defaults to `mca-web-client`). You can override per-request with `/oauth/demo/start?clientId=...`.
- Development defaults seed a bootstrap admin (`admin@example.com` / `Admin123!`) via `appsettings.Development.json`.
- OpenIddict dev client redirect URIs are seeded from both `App:BaseUrl` and runtime `ASPNETCORE_URLS`, reducing localhost port mismatch issues.
- Bootstrap admin seeding is controlled by `Seed:*` settings (`appsettings.json` defaults to disabled; `appsettings.Development.json` enables a demo admin by default).
- Outside Development, OpenIddict client secret and certificate settings are validated on startup.

## External Sign-In (Google, Microsoft, GitHub)

Use this when your generated app includes `--auth`. Providers register themselves when **both** `ClientId` and `ClientSecret` are non-empty. Empty `appsettings` values keep the scheme off. Do not put secrets in source; use user-secrets or environment variables.

```bash
dotnet user-secrets set "Authentication:Google:ClientId" "<id>"
dotnet user-secrets set "Authentication:Google:ClientSecret" "<secret>"
```

Or environment: `Authentication__Google__ClientId` / `Authentication__Google__ClientSecret` (same shape for Microsoft and GitHub).

Then in Development: `GET /api/auth/external/Google` (or Microsoft / GitHub) challenges that provider. Login HTML includes the same links. Unconfigured providers return 404. `GET /api/auth/external/providers` lists only schemes that have both ClientId and ClientSecret.

Provider callback URLs (register these at the IdP):
- Google: `https://localhost:<port>/signin-google`
- Microsoft: `https://localhost:<port>/signin-microsoft`
- GitHub: `https://localhost:<port>/signin-github`

## Validate Templates Locally

```bash
# From repo root — pack then validate
./scripts/pack.sh --package-version 0.1.20-preview
./scripts/validate-templates.sh -McaVersion 0.1.20-preview -Framework net10.0

# PowerShell equivalents
# ./scripts/pack.ps1 -PackageVersion 0.1.20-preview
# ./scripts/validate-templates.ps1 -McaVersion 0.1.20-preview -Framework net10.0
```

`scripts/validate-templates.*` wraps `templates/scripts/validate-templates.ps1` (implementation lives under `templates/scripts/`).

Validation behavior:
- Scaffolds and **builds** multi/single variants (default, recommended, auth, all, SQL Server, Postgres, SQLite).
- Includes **Aspire** scenarios by default:
  - multi `--recommended --aspire --db postgres` (builds `{Name}.AppHost`, asserts `scripts/run-apphost.*`)
  - single `--single-project --recommended --aspire --db sqlserver` (builds `{Name}.AppHost`, checks nested-project exclusions)
- Aspire checks assert stable connection name `appdb`, `AddServiceDefaults` / `MapDefaultEndpoints`, no `docker-compose.yml`, no compose/kind deploy scripts.
- Isolates generated apps from the repo `Directory.Build.props` (CPM / TreatWarningsAsErrors / NuGet audit).
- Uses the local feed for `MinimalCleanArch.*` packages and `nuget.org` for third-party packages by default.
- Pass `-IncludeNugetOrg:$false` only if your local feed also contains every external package referenced by the generated templates.
- Pass `-SkipAspire` to omit Aspire scaffolds (e.g. offline without Aspire packages).
- Pass `-RunDockerE2E` when you want durable SQL Server and PostgreSQL integration tests to run instead of being skipped.
- On **success**, deletes the run directory under `temp/validate/` (pass `-KeepOutput` to retain). On **failure**, keeps it for debugging.
- Template xUnit tests clean each run’s `temp/MCA_Tests/<id>/` unless `MCA_KEEP_TEMPLATE_OUTPUT=1`.
- Manual cleanup: `./scripts/clean-temp.sh` (or `pwsh ./scripts/clean-temp.ps1`).

## Uninstall

```bash
dotnet new uninstall MinimalCleanArch.Templates
```




