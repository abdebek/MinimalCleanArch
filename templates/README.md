# MinimalCleanArch Templates

Project templates for bootstrapping Clean Architecture APIs with MinimalCleanArch, using vertical-slice-style use-case organization inside clean dependency boundaries.

## Quick Start

Default multi-project app (SQLite):

```bash
dotnet new install MinimalCleanArch.Templates
dotnet new mca -n MyApp
cd MyApp
dotnet run --project src/MyApp.Api
```

Recommended single-project app:

```bash
dotnet new mca -n MyApp --single-project --recommended
cd MyApp
dotnet run
```

Open Scalar at `https://localhost:<port>/scalar/v1`.

## What It Builds
- a Minimal API application that starts with MCA package boundaries already in place instead of leaving architecture decisions implicit
- either a layered multi-project solution or a pragmatic single-project application with the same conceptual separation
- an application where domain, application, infrastructure, and host concerns already follow the intended dependency direction
- optional capabilities such as auth, audit logging, messaging, caching, telemetry, and deployment scripts without hand-assembling the baseline

## Choosing a Shape
- Default multi-project template: best when you want strict project boundaries and independent domain/application/infrastructure assemblies.
- `--single-project`: best when you want the same architectural separation but lower solution complexity and faster iteration for smaller services.
- `--recommended`: good default for production-oriented APIs that need HTTP polish and operational basics without every optional subsystem.
- `--all`: good for exploring the full MCA stack, generated tests, and deployment workflows end to end.

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

# Full-featured app
dotnet new mca -n EnterpriseApp --all --db postgres --tests

# Secure API
dotnet new mca -n SecureApp --auth --db postgres

