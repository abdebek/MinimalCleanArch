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
| `--all` | Includes all features plus: messaging, audit, opentelemetry, docker, tests |

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
| `--mcaVersion <version>` | 0.1.10-preview | MinimalCleanArch package version to reference (local default) |
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
- Template package version is `0.1.10-preview` (local/default). The current stable packages are `0.1.7` (pass `--mcaVersion 0.1.7`).
- Templates reference MinimalCleanArch packages via `--mcaVersion` (default `0.1.10-preview`).
- Validation, CQRS, and messaging are wired: Wolverine-based commands/queries with FluentValidation; durable messaging/outbox is enabled for SQL Server/Postgres when requested.
- Launch settings default to Swagger and random ports between 5000-8000; adjust `Properties/launchSettings.json` if you need fixed ports.
- When using a local package feed, add a `nuget.config` with your `packageSources` (e.g., `D:\C\repos\MinimalCleanArch\artifacts\packages`) before restoring.

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
