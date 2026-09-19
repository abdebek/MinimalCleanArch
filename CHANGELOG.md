# Changelog

All notable changes to this project will be documented in this file.

The format is based on Keep a Changelog.

## [Unreleased]

### Added
- Tenancy isolation: kernel `ITenantEntity`; `DbContextBase` / `IdentityDbContextBase` apply a fail-closed EF query filter and stamp `TenantId` on insert. Default is the EF filter, not Postgres RLS.
- Template flag **`--multitenant`**: Todo is tenant-owned; `--auth` issues a `tenant_id` claim. Tests prove tenant A cannot read tenant B Todos.
- Organization membership when `--auth --multitenant`: create org, invite-by-code, org-scoped role rows (data, not Identity seed constants). Join switches the invitee onto the org tenant so Todos are shared.
- Template `--recommended` / `--versioning` / `--all` register `AddMinimalCleanArchApiVersioning` the same way the sample does.
- Template `--caching`: Todo handlers use `ICacheService.GetOrCreateAsync` (memory or Redis), not raw `IMemoryCache`. Cached values are `TodoResponse` DTOs so Redis JSON round-trips.
- `MinimalCleanArch.Jobs`: `IJobScheduler` (recurring + delayed) with `IHostedService` fallback and Wolverine `AddWolverineJobs`. Template `--jobs` / `--messaging` / `--all` run `PurgeSoftDeletedTodos`.
- `MinimalCleanArch.Realtime`: `IRealtimePublisher` (no SignalR types). SignalR adapter in Extensions. Template `--realtime` / `--all` maps `/hubs/realtime` and publishes Todo writes.
- `MinimalCleanArch.Features`: `IFeatureGate.IsEnabled(feature, tenant)` with config backing and optional `IFeatureStore`. Template `--features` / `--all` gates `GET /api/todos/export` (`todo-export`). Flipping the flag in config changes 403 vs 200 without domain changes.
- Template soft-delete restore: `POST /api/todos/{id}/restore` (`RestoreTodoCommand` / `Todo.Restore()`). Optional; `--auth` requires the Admin role.
- Template `--frontend` / `--mobile` emit `apps/web` and `apps/mobile` next to the API (layout slots). API-only generation is unchanged; the API still runs with `dotnet run --project src/{Name}.Api`.
- `--frontend` includes `apps/web/src/lib/auth` (`oidc-client-ts`: authorization-code + PKCE, refresh, Bearer `fetch`). `--auth` seeds public OpenIddict client `mca-spa-client` and CORS for `http://localhost:4321` / `http://localhost:3000`.
- `MinimalCleanArch.Email`: `IEmailSender` port with SMTP and HTTP adapters; template `--auth` calls `AddEmail` instead of copied senders.
- Official **Astro** web scaffold (`--frontend`): login, signup, confirm-email, forgot/reset, Todos, logout, privacy/terms. English/Arabic RTL toggle.
- Second web adapter: **TanStack Start** (`--frontend --webFramework tanstack`) on port 3000, same OIDC client.
- Official **Expo** mobile scaffold (`--mobile`): password grant, `expo-secure-store`, Todo list.
- Playwright smoke: Astro and TanStack Start `apps/web/e2e/smoke.spec.ts` (`API_URL` + `WEB_URL` + `npm run test:e2e`).
- `--controllers`: ASP.NET `TodoController` instead of Minimal API Todo endpoints (same handlers).
- `--fastendpoints`: FastEndpoints Todo host adapter (same handlers; wins over `--controllers`).
- Named EF query filters on EF 10 (`QueryFilters.SoftDelete` / `QueryFilters.Tenant`). `IgnoreSoftDelete()` keeps tenant isolation on **net9 and net10** (EF 9 re-applies the tenant predicate after `IgnoreQueryFilters()`). `IgnoreQueryFilters()` remains the full bypass (admin/seed and process-wide purge).

### Fixed
- Development OpenIddict uses ephemeral signing/encryption keys instead of `AddDevelopment*Certificate()`. On macOS the development cert lives in Keychain and signing `/connect/authorize` hangs or throws `CSSMERR_CSP_USER_CANCELED`.
- `SecurityHeadersOptions.ForApi()` CSP includes `form-action 'self'` (and `base-uri 'none'`) so the API cookie-login HTML form can submit.
- OIDC `handleCallback()` is idempotent so React Strict Mode does not redeem the authorization code twice (`invalid_grant`).
- Generated Development hosts skip HTTPS redirection so SPA PKCE on `http://localhost` is not bounced to HTTPS.
- `--all` / `--ratelimiting` still emit `AuthTestApiFactory` so `ExternalProviderTests` compile after `AuthEndpointTests.cs` is excluded.

## [0.1.20-preview] - 2026-08-02

