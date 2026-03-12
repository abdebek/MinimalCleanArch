# Third-Party Notices

This file summarizes notable third-party dependencies surfaced by the MinimalCleanArch packages and templates.

It is intentionally high-level and not a complete legal inventory of every direct and transitive dependency shipped by every build output.

Use this as a visibility aid, not as legal advice. If you need compliance-grade reporting, generate a full dependency license inventory as part of CI/release.

## Core and Host Dependencies

| Dependency | Where it appears | Upstream | Observed license | Notes |
| :-- | :-- | :-- | :-- | :-- |
| FluentValidation | `MinimalCleanArch.Extensions`, `MinimalCleanArch.Validation`, templates | https://github.com/FluentValidation/FluentValidation | Apache-2.0 | Core validation library used for validator definitions and registration |
| FluentValidation.AspNetCore | `MinimalCleanArch.Extensions`, `MinimalCleanArch.Validation` | https://github.com/FluentValidation/FluentValidation.AspNetCore | Apache-2.0 | Upstream repo states this package is no longer maintained; this is primarily a maintenance concern rather than a separate license restriction |
| ASP.NET API Versioning (`Asp.Versioning.Http`) | `MinimalCleanArch.Extensions` | https://github.com/dotnet/aspnet-api-versioning | MIT | Used for Minimal API versioning support |
| Scalar | `MinimalCleanArch.Extensions`, templates | https://github.com/scalar/scalar | MIT | Used for API reference / client UI |
| Serilog.AspNetCore | `MinimalCleanArch.Extensions`, templates | https://github.com/serilog/serilog-aspnetcore | Apache-2.0 | Used for ASP.NET Core logging integration |
| Serilog.Sinks.File | `MinimalCleanArch.Extensions`, templates | https://github.com/serilog/serilog-sinks-file | Apache-2.0 | Used for file sink support |
| OpenTelemetry .NET | `MinimalCleanArch.Extensions`, templates | https://github.com/open-telemetry/opentelemetry-dotnet | Apache-2.0 | Used for tracing/metrics/logging integration |

## Persistence, Messaging, and Security Dependencies

| Dependency | Where it appears | Upstream | Observed license | Notes |
| :-- | :-- | :-- | :-- | :-- |
| Entity Framework Core | `MinimalCleanArch.DataAccess`, `MinimalCleanArch.Audit`, templates | https://github.com/dotnet/efcore | Apache-2.0 | Persistence layer foundation for repositories, DbContexts, and audit interception |
| Wolverine | `MinimalCleanArch.Messaging`, templates | https://github.com/JasperFx/wolverine | MIT | Used for domain-event handling, messaging, and outbox-capable workflows |
| OpenIddict | auth-enabled templates | https://github.com/openiddict/openiddict-core | Apache-2.0 | Used for OAuth2/OpenID Connect server and validation flows in generated auth-enabled apps |
| Microsoft ASP.NET Core Data Protection | `MinimalCleanArch.Security` | https://github.com/dotnet/aspnetcore | Apache-2.0 | Used for development-friendly and production-oriented encryption support |
| Azure Identity / Azure Data Protection Key Storage packages | `MinimalCleanArch.Security` | https://github.com/Azure/azure-sdk-for-net | MIT | Used when consumers choose Azure-backed key protection/storage |

## Practical Guidance

- The major third-party packages currently surfaced by MCA are predominantly Apache-2.0 or MIT.
- That is generally a low-friction licensing profile for commercial and internal use, but you still need to verify obligations for your own distribution model.
- The package READMEs mention the most important external technologies for architectural clarity; this file exists to make that external surface more explicit.
- Template consumers should also evaluate the licenses of any optional packages enabled by feature flags, especially in generated applications with auth, messaging, telemetry, or database provider extras.
