# MinimalCleanArch

A Clean Architecture toolkit for Minimal APIs on .NET 9 and .NET 10.

## Core Features
- Domain building blocks (entities, repositories, unit of work, specifications, result pattern)
- Minimal API helpers (validation wiring, standardized error handling, OpenAPI + Scalar)
- Security and encryption (Data Protection/AES column encryption)
- Soft delete and auditing support
- EF Core integration with specification evaluation

## Versions
- Stable packages/templates: `0.1.14`
- Next preview line: `0.1.15-preview`

## Try It Fast

```bash
dotnet new install MinimalCleanArch.Templates
dotnet new mca -n QuickStart --single-project --recommended
cd QuickStart
dotnet run
```

Then open `https://localhost:<port>/scalar/v1`.

For auth + OpenIddict + Scalar password flow:

```bash
dotnet new mca -n QuickAuth --single-project --auth --tests --mcaVersion 0.1.14
cd QuickAuth
dotnet run
```

Use the auth walkthrough in [`templates/README.md`](templates/README.md).

## Packages
| Package | Description |
| :-- | :-- |
| [`MinimalCleanArch`](src/MinimalCleanArch/README.md) | Core abstractions and domain primitives. |
| [`MinimalCleanArch.DataAccess`](src/MinimalCleanArch.DataAccess/README.md) | EF Core repository/unit of work implementation. |
| [`MinimalCleanArch.Extensions`](src/MinimalCleanArch.Extensions/README.md) | Minimal API extensions (validation, errors, responses). |
| [`MinimalCleanArch.Validation`](src/MinimalCleanArch.Validation/README.md) | FluentValidation integration components. |
| [`MinimalCleanArch.Security`](src/MinimalCleanArch.Security/README.md) | Encryption and security utilities. |
| [`MinimalCleanArch.Messaging`](src/MinimalCleanArch.Messaging/README.md) | Messaging/domain event helpers. |
| [`MinimalCleanArch.Audit`](src/MinimalCleanArch.Audit/README.md) | Audit logging components. |
| [`MinimalCleanArch.Templates`](templates/README.md) | `dotnet new mca` templates. |

## Documentation Map
- Templates: [`templates/README.md`](templates/README.md)
- Sample app: [`samples/MinimalCleanArch.Sample/README.md`](samples/MinimalCleanArch.Sample/README.md)
- Core: [`src/MinimalCleanArch/README.md`](src/MinimalCleanArch/README.md)
- DataAccess: [`src/MinimalCleanArch.DataAccess/README.md`](src/MinimalCleanArch.DataAccess/README.md)
- Extensions: [`src/MinimalCleanArch.Extensions/README.md`](src/MinimalCleanArch.Extensions/README.md)
- Validation: [`src/MinimalCleanArch.Validation/README.md`](src/MinimalCleanArch.Validation/README.md)
- Security: [`src/MinimalCleanArch.Security/README.md`](src/MinimalCleanArch.Security/README.md)
- Messaging: [`src/MinimalCleanArch.Messaging/README.md`](src/MinimalCleanArch.Messaging/README.md)
- Audit: [`src/MinimalCleanArch.Audit/README.md`](src/MinimalCleanArch.Audit/README.md)

## Contributing
Contributions are welcome. See [CONTRIBUTING.md](CONTRIBUTING.md).

## License
MIT. See [LICENSE](LICENSE).

