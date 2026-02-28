# MinimalCleanArch Templates

Project templates for bootstrapping Clean Architecture APIs with MinimalCleanArch.

## Install

From NuGet:

```bash
dotnet new install MinimalCleanArch.Templates
```

From local preview packages:

```bash
dotnet new uninstall MinimalCleanArch.Templates
dotnet new install ./artifacts/packages
```

## Fastest Start

Default multi-project app (SQLite):

```bash
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

## Try Auth + Scalar Password Flow (5 Minutes)

This is the quickest way to validate OpenIddict + user auth + global Bearer reuse in Scalar.

1. Scaffold and run:

```bash
dotnet new mca -n QuickAuth --single-project --auth --tests --mcaVersion 0.1.13-preview
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

## Try Password Reset + SMTP Quickly

1. Configure `EmailSettings` in `appsettings.json` (or user-secrets):
- `SmtpServer`
- `Port`
- `SenderEmail`
- `EnableSsl`
- `AppBaseUrl`

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

## Template Options

### Presets
| Option | Description |
|--------|-------------|
| `--recommended` | Includes: serilog, healthchecks, validation, security, caching |
| `--all` | Includes: auth, messaging, audit, opentelemetry, docker, tests (plus recommended set) |

### Project Structure
| Option | Default | Description |
|--------|---------|-------------|
| `--single-project` | false | Single project instead of multi-project solution |
| `--tests` | false | Include test projects |
| `--docker` | false | Include Dockerfile and docker-compose.yml |

### Features
| Option | Description |
|--------|-------------|
| `--serilog` | Structured logging with Serilog |
| `--healthchecks` | Health check endpoints |
| `--validation` | FluentValidation integration |
| `--auth` | OpenIddict auth (Identity + OAuth2/OIDC) |
| `--security` | Encryption, security headers, CORS |
| `--caching` | In-memory and Redis caching |
| `--messaging` | Wolverine domain events |
| `--audit` | Audit logging |
| `--opentelemetry` | Distributed tracing |

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
| `--mcaVersion <version>` | 0.1.13-preview | MinimalCleanArch package version |
| `--framework <tfm>` | net10.0 | Target framework (`net9.0` or `net10.0`) |

## Common Examples

```bash
# Production-ready API
dotnet new mca -n OrderService --recommended --db sqlserver --docker

# Full-featured app
dotnet new mca -n EnterpriseApp --all --db postgres --tests

# Secure API
dotnet new mca -n SecureApp --auth --db postgres
```

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

## Auth and Security Notes

- `--auth` automatically enables `--security`.
- Password reset endpoints do not return reset tokens in API responses.
- OAuth demo endpoints (`/oauth/demo/*`) and OpenIddict dev endpoints (`/dev/openiddict/*`) are mapped only in Development.
- Optional bootstrap admin seeding is available via `Seed:*` settings and is disabled by default.
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
  -McaVersion 0.1.13-preview `
  -Framework net10.0 `
  -IncludeNugetOrg
```

## Uninstall

```bash
dotnet new uninstall MinimalCleanArch.Templates
```
