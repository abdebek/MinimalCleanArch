# MinimalCleanArch

A Clean Architecture **kernel** with host adapters and optional client surfaces. ASP.NET Minimal APIs on .NET 9 and .NET 10 are the first host adapter.

**Navigate the repo:** start at [`docs/index.md`](docs/index.md). Product layers: [`docs/07-bootstrap-product.md`](docs/07-bootstrap-product.md).

## Supported teaching path

The supported way to start a real app is the generated template with `--recommended --auth` (multi-project, filesystem `src/`):

```bash
dotnet new install MinimalCleanArch.Templates
dotnet new mca -n MyApp --recommended --auth
cd MyApp
dotnet run --project src/MyApp.Api
```

Then open `https://localhost:<port>/scalar/v1`. Use the auth walkthrough in [`templates/README.md`](templates/README.md).

`samples/MinimalCleanArch.Sample` is a package smoke demo. Do not copy its endpoint-writes-the-aggregate Todo path when generating a product; generated apps use handlers.

Minimal API-only (`dotnet new mca -n MyApp`) and `--single-project` remain valid shapes. They are not the teaching default.

For flags and generated structure, see [`templates/README.md`](templates/README.md).

## Why Use It
- keep domain rules, repository contracts, and specifications separate from infrastructure concerns
- add EF Core persistence without pushing EF types into the domain layer
- bootstrap Minimal API applications with consistent validation, error handling, OpenAPI, rate limiting, optional blob storage (`--storage`), and operational defaults
- opt into messaging, audit logging, and encryption only when the application actually needs them
- scaffold new applications with a package set that already follows the intended dependency direction

## Architectural Style
- project and package boundaries follow Clean Architecture dependency direction
- application use cases are organized in a vertical-slice/CQRS-friendly style rather than around large layered service classes
- the intent is not “pure vertical slices with no shared layers”; it is clean boundaries plus feature-oriented handlers and endpoints

## Core Features
- Domain building blocks (entities, repositories, unit of work, specifications, result pattern)
- Minimal API helpers (validation wiring, standardized error handling, OpenAPI + Scalar)
- Security and encryption (Data Protection/AES column encryption)
- Blob/object storage abstractions with Azure Blob Storage support
- Soft delete and auditing support
- EF Core integration with specification evaluation

## Recommended Package Sets
- Core domain and repository abstractions: `MinimalCleanArch`
- EF Core repositories and specifications: `MinimalCleanArch.DataAccess`
- Minimal API bootstrap, error mapping, OpenAPI, rate limiting: `MinimalCleanArch.Extensions`
- FluentValidation registration: `MinimalCleanArch.Validation`
- Domain events and Wolverine integration: `MinimalCleanArch.Messaging`
- Audit interception and audit queries: `MinimalCleanArch.Audit`
- Encrypted EF properties and encryption services: `MinimalCleanArch.Security`
- Generic blob storage abstraction and Azure Blob Storage integration: `MinimalCleanArch.Storage`
- Project scaffolding: `MinimalCleanArch.Templates`

## Versions
- Latest packages/templates: `0.1.20-preview`

## Local Validation
```bash
./scripts/pack.sh --package-version 0.1.20-preview
./scripts/validate-templates.sh -McaVersion 0.1.20-preview   # or: ./scripts/validate-templates.ps1
./scripts/clean-temp.sh                                      # reclaim temp/MCA_Tests + temp/validate
```
- Wrappers live under `scripts/`; implementation is `templates/scripts/validate-templates.ps1`.
- Scaffolds multi/single variants (including **Aspire AppHost** builds by default; `-SkipAspire` to omit).
- Successful validation **deletes** its `temp/validate/<run>/` tree (use `-KeepOutput` to keep). Template tests clean `temp/MCA_Tests/<id>/` after each test unless `MCA_KEEP_TEMPLATE_OUTPUT=1`.
- Uses two package sources by default: the local `MinimalCleanArch` feed and `nuget.org`.
- Use local-feed-only validation only if your feed mirrors every external dependency used by the templates.

## Package management
- Repo libraries/sample/tests use **Central Package Management** (`eng/Directory.Packages.props`).
- Generated templates keep explicit package versions (no CPM dependency).
- See [`docs/package-management.md`](docs/package-management.md).

## Aspire
- **Template:** `dotnet new mca -n MyApp --recommended --aspire --db postgres`
- **Run:** `./scripts/run-apphost.sh` / `pwsh ./scripts/run-apphost.ps1` (or `dotnet run --project MyApp.AppHost`)
- Connection names: `appdb` (Postgres/SQL Server), `redis` (when caching is enabled)
- **Sample spike:** [`samples/MinimalCleanArch.Aspire/README.md`](samples/MinimalCleanArch.Aspire/README.md) (sample still uses name `mca`)
- Direct sample (SQLite, no containers): `dotnet run --project samples/MinimalCleanArch.Sample`

## Preferred Integration Path
For new applications, the recommended order is:
1. Model entities, repository contracts, and specifications with `MinimalCleanArch`.
2. Add EF Core repositories and unit of work with `MinimalCleanArch.DataAccess`.
3. Add API bootstrap with `MinimalCleanArch.Extensions`.
4. Register application validators with `MinimalCleanArch.Validation`.
5. Add `MinimalCleanArch.Messaging`, `MinimalCleanArch.Jobs`, `MinimalCleanArch.Realtime`, `MinimalCleanArch.Features`, `MinimalCleanArch.Audit`, `MinimalCleanArch.Security`, and `MinimalCleanArch.Storage` only when the app actually needs them.

