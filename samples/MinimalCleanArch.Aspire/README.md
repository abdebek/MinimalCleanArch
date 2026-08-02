# MinimalCleanArch + .NET Aspire (sample)

Local orchestration sample for the MCA API: **AppHost** runs Postgres, Redis, and the API with the Aspire dashboard (OTLP).

The `--aspire` template flag is now available (`dotnet new mca --aspire ...`) and generates an AppHost + ServiceDefaults for new projects. **This sample** is the reference implementation the template was derived from — it stays in the repo to validate Aspire integration against the live MCA packages without a scaffolding step.

## Prerequisites

- .NET 10 SDK
- Docker (for Postgres + Redis containers)

## Projects

| Project | Role |
|---------|------|
| `MinimalCleanArch.AppHost` | Aspire orchestrator |
| `MinimalCleanArch.ServiceDefaults` | Shared OTel, health, resilience |
| `../MinimalCleanArch.Sample` | Existing MCA sample API (referenced by AppHost) |

## Run

```bash
# From repo root
dotnet run --project samples/MinimalCleanArch.Aspire/MinimalCleanArch.AppHost
```

Aspire opens the dashboard. Open the **api** resource HTTP endpoint (or Scalar under `/scalar/v1`).

### Without Aspire (still works)

```bash
dotnet run --project samples/MinimalCleanArch.Sample
```

Uses SQLite (`ConnectionStrings:DefaultConnection`) and in-memory cache as before.

## Connection names

AppHost injects:

| Resource | Connection string name | Sample behavior |
|----------|------------------------|-----------------|
| Postgres database | `mca` | EF Core uses **Npgsql** when this (or a Postgres-shaped) connection string is present |
| Redis | `redis` | Registers **distributed Redis cache** when present; otherwise in-memory MCA cache |

Fallback for local non-Aspire runs:

```json
"ConnectionStrings": {
  "DefaultConnection": "Data Source=todos.db"
}
```

## OpenTelemetry

- Under Aspire, `OTEL_EXPORTER_OTLP_ENDPOINT` is set → **ServiceDefaults** exports traces/metrics/logs to the dashboard.
- Direct sample runs: MCA `AddMinimalCleanArchTelemetry` may still emit console exporters in Development.
- Prefer **one** OTel path when both are active: when OTLP is present, the sample skips MCA console/OTel double-wiring.

## Messaging note

The sample keeps **in-memory Wolverine** even under Aspire for this spike. Durable outbox against the Aspire Postgres connection is a follow-up (`AddMinimalCleanArchMessagingWithPostgres` using the `mca` connection string).

## Generating a new Aspire project

To scaffold a new MCA app with Aspire orchestration instead of using this sample:

```bash
dotnet new mca -n MyApp --aspire --recommended --db postgres
```

See the template README (`templates/README.md`) for the full `--aspire` flag matrix (Postgres/SQL Server, optional Redis, mutual exclusion with `--docker`).

## Build only

```bash
dotnet build samples/MinimalCleanArch.Aspire/MinimalCleanArch.AppHost
```
