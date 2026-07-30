# Package management

## Repo (libraries, sample, tests)

Central Package Management is enabled for **repo projects only** via:

- shared versions: `eng/Directory.Packages.props`
- imported from: `src/Directory.Packages.props`, `tests/Directory.Packages.props`, `samples/Directory.Packages.props`

This keeps CPM **out of** `temp/` template smoke builds and other trees that must use explicit `Version=` pins.

- **Versions live in** `eng/Directory.Packages.props`
- **Project files** use versionless `PackageReference` entries
- **Multi-targeting** (`net9.0` / `net10.0`) uses `VersionOverride` on TFM-conditioned package groups when the net9 line must differ from the central net10 default
- **Transitive pinning** is off (`CentralPackageTransitivePinningEnabled=false`) so net9 graphs are not forced onto net10 package versions

```xml
<!-- Directory.Packages.props -->
<PackageVersion Include="Microsoft.EntityFrameworkCore" Version="10.0.3" />

<!-- src project -->
<PackageReference Include="Microsoft.EntityFrameworkCore" /> <!-- uses 10.0.3 -->
<PackageReference Include="Microsoft.EntityFrameworkCore" VersionOverride="9.0.5"
                  Condition="'$(TargetFramework)' == 'net9.0'" />
```

Build tooling packages (`MinVer`, `SourceLink`) are referenced from `Directory.Build.props` and versioned centrally.

## Templates (generated apps)

Scaffolded projects **do not** rely on this repo’s `Directory.Packages.props`. They keep **explicit** `Version=` attributes so `dotnet new mca` apps restore cleanly on any machine.

When bumping shared dependencies (EF, HealthChecks, Wolverine, OpenTelemetry, etc.), update:

1. `Directory.Packages.props`
2. Matching pins under `templates/mca/**/*.csproj`

Template packaging under `templates/` sets `ManagePackageVersionsCentrally=false` so the template nupkg project is unaffected by CPM.

## FluentValidation

- Use **FluentValidation 12.x** + **FluentValidation.DependencyInjectionExtensions**
- **FluentValidation.AspNetCore** was removed from library packages; MCA registers validators via `AddMinimalCleanArchApi` / `AddValidatorsFromAssembly*` and endpoint filters (`WithValidation` / `ValidateAsync`)

## Wolverine / Roslyn

WolverineFx `5.15.0` is pinned centrally. `Microsoft.CodeAnalysis.Common` / `Workspaces.Common` `5.0.0` are pinned next to it in `MinimalCleanArch.Messaging` to keep the dependency graph aligned. Residual `NU1608` is still suppressed on the messaging project if the graph reports unavoidable version range noise.

## Health checks

Generated templates use **AspNetCore.HealthChecks.\*** **9.0.0** (Xabaril) for net9/net10 compatibility (replacing the previous 8.x pins).

## Versioning note

| Channel | Meaning |
|---------|---------|
| `0.1.x-preview` | Pre-release packages/templates; default template `mcaVersion` tracks the latest preview used in this repo |
| stable (no `-preview`) | Intended for consumers that pin non-preview feeds; publish cadence is independent of this doc |

Always pass `--mcaVersion` when validating against a local feed:

```bash
dotnet new mca -n App --mcaVersion 0.1.20-preview
```

## Future (not implemented)

Splitting `MinimalCleanArch.Extensions` into Core / Telemetry / OpenApi packages would shrink transitive graphs for apps that only need ProblemDetails/validation. Defer until consumer demand outweighs package-surface cost.
