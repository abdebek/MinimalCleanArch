# MinimalCleanArch.Audit

Audit logging components for MinimalCleanArch.

## Version
- Current stable: 0.1.18 (net9.0, net10.0).

## Why Use It
- persist entity change history and audit metadata without duplicating audit plumbing in each EF Core DbContext
- query audit history by user, tenant, correlation ID, and other operational criteria
- bridge MCA execution-context data into audit logging automatically when available

## When to Use It
- use it when the application needs traceability, compliance history, or administrative audit views
- keep it in infrastructure, near EF Core and persistence concerns
- skip it in small applications where audit storage is unnecessary overhead

## Dependency Direction
- Depends on: `MinimalCleanArch`
- Typically referenced by: infrastructure projects
- Used by: API/host projects indirectly through infrastructure registration
- Do not reference from: domain projects

## What's included
- Audit logging services and helpers.
- DI extensions to plug audit logging into your MinimalCleanArch app.
- Tenant-aware audit context support through `IAuditContextProvider`.
- Audit query service support for user, tenant, correlation ID, and flexible search queries.
- default audit-context bridging from `IExecutionContext` when available

## Usage
```bash
dotnet add package MinimalCleanArch.Audit --version 0.1.18
```

Register services:

```csharp
builder.Services.AddAuditLogging();
builder.Services.AddAuditLogService<AppDbContext>();
```

If `IExecutionContext` is registered, `AddAuditLogging()` uses it automatically. Existing `IAuditContextProvider` customizations still work.

Configure the DbContext:

```csharp
builder.Services.AddDbContext<AppDbContext>((sp, options) =>
{
    options.UseSqlite("Data Source=app.db");
    options.UseAuditInterceptor(sp);
});
```

Configure the audit model:

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    base.OnModelCreating(modelBuilder);
    modelBuilder.UseAuditLog();
}
```

Implement a custom context provider when you need tenant-aware auditing:

```csharp
public sealed class AppAuditContextProvider : IAuditContextProvider
{
    public string? GetUserId() => "...";
    public string? GetUserName() => "...";
    public string? GetTenantId() => "...";
    public string? GetCorrelationId() => "...";
    public string? GetClientIpAddress() => "...";
    public string? GetUserAgent() => "...";
    public IDictionary<string, object>? GetMetadata() => null;
}
```

Notes:
- `TenantId` is a first-class field on `AuditLog`, so tenant filtering does not need to be pushed into metadata.
- `IAuditLogService` supports tenant-aware queries in addition to user and correlation-based lookups.
- `IAuditContextProvider.GetTenantId()` now defaults to `null`, so existing custom providers do not need to implement tenant support immediately.
- When using a local feed, add a `nuget.config` pointing to your local packages folder and keep `nuget.org` available unless your feed mirrors all external dependencies.


