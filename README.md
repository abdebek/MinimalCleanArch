# MinimalCleanArch

A comprehensive library for implementing Clean Architecture with Minimal APIs on .NET 9. It provides domain foundations, repositories, unit of work, specifications, security/encryption, and minimal-API extensions.

## Core Features
- Clean Architecture foundations (entities, repositories, unit of work, specifications, result pattern)
- Minimal API extensions (validation wiring, standardized error handling, OpenAPI helpers)
- Security & encryption (Data Protection/AES column encryption)
- Soft delete & auditing (IsDeleted + auditing timestamps/users)
- EF Core integration (repositories, unit of work, auditing/soft-delete filters)

## Version & Templates
- Current package version: `0.1.8-preview` (targets .NET 9).
- Templates: `dotnet new install MinimalCleanArch.Templates` (or local nupkg), then `dotnet new mca -n MyApp` (multi-project) or `--single-project`.
- Launch defaults: Swagger, randomized ports 5000–8000; adjust `Properties/launchSettings.json` if needed.
- Using local nupkgs? Add a `nuget.config` with your local feed (e.g., `artifacts/nuget`) before restoring.

## Packages
| Package | Description |
| :-- | :-- |
| [`MinimalCleanArch`](src/MinimalCleanArch/README.md) | Core interfaces and base classes (Entities, Repositories, Specifications, Result pattern). |
| [`MinimalCleanArch.DataAccess`](src/MinimalCleanArch.DataAccess/README.md) | EF Core implementation (DbContextBase, Repository, UnitOfWork, SpecificationEvaluator). |
| [`MinimalCleanArch.Extensions`](src/MinimalCleanArch.Extensions/README.md) | Minimal API enhancements (validation filters, error handling, standard responses). |
| [`MinimalCleanArch.Validation`](src/MinimalCleanArch.Validation/README.md) | FluentValidation integration components (often used via Extensions). |
| [`MinimalCleanArch.Security`](src/MinimalCleanArch.Security/README.md) | Data encryption services (AES, Data Protection) and EF Core integration. |
| [`MinimalCleanArch.Messaging`](src/MinimalCleanArch.Messaging/README.md) | Domain events/messaging helpers and Wolverine integration. |
| [`MinimalCleanArch.Audit`](src/MinimalCleanArch.Audit/README.md) | Audit logging components. |
| [`MinimalCleanArch.Templates`](templates/README.md) | `dotnet new mca` templates (single- or multi-project, clean architecture). |

## Quick Start (short)
- Scaffold with the template:
  ```bash
  dotnet new install MinimalCleanArch.Templates
  dotnet new mca -n MyApp              # multi-project
  dotnet new mca -n MyApp --single-project
  ```
- Or install packages directly:
  ```bash
  dotnet add package MinimalCleanArch
  dotnet add package MinimalCleanArch.DataAccess
  dotnet add package MinimalCleanArch.Extensions
  dotnet add package MinimalCleanArch.Security
  dotnet add package MinimalCleanArch.Validation
  dotnet add package MinimalCleanArch.Messaging
  dotnet add package MinimalCleanArch.Audit
  ```
- Then follow the per-package guides (links above) for setup specifics.

## Documentation map
- Core: [`src/MinimalCleanArch/README.md`](src/MinimalCleanArch/README.md)
- DataAccess: [`src/MinimalCleanArch.DataAccess/README.md`](src/MinimalCleanArch.DataAccess/README.md)
- Extensions: [`src/MinimalCleanArch.Extensions/README.md`](src/MinimalCleanArch.Extensions/README.md)
- Validation: [`src/MinimalCleanArch.Validation/README.md`](src/MinimalCleanArch.Validation/README.md)
- Security: [`src/MinimalCleanArch.Security/README.md`](src/MinimalCleanArch.Security/README.md)
- Messaging: [`src/MinimalCleanArch.Messaging/README.md`](src/MinimalCleanArch.Messaging/README.md)
- Audit: [`src/MinimalCleanArch.Audit/README.md`](src/MinimalCleanArch.Audit/README.md)
- Templates: [`templates/README.md`](templates/README.md)
- Sample app: [`samples/MinimalCleanArch.Sample/README.md`](samples/MinimalCleanArch.Sample/README.md)

## Sample Application
- See [`samples/MinimalCleanArch.Sample/README.md`](samples/MinimalCleanArch.Sample/README.md).

## Contributing
Contributions are welcome! Please read [CONTRIBUTING.md](CONTRIBUTING.md).

## License
MIT License. See [LICENSE](LICENSE).