Preferred defaults:
- use specifications through `IRepository<TEntity, TKey>`
- use `AddMinimalCleanArchApi(...)` as the main API bootstrap method (validators + rate limiting + problem details)
- use `UseMinimalCleanArchApiDefaults(...)` for the standard middleware pipeline (correlation ID, security headers, error handling, optional rate limiting)
- prefer `options.AddValidatorsFromAssemblyContaining<T>()` on `AddMinimalCleanArchApi` over separate validator registration calls
- map endpoint outcomes with `result.MatchHttp(...)` / `error.ToProblem(...)` (RFC 7807), not plain string bodies
- validate handler-built commands/queries with `httpContext.ValidateAsync(...)` (or `WithValidation<T>()` for body parameters)
- use `AddMinimalCleanArchMessaging...` extensions instead of wiring Wolverine from scratch
- use Data Protection-based encryption for new development
- use `IExecutionContext` as the shared source for user, tenant, and correlation data across HTTP and message-handler flows

The sample app and generated templates follow this bootstrap and HTTP mapping path when API polish features are enabled.

## Dependency Direction
- `MinimalCleanArch` is the foundation. Other MCA packages can depend on it; your domain layer can depend on it.
- `MinimalCleanArch.DataAccess` depends on `MinimalCleanArch` and belongs in infrastructure.
- `MinimalCleanArch.Extensions` depends on `MinimalCleanArch` and belongs in the API/host layer.
- `MinimalCleanArch.Validation` depends on `MinimalCleanArch` and `MinimalCleanArch.Extensions`; use it where API validation registration happens.
- `MinimalCleanArch.Messaging` and `MinimalCleanArch.Audit` depend on `MinimalCleanArch` and are optional infrastructure/application-host add-ons.
- `MinimalCleanArch.Security` is an optional infrastructure package for encryption concerns.
- `MinimalCleanArch.Storage` is an optional infrastructure package for blob/object storage concerns.
- `MinimalCleanArch.Email` is an optional infrastructure package for SMTP / HTTP email.
- `MinimalCleanArch.Jobs` is an optional scheduling port (recurring + delayed) with an `IHostedService` fallback.
- `MinimalCleanArch.Realtime` is an optional publish port (channel, payload, tenant/user). SignalR lives in Extensions.
- `MinimalCleanArch.Features` is an optional feature-flag port (`IFeatureGate`). HTTP `RequireFeature` lives in Extensions.
- Domain projects should not reference `DataAccess`, `Extensions`, `Validation`, `Messaging`, `Audit`, `Security`, `Storage`, `Email`, `Jobs`, `Realtime`, or `Features`.

## Packages
| Package | Helps achieve | Depends on | Typical layer |
| :-- | :-- | :-- | :-- |
| [`MinimalCleanArch`](src/MinimalCleanArch/README.md) | domain model, contracts, specifications, result types | none | Domain |
| [`MinimalCleanArch.DataAccess`](src/MinimalCleanArch.DataAccess/README.md) | EF Core repositories, unit of work, audited DbContext base types | `MinimalCleanArch` | Infrastructure |
| [`MinimalCleanArch.Extensions`](src/MinimalCleanArch.Extensions/README.md) | API bootstrap, validation pipeline, error mapping, OpenAPI, rate limiting, SignalR realtime adapter, feature-gate filter | `MinimalCleanArch`, `MinimalCleanArch.Realtime`, `MinimalCleanArch.Features` | API/Host |
| [`MinimalCleanArch.Validation`](src/MinimalCleanArch.Validation/README.md) | validator registration and API validation integration | `MinimalCleanArch`, `MinimalCleanArch.Extensions` | API/Host or composition root |
| [`MinimalCleanArch.Security`](src/MinimalCleanArch.Security/README.md) | encryption services and encrypted EF property support | no MCA package dependency | Infrastructure |
| [`MinimalCleanArch.Storage`](src/MinimalCleanArch.Storage/README.md) | blob/object storage abstraction with Azure Blob Storage integration | no MCA package dependency | Infrastructure |
| [`MinimalCleanArch.Email`](src/MinimalCleanArch.Email/README.md) | email port with SMTP and HTTP API adapters | no MCA package dependency | Infrastructure |
| [`MinimalCleanArch.Jobs`](src/MinimalCleanArch.Jobs/README.md) | recurring + delayed job port (`IJobScheduler`) | no MCA package dependency | Application / host |
| [`MinimalCleanArch.Realtime`](src/MinimalCleanArch.Realtime/README.md) | realtime publish port (`IRealtimePublisher`) | no MCA package dependency | Application / host |
| [`MinimalCleanArch.Features`](src/MinimalCleanArch.Features/README.md) | feature-flag port (`IFeatureGate`) | no MCA package dependency | Application / host |
| [`MinimalCleanArch.Messaging`](src/MinimalCleanArch.Messaging/README.md) | domain events, Wolverine integration, outbox-capable messaging | `MinimalCleanArch`, `MinimalCleanArch.Jobs` | Infrastructure or host |
| [`MinimalCleanArch.Audit`](src/MinimalCleanArch.Audit/README.md) | audit interception, audit storage, audit queries | `MinimalCleanArch` | Infrastructure |
| [`MinimalCleanArch.Templates`](templates/README.md) | scaffold new MCA-based applications | packaged templates | Project scaffolding |

Additional docs:
- Repo navigation (DDD-style map of the system as built): [`docs/index.md`](docs/index.md)
- Sample app: [`samples/MinimalCleanArch.Sample/README.md`](samples/MinimalCleanArch.Sample/README.md)
- Package management: [`docs/package-management.md`](docs/package-management.md)
- Release notes template: [`release-notes.md`](release-notes.md)
- Third-party notices: [`THIRD_PARTY_NOTICES.md`](THIRD_PARTY_NOTICES.md)

## Contributing
Contributions are welcome. See [CONTRIBUTING.md](CONTRIBUTING.md).

## License
MIT. See [LICENSE](LICENSE).


