# Changelog

All notable changes to this project will be documented in this file.

The format is based on Keep a Changelog.

## [Unreleased]

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

### Changed
- `AddAuditLogging()` now prefers `IExecutionContext` when one is registered, while preserving HTTP fallback behavior
- sample and generated DbContexts now use `DbContextBase` or `IdentityDbContextBase` as the preferred path for audit stamping and soft-delete filtering
- template and sample messaging setup now targets the updated MinimalCleanArch messaging defaults

### Fixed
- `UseMinimalCleanArchApiDefaults()` now applies middleware in the correct order: correlation ID, security headers, error handling, then rate limiting
- `IAuditContextProvider.GetTenantId()` is now non-breaking for existing implementations through a default interface implementation
- `MessagingOptions.SchemaName` documentation now correctly states that it applies to both SQL Server and PostgreSQL persistence

### Breaking Changes
- none intended in this batch; compatibility shims were added for the expanded repository and audit interfaces
