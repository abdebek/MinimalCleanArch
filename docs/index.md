# MinimalCleanArch docs

This folder describes the repository as the code exists today and the product it is becoming. MinimalCleanArch is a Clean Architecture **kernel** with host adapters and optional client surfaces. The **supported teaching path** is `dotnet new mca --recommended --auth` (see [07. Bootstrap product](07-bootstrap-product.md)). The in-repo sample is a package smoke demo, not that path.

## How to use these docs

Start with the question you have, then open one page.

| If you need to know | Open |
|---|---|
| What this repo is and how pieces relate | [01. System overview](01-system-overview.md) |
| How a request, save, and composition actually run | [02. Architecture](02-architecture.md) |
| What a word means in this codebase | [03. Ubiquitous language](03-ubiquitous-language.md) |
| Where to change a feature | [04. How to find things](04-how-to-find-things.md) |
| Todo and auth state machines | [05. Lifecycles](05-lifecycles.md) |
| How events, outbox, and audit fire | [06. Events and side effects](06-events-and-side-effects.md) |
| Kernel vs host adapter vs client | [07. Bootstrap product](07-bootstrap-product.md) |
| How CI validates and publishes packages | [07. Bootstrap product — CI](07-bootstrap-product.md#ci-validate-and-publish) |
| Package versions and CPM | [Package management](package-management.md) |

Bounded context pages (capability modules plus the two consumer domains):

| Context | Page |
|---|---|
| Toolkit core primitives | [contexts/toolkit-core.md](contexts/toolkit-core.md) |
| Persistence (EF) | [contexts/persistence.md](contexts/persistence.md) |
| HTTP host | [contexts/http-host.md](contexts/http-host.md) |
| Todo | [contexts/todo.md](contexts/todo.md) |
| Identity and auth | [contexts/identity.md](contexts/identity.md) |
| Messaging | [contexts/messaging.md](contexts/messaging.md) |
| Audit | [contexts/audit.md](contexts/audit.md) |
| Security (encryption) | [contexts/security.md](contexts/security.md) |
| Blob storage | [contexts/storage.md](contexts/storage.md) |

Package READMEs under `src/*/README.md` and `templates/README.md` stay the consumer facing usage guides. This folder is the navigation map for engineers working in the repo.

## What this is not

These pages describe the code as it exists. They also name the bootstrap product direction on [07. Bootstrap product](07-bootstrap-product.md): kernel, host adapters, client surfaces. That page is the redesign statement; the rest of this folder stays a map of today’s packages, sample, and template.

If a README and the source disagree, the source wins.

This is DDD inspired, not strict DDD. Shared databases, generic repositories, and anemic Identity models are documented as they are.

## Diagrams

Sources live in `docs/diagrams/*.d2`. Regenerate with:

```bash
d2 --theme=0 --layout=elk --pad=40 docs/diagrams/context-map.d2 docs/diagrams/context-map.svg
d2 --theme=0 --layout=elk --pad=40 docs/diagrams/request-path.d2 docs/diagrams/request-path.svg
d2 --theme=0 --layout=elk --pad=40 docs/diagrams/aggregate-graph.d2 docs/diagrams/aggregate-graph.svg
d2 --theme=0 --layout=elk --pad=40 docs/diagrams/lifecycle.d2 docs/diagrams/lifecycle.svg
d2 --theme=0 --layout=elk --pad=40 docs/diagrams/event-path.d2 docs/diagrams/event-path.svg
d2 --theme=0 --layout=elk --pad=40 docs/diagrams/surfaces.d2 docs/diagrams/surfaces.svg
```

## Leftover, not live

| Path | What it is |
|---|---|
| `docs/MinimalCleanArch.Docs/` | DocFX project whose `Program.cs` prints `Hello, World!`. Not the live documentation host. Ignore its `bin/` and `obj/`. |
| `src/*/bin`, `src/*/obj` | Build output. |
| `temp/` | Template validation scratch. Deleted after successful runs. |
| `artifacts/packages` | Packed nupkgs from `scripts/pack.sh`. |

## Verification basis

These docs were written from the packages under `src/`, the sample under `samples/MinimalCleanArch.Sample/`, the Aspire host under `samples/MinimalCleanArch.Aspire/`, and the template sources under `templates/mca/`.
