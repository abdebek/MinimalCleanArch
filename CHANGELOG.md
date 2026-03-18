# Changelog

All notable changes to this project will be documented in this file.

The format is based on Keep a Changelog.

## [Unreleased]

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
