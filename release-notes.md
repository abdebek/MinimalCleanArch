## MinimalCleanArch {{TAG}}

### Summary
This release publishes MinimalCleanArch packages and templates at version `{{VERSION}}`.
Detailed commit and pull request notes are auto-generated below by GitHub.

### Compatibility
- Target framework: **.NET 11 (`net11.0`) / C# 15**
- Delivery channels: NuGet packages + `dotnet new` templates

### Breaking Changes
- Single-target `.NET 11` across all 12 packages, samples, and templates (`.NET 8`/`9`/`10` removed).
- `Result` and `Result<TValue>` are now non-boxing C# 15 `[Union]` `readonly struct`s. Replace `result.Value` with C# 15 union pattern matching (`result switch { T v => ..., Error e => ... }`), `TryGetValue(...)`, or `GetValueOrThrow()`.
- `Error.Details` removed in favor of immutable `Error.Metadata`.
- `Result<TValue>.Map` and `Bind` no longer swallow unexpected exceptions.
- `IUnitOfWork` now inherits `IAsyncDisposable`.
- Removed unused `ValidationFilter<T>` classes and obsolete `EncryptionOptions.IV`.
- Removed legacy EF Core 9 query filter fallbacks in favor of EF named query filters (`QueryFilters.SoftDelete` / `QueryFilters.Tenant`).

### Migration Notes
- Update package references and SDK (`global.json`) to `.NET 11` and MinimalCleanArch `{{VERSION}}`.
- Replace `result.Value` usages with C# 15 union pattern matching (`result switch { T v => ..., Error e => ... }` or `result is T v`), `result.TryGetValue(out T? value)` (explicit type), or `result.GetValueOrThrow()`.
- Existing v1 AES-CBC encrypted database columns decrypt transparently under the upgraded AES-256-GCM `AesEncryptionService`; new writes use authenticated `MCA2` ciphertexts.
- Re-run restore/build/tests after upgrading to confirm environment-specific behavior.

### Packages published
- `MinimalCleanArch`: core domain contracts, C# 15 union `Result`/`Result<T>`, and specification/projection primitives
- `MinimalCleanArch.DataAccess`: EF Core repositories, projection evaluation, and unit of work
- `MinimalCleanArch.Extensions`: Minimal API / Controller / FastEndpoints RFC 7807 ProblemDetails helpers, validation, OpenAPI, caching, and operational defaults
- `MinimalCleanArch.Validation`: FluentValidation integration and registration helpers
- `MinimalCleanArch.Security`: Data Protection & AES-256-GCM encryption services and encrypted EF property support
- `MinimalCleanArch.Storage`: blob/object storage abstractions with Azure Blob Storage and Cloudflare R2
- `MinimalCleanArch.Email`: email port with SMTP and HTTP API adapters
- `MinimalCleanArch.Jobs`: recurring + delayed job port (`IJobScheduler`) with hosted-service fallback
- `MinimalCleanArch.Realtime`: realtime publish port (`IRealtimePublisher`); SignalR adapter in Extensions
- `MinimalCleanArch.Features`: feature-flag port (`IFeatureGate`) with config backing
- `MinimalCleanArch.Messaging`: domain events and Wolverine integration
- `MinimalCleanArch.Audit`: audit interception (including DB-generated keys) and query support
- `MinimalCleanArch.Templates`: project scaffolding templates (`dotnet new mca`)

### Installation
```bash
dotnet add package MinimalCleanArch --version {{VERSION}}
dotnet add package MinimalCleanArch.DataAccess --version {{VERSION}}
dotnet add package MinimalCleanArch.Extensions --version {{VERSION}}
```

### Known Issues
- `Npgsql.EntityFrameworkCore.PostgreSQL` currently tracks `11.0.0-rc.3` until Npgsql publishes `11.0.0` GA.

See the README for full setup guidance and package-specific docs.
