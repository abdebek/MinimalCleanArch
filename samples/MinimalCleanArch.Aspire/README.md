# MinimalCleanArch + .NET Aspire (spike)

Local orchestration spike for the MCA sample: **AppHost** runs Postgres, Redis, and the API with the Aspire dashboard (OTLP).

This is **not** a `dotnet new mca --aspire` template yet (Sprint 7). It validates connection naming, service defaults, and OTel before baking into the product template.

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

## Lessons for Sprint 7 (`--aspire` template)

1. AppHost + ServiceDefaults are outer composition only — Domain/Application stay unchanged.
2. Prefer named connection strings (`mca`, `redis`) over hand-built compose URLs.
3. `WaitFor` resources before starting the API.
4. Disable console OTel exporters when Aspire OTLP is configured.
5. Keep docker-compose and Aspire mutually exclusive in the template matrix.

## Build only

```bash
dotnet build samples/MinimalCleanArch.Aspire/MinimalCleanArch.AppHost
```
