# Context: Audit

**Boundary:** NuGet `MinimalCleanArch.Audit`.  
**Kind:** Infrastructure. Optional. Must be registered explicitly.  
**Database:** `AuditLog` table in the **same** consumer DbContext.  
**UI:** none in sample or template (query API exists as `IAuditLogService` only).

## What it owns

| Type | Path |
|---|---|
| `AuditLog`, `AuditOperation` | `src/MinimalCleanArch.Audit/Entities/AuditLog.cs` |
| `AuditSaveChangesInterceptor` | `Interceptors/AuditSaveChangesInterceptor.cs` |
| `AuditOptions` | `Configuration/AuditOptions.cs` |
| `IAuditContextProvider` | `Services/IAuditContextProvider.cs` |
| `ExecutionContextAuditContextProvider` | prefers `IExecutionContext` when registered |
| `HttpContextAuditContextProvider` | fallback |
| `IAuditLogService`, `AuditLogService`, `AuditLogQuery` | `Services/` |
| `AddAuditLogging`, `UseAuditInterceptor`, `UseAuditLog` | `Extensions/AuditExtensions.cs` |

## Aggregates

`AuditLog` is a table row, not an aggregate. Primary key `long Id`. Operations: Create, Update, Delete, SoftDelete, Restore.

## Commands / queries / hosts

No commands. Queries are `IAuditLogService` methods (`GetEntityHistoryAsync`, `GetByUserAsync`, `GetByTenantAsync`, `SearchAsync`, `PurgeAsync`). Neither sample nor template maps HTTP onto these methods.

## Integration

Wired next to the app DbContext:

1. `AddAuditLogging`
2. `options.UseAuditInterceptor(sp)`
3. `modelBuilder.UseAuditLog()` (sample only if `AuditOptions` is injected; template always when `--audit`)

Sample default: `Features:AuditLogging` true (not present in `appsettings.json`, code default).

## Honesty

- Shared database.
- `ProcessTemporaryPropertiesAsync` does nothing. Creates that still have a temporary primary key can miss an audit row.
- Not a domain event. It is an interceptor.
