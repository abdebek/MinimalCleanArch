# Changelog

All notable changes to this project will be documented in this file.

The format is based on Keep a Changelog.

## [Unreleased]

### Changed
- Dependency refresh for configured TFMs (net10 default / net9 overrides):
  - Microsoft.AspNetCore / EF / Extensions **10.0.10** (net9 **9.0.18**)
  - Npgsql.EF Core **10.0.3** (net9 **9.0.4**)
  - OpenTelemetry **1.17.0**, Scalar.AspNetCore **2.16.16**, Serilog.AspNetCore **10.0.0** (net9 **9.0.0**)
  - WolverineFx **6.24.0** (+ **`WolverineFx.RuntimeCompilation`** for dynamic handler codegen), OpenIddict **7.6.0**, Azure.Identity **1.21.0**, Azure.Storage.Blobs **12.29.1**
  - Asp.Versioning.Http **10.0.1** (net9 **8.1.1**), test/tooling packages (Testcontainers **4.13.0**, FluentAssertions **8.10.0**, etc.)
  - Lift transitive `Microsoft.OpenApi` to **2.11.0** (NU1903 on 2.0.0)
  - `global.json` SDK roll-forward target **10.0.302**
  - Template package pins aligned with `eng/Directory.Packages.props`

### Added
- Aspire orchestration spike under `samples/MinimalCleanArch.Aspire/` (AppHost + ServiceDefaults; Postgres + Redis + Sample API)
- Sample app supports Aspire-injected connection names `mca` (Postgres) and `redis` (distributed cache); skips MCA OTel console path when Aspire OTLP is present
- template flag **`--aspire`**: generates `{Name}.AppHost` + `{Name}.ServiceDefaults` (Postgres/SQL Server → connection `appdb`, optional Redis → `redis`; mutual exclusion with docker-compose)
- `templates/scripts/validate-templates.ps1` covers Aspire multi/single scaffolds (AppHost build, `appdb`, docker-compose exclusion; `-SkipAspire` to omit)
- repo wrappers `scripts/validate-templates.ps1` / `.sh`; `pack.*` prints validate as next step
- template tests and `validate-templates` clean `temp/` scaffolds by default; `scripts/clean-temp.*` for manual reclaim
- generated `--aspire` apps include `scripts/run-apphost.*` + `smoke-test.*` (compose/kind/deploy scripts only with `--docker`)
- auth handlers map Identity failures to typed errors (`Error.Validation` / `NotFound` / `Unauthorized`) so `MatchHttp` returns 400/404/401 instead of 500
- SSR login uses `Results.LocalRedirect` for relative return URLs; auth integration tests fix Unix `file://` mangling of `/connect/authorize?...`
- template integration test factories dispose temporary `ServiceProvider` via `IAsyncDisposable` (Wolverine-safe)
- rate-limit integration test forces global permit=1 via `ConfigureTestServices`; auth integration tests omitted when `--ratelimiting` is on (covered by auth-only scenarios)
- template `DatabaseInitializer` with SQLite `EnsureCreated` and SQL Server/PostgreSQL `Migrate` (+ Development fallback)
- design-time `AppDbContextFactory` for `dotnet ef migrations`
- template `Database` and `Cors` configuration sections
- repo **Central Package Management** via `eng/Directory.Packages.props` (imported from src/tests/samples only)
- `docs/package-management.md` for CPM, template pin policy, and FluentValidation/Wolverine notes

### Changed
- template Todo use cases live in `TodoCommandHandler` (repository + unit of work); removed `ITodoService` / `TodoService` double abstraction
- template endpoints always call handlers (or Wolverine bus); no service-layer pass-through
- Todo specifications moved to `Application/Specifications` (query concerns, not infrastructure)
- `ApplicationUser` moved from Domain to `Application/Identity` so Domain has no ASP.NET Identity dependency
- multi-project Application no longer references `MinimalCleanArch.Messaging` / Wolverine (host/API owns messaging packages)
- architecture tests assert Domain is free of Identity and Application is free of Wolverine
- CORS is config-driven (`Cors:AllowedOrigins`); non-Development fails closed when empty
- encryption: Development uses Data Protection helpers; non-Development requires `Encryption:Key` (no committed production key)
- production `appsettings` keep bootstrap admin seeding disabled; Development enables demo admin only
- removed unused `FluentValidation.AspNetCore` from Extensions/Validation/sample (FV 12 + DI extensions only)
- aligned `Microsoft.AspNetCore.OpenApi` on net10 to **10.0.3**
- template health-check packages bumped from 8.x to **9.0.0**
- template Serilog.AspNetCore pin aligned to **9.0.0** (matches library)

## [0.1.20-preview] - 2026-07-30