### Added
- template flag **`--storage`**: `MinimalCleanArch.Storage` + signed upload/download endpoints; Azurite service when `--docker` is set; included in `--all`
- **Cloudflare R2** in `MinimalCleanArch.Storage`: `R2BlobStorage`, unified `BlobStorageOptions` (`Provider` = `Azure` | `R2`), `AddBlobStorage` / `AddR2BlobStorage`
- template storage uses `AddBlobStorage`; appsettings include R2 fields; `ApiEmailSender` posts Cloudflare Email Sending–compatible payload (`address`/`name`, camelCase, omit nulls)
- architecture test: Domain must not reference `MinimalCleanArch.Storage`
- template integration test `Create_Build_Storage_MultiProject`
- validate-templates scenario `multi-storage-sqlite`
- template hygiene: feature-gated appsettings sections, StorageEndpoints exclude, single-TFM package groups (`UseNet9`/`UseNet10`), no empty Scalar options, Wolverine FV only with messaging
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
- `HttpContext.ValidateAsync<T>(...)` in `MinimalCleanArch.Extensions` for validating commands/queries constructed inside Minimal API handlers (RFC 7807 validation problems)
- `R2BlobStorage.CreateDownloadUrlAsync` honors `R2PublicBaseUrl` (CDN/r2.dev override) when set; falls back to S3 presigned URL when unset
- single-project `--docker` template includes Azurite service when `--storage` is set (parity with multi-project)
- `BlobKeyValidator` normalizes blob keys (backslash → `/`, collapses `./`, rejects `..`/absolute/drive paths) for both Azure and R2 providers
- non-Aspire template caching reads `Caching:Redis:ConnectionString` and wires `AddStackExchangeRedisCache` + `AddMinimalCleanArchDistributedCaching` (previously always in-memory outside Aspire); `Caching:Redis` section added to appsettings
- storage endpoint validation reports only the actually-failed fields instead of all three unconditionally
- sample Aspire README updated to reflect the shipped `--aspire` template flag

### Fixed
- Wolverine 6: `AddMinimalCleanArchMessaging*` sets `ServiceLocationPolicy.AllowedButWarn` so constructor-injected MS.DI handlers work (6.0 default `NotAllowed` caused 500s on `IMessageBus.InvokeAsync`)
- `validate-templates.ps1` ignores `*.symbols.nupkg` when selecting the template package to install
- `AzureBlobStorage` now applies `BlobStorageOptions.KeyPrefix` (previously only R2); unified options/docs are accurate for both providers
- `R2BlobStorage.GetBlobAsync` no longer buffers the entire object into a `MemoryStream` (hashes via a streaming drain with an 8 KB buffer)
- template `DatabaseInitializer` no longer silently ignores `Database:ApplyMigrations` on the SQLite branch; migrations path is now provider-agnostic and guarded by `Database.IsRelational()` so EF Core InMemory (tests) skips cleanly
- storage endpoints require authorization when `--auth` is enabled (previously anonymous PUT/GET URL minting for arbitrary keys)
- R2 credentials (`R2ServiceUrl` / `R2AccessKeyId` / `R2SecretAccessKey`) are validated at host start via `ValidateOnStart` instead of failing on first use
- `Create_Build_Storage_MultiProject` template test extended with a live `Create_Build_Run_Storage_Endpoints` test exercising presign + validation + path-traversal rejection
- template integration tests isolate generated apps under `temp/` from repo `Directory.Build.props` so NuGet audit advisories do not fail smoke builds
- template integration tests use a unique HTTP port per run to avoid collisions under parallel xUnit execution
- repository `NoWarn` includes NuGet audit codes (NU1902–NU1904) for known transitive package advisories
- sample registers FluentValidation user request validators and scans the API validators assembly via `AddMinimalCleanArchApi` (DataAnnotations alone were not enforced by `WithValidation`)
- generated Todo integration tests assert ProblemDetails for validation and not-found paths

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
- templates (single + multi) now use preferred host bootstrap: `AddMinimalCleanArchApi(...)` and `UseMinimalCleanArchApiDefaults(...)` whenever API polish features are enabled
- templates reference `MinimalCleanArch.Extensions` for validation, security, rate limiting, health checks, caching, Serilog, and OpenTelemetry feature sets (not only caching/rate limiting)
- template host projects always reference `MinimalCleanArch.Extensions` so endpoints can use `MatchHttp` / `ValidateAsync` / filters
- template Todo and Auth endpoints map failures with `MatchHttp` / `ToProblem` instead of plain string error bodies
- template validation uses `ValidateAsync` after mapping to commands/queries (no per-endpoint `IValidator<T>` injection)
- sample app aligned to the same preferred bootstrap and middleware pipeline
- docs updated to describe the preferred template/host bootstrap path and Result → ProblemDetails mapping

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
