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
|- MyApp.sln
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
dotnet new mca -n QuickAuth --single-project --auth --tests --mcaVersion 0.1.17
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

When you scaffold with `--docker` (or `--all`), the generated app includes `scripts/` for local deployment workflows.

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

PowerShell:

```powershell
pwsh ./scripts/deploy.ps1 -Target kind -ImageTag myapp:local
```

Bash:

```bash
./scripts/deploy.sh --target kind --image-tag myapp:local
```

Notes:
- Compose is the fastest way to validate full local dependencies (API + DB + cache from `docker-compose.yml`).
- The kind smoke path is best for quick API image validation and local cluster checks.
- For SQL Server/PostgreSQL generated apps, prefer Compose unless you also deploy matching DB services to your cluster.
- Compose uses `name: ${COMPOSE_PROJECT_NAME:-mca}`; the generated compose scripts automatically set `COMPOSE_PROJECT_NAME` from the app folder name. You can override it explicitly with `COMPOSE_PROJECT_NAME=...`.

## Template Options

### Presets
| Option | Description |
|--------|-------------|
| `--recommended` | Includes: serilog, healthchecks, validation, security, caching, ratelimiting |
| `--all` | Includes: auth, messaging, audit, opentelemetry, docker, tests (plus recommended set) |

### Project Structure
| Option | Default | Description |
|--------|---------|-------------|
| `--single-project` | false | Single project instead of multi-project solution |
| `--tests` | false | Include test projects |
| `--docker` | false | Include Dockerfile and docker-compose.yml |

### How Options Affect Architecture
| Option | Main effect on generated solution |
|--------|----------------------------------|
| `--single-project` | Collapses layers into one project while keeping `Domain`, `Application`, `Infrastructure`, and endpoint folders separate by responsibility |
| `--tests` | Adds unit and integration test projects or test targets for the generated app |
| `--docker` | Adds container build and local deployment assets (`Dockerfile`, `docker-compose.yml`, generated `scripts/`) |
| `--recommended` | Enables common API-facing concerns such as logging, validation, health checks, security, caching, and rate limiting |
| `--all` | Builds on `--recommended` and adds auth, messaging, audit, telemetry, tests, and deployment assets |

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
| `--mcaVersion <version>` | 0.1.17 | MinimalCleanArch package version |
| `--framework <tfm>` | net10.0 | Target framework (`net9.0` or `net10.0`) |

## Architecture Overview

Generated apps follow a hybrid approach: Clean Architecture for dependency direction and DDD-style domain modeling, plus vertical-slice/CQRS-style handlers for use-case organization.

### Architectural Style

- Clean Architecture for dependency direction and framework isolation
- vertical-slice/CQRS-style organization for commands, queries, handlers, and endpoints
- not a classic “service layer per entity” template; the generated app is intended to group behavior around use cases

### Generated Dependency Direction

- `Domain` depends on nothing else in the generated solution.
- `Application` depends on `Domain`.
- `Infrastructure` depends on `Application` and `Domain`.
- `Api` depends on `Application`, `Infrastructure`, and `Domain`.
- In single-project mode, folders stay separated by responsibility even though they compile into one project.
- HTTP, persistence, messaging, and encryption concerns stay out of `Domain`.

## What Stays Where
- `Domain`: business entities, invariants, repository contracts, specifications, value objects, domain events, and no infrastructure frameworks.
- `Application`: commands, queries, handlers, and use-case orchestration over domain contracts.
- `Infrastructure`: EF Core, Identity/OpenIddict, messaging transports, email senders, encryption, caching implementations, and external integrations.
- `Api` or top-level host: endpoint mapping, middleware, auth policies, OpenAPI/Scalar, service registration, and environment-specific startup behavior.

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
2. Application handler executes use-case through domain contracts/repositories.
3. Domain entities enforce invariants and may raise domain events.
4. Infrastructure persists state and publishes/handles events.
5. Result is mapped to consistent HTTP responses/ProblemDetails.

## Auth and Security Notes

- `--auth` automatically enables `--security`.
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
pwsh ./templates/scripts/validate-templates.ps1 `
  -TemplatePackagePath ./artifacts/packages `
  -LocalFeedPath ./artifacts/packages `
  -McaVersion 0.1.17 `
  -Framework net10.0
```

Validation behavior:
- The script uses the local feed for `MinimalCleanArch.*` packages and `nuget.org` for third-party packages by default.
- This keeps validation deterministic on clean machines and CI agents.
- Pass `-IncludeNugetOrg:$false` only if your local feed also contains every external package referenced by the generated templates.
- Pass `-RunDockerE2E` when you want durable SQL Server and PostgreSQL integration tests to run instead of being skipped.

## Uninstall

```bash
dotnet new uninstall MinimalCleanArch.Templates
```



