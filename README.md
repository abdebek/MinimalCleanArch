# MinimalCleanArch

A Clean Architecture toolkit for Minimal APIs on .NET 9 and .NET 10.

## Core Features
- Domain building blocks (entities, repositories, unit of work, specifications, result pattern)
- Minimal API helpers (validation wiring, standardized error handling, OpenAPI + Scalar)
- Security and encryption (Data Protection/AES column encryption)
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
- Project scaffolding: `MinimalCleanArch.Templates`

## Versions
- Stable packages/templates: `0.1.14`
- Next preview line: `0.1.16-preview`

## Local Validation
- Template validation uses two package sources by default: the local `MinimalCleanArch` feed and `nuget.org`.
- This is required because generated projects reference both `MinimalCleanArch.*` packages and pinned third-party packages.
- Use local-feed-only validation only if your feed mirrors every external dependency used by the templates.

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
dotnet new mca -n QuickAuth --single-project --auth --tests --mcaVersion 0.1.16-preview
cd QuickAuth
dotnet run
```

Use the auth walkthrough in [`templates/README.md`](templates/README.md).

## Preferred Integration Path
For new applications, the recommended order is:
1. Model entities, repository contracts, and specifications with `MinimalCleanArch`.
2. Add EF Core repositories and unit of work with `MinimalCleanArch.DataAccess`.
3. Add API bootstrap with `MinimalCleanArch.Extensions`.
4. Register application validators with `MinimalCleanArch.Validation`.
5. Add `MinimalCleanArch.Messaging`, `MinimalCleanArch.Audit`, and `MinimalCleanArch.Security` only when the app actually needs them.

Preferred defaults:
- use specifications through `IRepository<TEntity, TKey>`
- use `AddMinimalCleanArchApi(...)` as the main API bootstrap method
- use `AddValidationFromAssemblyContaining<T>()` for validator registration
- use `AddMinimalCleanArchMessaging...` extensions instead of wiring Wolverine from scratch
- use Data Protection-based encryption for new development
- use `IExecutionContext` as the shared source for user, tenant, and correlation data across HTTP and message-handler flows

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
- Generated app architecture: [`templates/README.md#architecture-overview`](templates/README.md#architecture-overview)
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

