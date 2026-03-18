# MinimalCleanArch

A Clean Architecture toolkit for Minimal APIs on .NET 9 and .NET 10, with vertical-slice-style application organization inside clean dependency boundaries.

## Quick Start

Default multi-project app:

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

Then open `https://localhost:<port>/scalar/v1`.

For auth + OpenIddict + Scalar password flow:

```bash
dotnet new mca -n QuickAuth --single-project --auth --tests --mcaVersion 0.1.19-preview
cd QuickAuth
dotnet run
```

Use the auth walkthrough in [`templates/README.md`](templates/README.md).
For template options, generated structure, and architecture details, see [`templates/README.md`](templates/README.md).

## Why Use It
- keep domain rules, repository contracts, and specifications separate from infrastructure concerns
- add EF Core persistence without pushing EF types into the domain layer
- bootstrap Minimal API applications with consistent validation, error handling, OpenAPI, rate limiting, and operational defaults
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
- Latest stable packages/templates: `0.1.19-preview`

## Local Validation
- Template validation uses two package sources by default: the local `MinimalCleanArch` feed and `nuget.org`.
- This is required because generated projects reference both `MinimalCleanArch.*` packages and pinned third-party packages.
- Use local-feed-only validation only if your feed mirrors every external dependency used by the templates.

## Preferred Integration Path
For new applications, the recommended order is:
1. Model entities, repository contracts, and specifications with `MinimalCleanArch`.
2. Add EF Core repositories and unit of work with `MinimalCleanArch.DataAccess`.
3. Add API bootstrap with `MinimalCleanArch.Extensions`.
4. Register application validators with `MinimalCleanArch.Validation`.
5. Add `MinimalCleanArch.Messaging`, `MinimalCleanArch.Audit`, `MinimalCleanArch.Security`, and `MinimalCleanArch.Storage` only when the app actually needs them.

Preferred defaults:
- use specifications through `IRepository<TEntity, TKey>`
- use `AddMinimalCleanArchApi(...)` as the main API bootstrap method
- use `AddValidationFromAssemblyContaining<T>()` for validator registration
- use `AddMinimalCleanArchMessaging...` extensions instead of wiring Wolverine from scratch
- use Data Protection-based encryption for new development
- use `IExecutionContext` as the shared source for user, tenant, and correlation data across HTTP and message-handler flows

## Dependency Direction
- `MinimalCleanArch` is the foundation. Other MCA packages can depend on it; your domain layer can depend on it.
- `MinimalCleanArch.DataAccess` depends on `MinimalCleanArch` and belongs in infrastructure.
- `MinimalCleanArch.Extensions` depends on `MinimalCleanArch` and belongs in the API/host layer.
- `MinimalCleanArch.Validation` depends on `MinimalCleanArch` and `MinimalCleanArch.Extensions`; use it where API validation registration happens.
- `MinimalCleanArch.Messaging` and `MinimalCleanArch.Audit` depend on `MinimalCleanArch` and are optional infrastructure/application-host add-ons.
- `MinimalCleanArch.Security` is an optional infrastructure package for encryption concerns.
- `MinimalCleanArch.Storage` is an optional infrastructure package for blob/object storage concerns.
- Domain projects should not reference `DataAccess`, `Extensions`, `Validation`, `Messaging`, `Audit`, `Security`, or `Storage`.

## Packages
| Package | Helps achieve | Depends on | Typical layer |
| :-- | :-- | :-- | :-- |
| [`MinimalCleanArch`](src/MinimalCleanArch/README.md) | domain model, contracts, specifications, result types | none | Domain |
| [`MinimalCleanArch.DataAccess`](src/MinimalCleanArch.DataAccess/README.md) | EF Core repositories, unit of work, audited DbContext base types | `MinimalCleanArch` | Infrastructure |
| [`MinimalCleanArch.Extensions`](src/MinimalCleanArch.Extensions/README.md) | API bootstrap, validation pipeline, error mapping, OpenAPI, rate limiting | `MinimalCleanArch` | API/Host |
| [`MinimalCleanArch.Validation`](src/MinimalCleanArch.Validation/README.md) | validator registration and API validation integration | `MinimalCleanArch`, `MinimalCleanArch.Extensions` | API/Host or composition root |
| [`MinimalCleanArch.Security`](src/MinimalCleanArch.Security/README.md) | encryption services and encrypted EF property support | no MCA package dependency | Infrastructure |
| [`MinimalCleanArch.Storage`](src/MinimalCleanArch.Storage/README.md) | blob/object storage abstraction with Azure Blob Storage integration | no MCA package dependency | Infrastructure |
| [`MinimalCleanArch.Messaging`](src/MinimalCleanArch.Messaging/README.md) | domain events, Wolverine integration, outbox-capable messaging | `MinimalCleanArch` | Infrastructure or host |
| [`MinimalCleanArch.Audit`](src/MinimalCleanArch.Audit/README.md) | audit interception, audit storage, audit queries | `MinimalCleanArch` | Infrastructure |
| [`MinimalCleanArch.Templates`](templates/README.md) | scaffold new MCA-based applications | packaged templates | Project scaffolding |

Additional docs:
- Sample app: [`samples/MinimalCleanArch.Sample/README.md`](samples/MinimalCleanArch.Sample/README.md)
- Release notes template: [`release-notes.md`](release-notes.md)
- Third-party notices: [`THIRD_PARTY_NOTICES.md`](THIRD_PARTY_NOTICES.md)

## Contributing
Contributions are welcome. See [CONTRIBUTING.md](CONTRIBUTING.md).

## License
MIT. See [LICENSE](LICENSE).


