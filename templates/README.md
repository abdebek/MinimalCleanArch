# MinimalCleanArch Templates

Project templates for creating Clean Architecture applications with the MinimalCleanArch library.

## Installation

```bash
# From NuGet
dotnet new install MinimalCleanArch.Templates

# From a local build (clean)
dotnet new uninstall MinimalCleanArch.Templates
dotnet new install ./artifacts/packages
```

## Usage

### Basic Usage

```bash
# Create a new project with default settings (multi-project solution, SQLite)
dotnet new mca -n MyApp

# Create with recommended features (production-ready)
dotnet new mca -n MyApp --recommended

# Create with all features
dotnet new mca -n MyApp --all
```

### Options

#### Presets
| Option | Description |
|--------|-------------|
| `--recommended` | Includes: serilog, healthchecks, validation, security, caching |
| `--all` | Includes all features plus: auth, messaging, audit, opentelemetry, docker, tests |

#### Project Structure
| Option | Default | Description |
|--------|---------|-------------|
| `--single-project` | false | Single project instead of multi-project solution |
| `--tests` | false | Include unit test project |
| `--docker` | false | Include Dockerfile and docker-compose.yml |

#### Features
| Option | Description |
|--------|-------------|
| `--serilog` | Structured logging with Serilog |
| `--healthchecks` | Health check endpoints |
| `--validation` | FluentValidation integration |
| `--auth` | OpenIddict authentication (Identity + OAuth2/OpenID Connect) |
| `--security` | Encryption, security headers, CORS |
| `--caching` | In-memory and Redis caching |
| `--messaging` | Wolverine domain events |
| `--audit` | Audit logging |
| `--opentelemetry` | Distributed tracing |

#### Database
| Option | Default | Description |
|--------|---------|-------------|
| `--db sqlite` | Yes | SQLite (default) |
| `--db sqlserver` | | SQL Server |
| `--db postgres` | | PostgreSQL |
| `--dbName <name>` | MCA_DB | Database name used for generated connection strings and Docker compose settings |

#### Versions
| Option | Default | Description |
|--------|---------|-------------|
| `--mcaVersion <version>` | 0.1.13-preview | MinimalCleanArch package version to reference (local default) |
| `--framework <tfm>` | net10.0 | Target framework for generated projects (`net9.0` or `net10.0`) |

### Examples

```bash
# Production-ready API with SQL Server
dotnet new mca -n OrderService --recommended --db sqlserver --docker

# Microservice with messaging and observability
dotnet new mca -n NotificationService --messaging --opentelemetry --db postgres

# Simple prototype
dotnet new mca -n Prototype --single-project

# Full-featured application
dotnet new mca -n EnterpriseApp --all --db sqlserver

# API with authentication (OpenIddict + Identity)
dotnet new mca -n SecureApp --auth --db postgres
```

## Project Structure

### Multi-Project Solution (Default)
```
MyApp/
|- MyApp.sln
|- src/
|  |- MyApp.Domain/         (Entities, Events, Interfaces)
|  |- MyApp.Application/    (Services, DTOs, Handlers)
|  |- MyApp.Infrastructure/ (DbContext, Repositories)
|  |- MyApp.Api/            (Endpoints, Program.cs)
|- tests/
|  |- MyApp.UnitTests/
|- Dockerfile
|- docker-compose.yml
```

### Single Project (--single-project)
```
MyApp/
|- MyApp.csproj
|- Program.cs
|- Domain/
|- Application/
|- Infrastructure/
|- Endpoints/
```

## Uninstall

```bash
dotnet new uninstall MinimalCleanArch.Templates
```

## Notes
- Template package version is `0.1.13-preview` (local/default). The current stable packages are `0.1.7` (pass `--mcaVersion 0.1.7`).
- Templates reference MinimalCleanArch packages via `--mcaVersion` (default `0.1.13-preview`).
- Validation, CQRS, and messaging are wired: Wolverine-based commands/queries with FluentValidation; durable messaging/outbox is enabled for SQL Server/Postgres when requested.
- `--auth` adds OpenIddict 7.2.0 with ASP.NET Core Identity: password, authorization code, and refresh token grants; register/change-password endpoints; seeded default API clients, plus roles scope/claims support. Automatically enables `--security`.
- Optional bootstrap admin seeding is available via `Seed:*` settings (`Seed:EnableBootstrapAdmin`, `Seed:AdminEmail`, `Seed:AdminPassword`, `Seed:AdminRole`), disabled by default.
- Password reset endpoints do not return reset tokens in API responses.
- OAuth demo (`/oauth/demo/*`) and OpenIddict dev endpoints (`/dev/openiddict/*`) are mapped only in Development.
- In Development, Scalar is preconfigured with OAuth2 password flow (`/connect/token`) so you can sign in once and reuse the bearer token across requests.
- Launch settings default to Scalar UI and random ports between 5000-8000; adjust `Properties/launchSettings.json` if you need fixed ports.
- When using a local package feed, add a `nuget.config` with your `packageSources` (e.g., `D:\C\repos\MinimalCleanArch\artifacts\packages`) before restoring.