### Added
- `HttpContext.ValidateAsync<T>(...)` in `MinimalCleanArch.Extensions` for validating commands/queries constructed inside Minimal API handlers (RFC 7807 validation problems)

### Changed
- templates (single + multi) now use preferred host bootstrap: `AddMinimalCleanArchApi(...)` and `UseMinimalCleanArchApiDefaults(...)` whenever API polish features are enabled
- templates reference `MinimalCleanArch.Extensions` for validation, security, rate limiting, health checks, caching, Serilog, and OpenTelemetry feature sets (not only caching/rate limiting)
- template host projects always reference `MinimalCleanArch.Extensions` so endpoints can use `MatchHttp` / `ValidateAsync` / filters
- template Todo and Auth endpoints map failures with `MatchHttp` / `ToProblem` instead of plain string error bodies
- template validation uses `ValidateAsync` after mapping to commands/queries (no per-endpoint `IValidator<T>` injection)
- sample app aligned to the same preferred bootstrap and middleware pipeline
- docs updated to describe the preferred template/host bootstrap path and Result → ProblemDetails mapping

### Fixed
- template integration tests isolate generated apps under `temp/` from repo `Directory.Build.props` so NuGet audit advisories do not fail smoke builds
- template integration tests use a unique HTTP port per run to avoid collisions under parallel xUnit execution
- repository `NoWarn` includes NuGet audit codes (NU1902–NU1904) for known transitive package advisories
- sample registers FluentValidation user request validators and scans the API validators assembly via `AddMinimalCleanArchApi` (DataAnnotations alone were not enforced by `WithValidation`)
- generated Todo integration tests assert ProblemDetails for validation and not-found paths


## [0.1.19] - 2026-03-18
- stable release following 0.1.19-preview that fixes the version mismatches when default mcaVersion used with later versions, with no any additional changes

## [0.1.19-preview] - 2026-03-18
- fix version mismatches when default mcaVersion used with later versions

## [0.1.18] - 2026-03-12

### Changed
- unified API error, validation, and rate-limit responses around ASP.NET Core `ProblemDetails`
- `AddMinimalCleanArchExtensions()` now registers ASP.NET Core problem-details services and enriches them with MCA trace and correlation metadata
- `MinimalCleanArch.Validation` now delegates validator scanning to the `MinimalCleanArch.Extensions` implementation so the registration logic has a single source of truth while preserving the existing public APIs
- migrated the repo-owned root solutions and CI references from `.sln` to `.slnx`
- switched generated multi-project template solutions from `.sln` to `.slnx`

### Fixed
- made `IdentityDbContextBase` provider-aware for standard Identity nullable index filters so SQL Server keeps them and PostgreSQL/non-SQL Server providers do not inherit SQL Server filter SQL
- added provider-backed unit coverage for SQL Server and PostgreSQL Identity index filter behavior
- aligned middleware, route filters, `Result` mapping, and rate-limit rejection onto the same RFC 7807 response shape instead of mixing MCA-specific and ASP.NET Core problem-details models

## [0.1.17] - 2026-03-12

### Added
- shared `IExecutionContext` in `MinimalCleanArch`
- `NullExecutionContext` fallback for non-HTTP and test scenarios
- HTTP-backed execution context registration in `MinimalCleanArch.Extensions`
- message-scope-aware execution context registration in `MinimalCleanArch.Messaging`
- execution-context-backed audit adapter in `MinimalCleanArch.Audit`
- non-breaking base DbContext constructor overloads that accept `IExecutionContext`
- `GetCurrentTenantId()` virtual hooks in DataAccess base DbContexts
- messaging options for local queue naming, queue prefixing, dead-letter expiration, and failure-policy hooks
- repository default interface implementations for `AnyAsync`, `SingleOrDefaultAsync`, and `CountAsync(ISpecification<T>)`
- generated app deployment scripts for Docker Compose publishing, deployment, teardown, and smoke tests
- generated app kind smoke-test and deploy scripts for local Kubernetes validation

### Changed
- `AddAuditLogging()` now prefers `IExecutionContext` when one is registered, while preserving HTTP fallback behavior
- sample and generated DbContexts now use `DbContextBase` or `IdentityDbContextBase` as the preferred path for audit stamping and soft-delete filtering
- template and sample messaging setup now targets the updated MinimalCleanArch messaging defaults

### Fixed
- `UseMinimalCleanArchApiDefaults()` now applies middleware in the correct order: correlation ID, security headers, error handling, then rate limiting
- `IAuditContextProvider.GetTenantId()` is now non-breaking for existing implementations through a default interface implementation
- `MessagingOptions.SchemaName` documentation now correctly states that it applies to both SQL Server and PostgreSQL persistence
- NuGet packaging now uses the repository version metadata consistently during restore, packing, and template validation

### Breaking Changes
- none intended in this batch; compatibility shims were added for the expanded repository and audit interfaces