# Public API with rate limiting
dotnet new mca -n PublicApi --single-project --ratelimiting
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
|- tests/
|- Dockerfile
|- docker-compose.yml
```

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
- In Development, Scalar is preconfigured with OAuth2 password flow (`/connect/token`) and a preferred `oauth2` security scheme.
- The bearer token is persisted and automatically reused for secured requests.

## Try Password Reset Email Quickly (SMTP or API)

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

The API sender posts JSON in this shape:

```json
{
  "from": { "email": "no-reply@example.com", "name": "MCA" },
  "to": [{ "email": "user@example.com" }],
  "subject": "Subject",
  "html": "<p>Body</p>",
  "text": "Body"
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
| `--recommended` | Includes: serilog, healthchecks, validation, security, caching, ratelimiting |
| `--all` | Includes: auth, messaging, audit, opentelemetry, storage, docker, tests (plus recommended set) |

### Project Structure
| Option | Default | Description |
|--------|---------|-------------|
| `--single-project` | false | Single project instead of multi-project solution |
| `--tests` | false | Include test projects |
| `--docker` | false | Include Dockerfile and docker-compose.yml (ignored when `--aspire` is set) |
| `--aspire` | false | Include .NET Aspire AppHost + ServiceDefaults for local orchestration |
| `--storage` | false | Include MinimalCleanArch.Storage (Azure Blob / Azurite signed URLs) |

### How Options Affect Architecture
| Option | Main effect on generated solution |
|--------|----------------------------------|
| `--single-project` | Collapses layers into one project while keeping `Domain`, `Application`, `Infrastructure`, and endpoint folders separate by responsibility |
| `--tests` | Adds unit and integration test projects or test targets for the generated app |
| `--docker` | Adds container build and local deployment assets (`Dockerfile`, `docker-compose.yml`, generated `scripts/`) |
| `--recommended` | Enables common API-facing concerns such as logging, validation, health checks, security, caching, and rate limiting |
| `--all` | Builds on `--recommended` and adds auth, messaging, audit, telemetry, storage, tests, and deployment assets |

### Features
| Option | Description |
|--------|-------------|
| `--serilog` | Structured logging with Serilog |
| `--healthchecks` | Health check endpoints |
| `--validation` | FluentValidation integration |
| `--auth` | OpenIddict auth (Identity + OAuth2/OIDC) |
| `--security` | Encryption, security headers, CORS |
| `--caching` | In-memory and Redis caching |
| `--ratelimiting` | Global + endpoint-specific rate limiting with 429 ProblemDetails |
| `--messaging` | Wolverine domain events |
| `--audit` | Audit logging |
| `--opentelemetry` | Distributed tracing |
| `--storage` | Blob storage via `MinimalCleanArch.Storage` (Azure Blob / Azurite) |

### Feature-to-Layer Impact
| Feature | Generated layers most affected | What changes |
|--------|-------------------------------|-------------|
| `--validation` | `Application`, `Api` | Adds validators plus API-side validation registration |
| `--auth` | `Infrastructure`, `Api` | Adds Identity/OpenIddict persistence, auth endpoints, and security setup |
| `--security` | `Infrastructure`, `Api` | Adds encryption/security registrations and HTTP security defaults |
| `--caching` | `Infrastructure`, `Api` | Adds cache configuration and host wiring |
| `--messaging` | `Application`, `Infrastructure`, `Api` | Adds domain-event handlers/contracts plus Wolverine setup and transport wiring |
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
| `--dbName <name>` | MCA_DB | Database name for generated connection strings/compose settings |

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
- `Domain`: business entities (e.g. `Todo`), invariants, repository contracts, value objects, domain events — no infrastructure frameworks and no ASP.NET Identity.
- `Application`: commands, queries, **use-case handlers** (own the business orchestration), specifications, validators, and when `--auth` is on `ApplicationUser` under `Application/Identity`.
- `Infrastructure`: EF Core, OpenIddict wiring, repository implementations, email senders, encryption, caching implementations, and external integrations.
- `Api` or top-level host: endpoint mapping, middleware, auth policies, OpenAPI/Scalar, Wolverine host setup, service registration.

This is the main rule the template is trying to preserve: dependencies point inward toward the domain model, while frameworks and operational concerns stay at the edges.

### Layer Responsibilities

- `Domain`: entities, value objects, domain events, repository contracts, core rules. No infrastructure dependencies.
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
- Registers `AddAzureBlobStorage(configuration)` (section `BlobStorage`)
- Maps `/api/storage/upload-url` and `/api/storage/download-url`
- With `--docker`, adds an **Azurite** service and wires the API connection string

Local defaults use the Azurite/devstore connection (`UseDevelopmentStorage=true`). Override `BlobStorage:ConnectionString` / `ContainerName` for production accounts.

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

Behavior:
- **SQLite**: uses `EnsureCreated` (migrations flag is ignored for SQLite in the initializer).
- **SQL Server / PostgreSQL**: prefers `Database.Migrate()`. If no EF migrations exist yet, Development falls back to `EnsureCreated` with a warning log.
- Outside Development, missing migrations with `ApplyMigrations=true` fails fast (no silent empty schema).

Create migrations after scaffolding (SQL Server/PostgreSQL):

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

Use this when your generated app includes `--auth`.

1. Enable provider handlers:

```csharp
// Single-project:
// Infrastructure/Configuration/IdentityServiceExtensions.cs

// Multi-project:
// MCA.Api/Configuration/IdentityServiceExtensions.cs

// Uncomment providers:
// .AddGoogle(...)
// .AddMicrosoftAccount(...)
// .AddGitHub(...)
```

1. Install GitHub provider package if needed:

```bash
dotnet add package AspNet.Security.OAuth.GitHub
```

1. Add secrets via user-secrets/environment variables:

```json
{
  "Authentication": {
    "Google": { "ClientId": "...", "ClientSecret": "..." },
    "Microsoft": { "ClientId": "...", "ClientSecret": "..." },
    "GitHub": { "ClientId": "...", "ClientSecret": "..." }
  }
}
```

1. Configure provider callback URLs:
- Google: `https://localhost:<port>/signin-google`
- Microsoft: `https://localhost:<port>/signin-microsoft`
- GitHub: `https://localhost:<port>/signin-github`

1. Optional: uncomment external-provider buttons in:
- Single: `Endpoints/AuthEndpoints.cs`
- Multi: `MCA.Api/Endpoints/AuthEndpoints.cs`

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