## External Sign-In (Google, Microsoft, GitHub)

Use this when your generated app includes `--auth`.

1. Enable provider handlers in generated code:
```csharp
// Single-project:
// Infrastructure/Configuration/IdentityServiceExtensions.cs

// Multi-project:
// MCA.Api/Configuration/IdentityServiceExtensions.cs

// Uncomment the providers you want:
// .AddGoogle(...)
// .AddMicrosoftAccount(...)
// .AddGitHub(...)
```

1. Add required NuGet package for GitHub:
```bash
dotnet add package AspNet.Security.OAuth.GitHub
```
`AddGoogle` and `AddMicrosoftAccount` are in `Microsoft.AspNetCore.Authentication.*` packages already referenced by the template when `--auth` is used.

1. Add provider secrets (prefer user-secrets or environment variables, not committed appsettings):
```json
{
  "Authentication": {
    "Google": {
      "ClientId": "YOUR_GOOGLE_CLIENT_ID",
      "ClientSecret": "YOUR_GOOGLE_CLIENT_SECRET"
    },
    "Microsoft": {
      "ClientId": "YOUR_MICROSOFT_CLIENT_ID",
      "ClientSecret": "YOUR_MICROSOFT_CLIENT_SECRET"
    },
    "GitHub": {
      "ClientId": "YOUR_GITHUB_CLIENT_ID",
      "ClientSecret": "YOUR_GITHUB_CLIENT_SECRET"
    }
  }
}
```

1. Configure provider callback URLs in each provider console (replace port with your running HTTPS port):
- Google: `https://localhost:<port>/signin-google`
- Microsoft: `https://localhost:<port>/signin-microsoft`
- GitHub: `https://localhost:<port>/signin-github`
These are handled by ASP.NET Core external auth middleware.

1. (Optional UI) uncomment provider buttons in login page:
- Single-project: `Endpoints/AuthEndpoints.cs`
- Multi-project: `MCA.Api/Endpoints/AuthEndpoints.cs`
Buttons are scaffolded as HTML comments by default.

1. Verify flow:
- Start endpoint (your app): `GET /api/auth/external/GitHub?returnUrl=/`
- Provider returns to middleware callback: `/signin-github`
- Middleware then returns to app finalize endpoint: `/api/auth/external/GitHub/callback`
- Replace `GitHub` with `Google` or `Microsoft` for other providers.

Notes:
- `/api/auth/external/{provider}` must match the provider scheme name you configured (`Google`, `Microsoft`, `GitHub`).
- External login endpoints are mapped automatically when `--auth` is enabled via `MapExternalAuthEndpoints()`.
- The dev SSR login page is available in Development environment at `/auth/login`.
- `GitHub` casing matters in the route segment (`/api/auth/external/GitHub`).
- If you see `GitHub did not provide an email claim`, add `options.Scope.Add("user:email")` in `.AddGitHub(...)` and ensure the GitHub account has at least one verified email.
- If GitHub says `redirect_uri is not associated`, set GitHub callback URL to exactly your app callback (example: `https://localhost:5026/signin-github`).

## Quick database containers

- SQL Server:

  ```bash
  docker run -e "ACCEPT_EULA=Y" -e "MSSQL_SA_PASSWORD=StrongP!12asd" -p 1433:1433 --name sqlserver -d mcr.microsoft.com/mssql/server:2022-latest
  ```

- PostgreSQL:

  ```bash
  docker run -e "POSTGRES_USER=postgres" -e "POSTGRES_PASSWORD=postgres" -e "POSTGRES_DB=mca" -p 5432:5432 --name postgres -d postgres:16-alpine
  ```

## Testing the template

- Standard tests (unit + lightweight API integration) run with `dotnet test` on the generated solution.
- Optional Docker E2E tests for durable messaging:
  - Set `RUN_DOCKER_E2E=1` to enable.
  - Set `RUN_DOCKER_DB=postgres` (default `sqlserver`) to choose provider.
  - Docker must be available; otherwise these tests are skipped.
