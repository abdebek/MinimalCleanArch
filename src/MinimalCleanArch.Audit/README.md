# MinimalCleanArch.Audit

Audit logging components for MinimalCleanArch.

## Version
- 0.1.16-preview (net9.0, net10.0). Use with `MinimalCleanArch` 0.1.16-preview and companions.

## What's included
- Audit logging services and helpers.
- DI extensions to plug audit logging into your MinimalCleanArch app.
- Tenant-aware audit context support through `IAuditContextProvider`.
- Audit query service support for user, tenant, correlation ID, and flexible search queries.

## Usage
```bash
dotnet add package MinimalCleanArch.Audit --version 0.1.16-preview
```

Register services:

```csharp
builder.Services.AddAuditLogging();
builder.Services.AddAuditLogService<AppDbContext>();
```

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

When using a local feed, add a `nuget.config` pointing to your local packages folder (e.g., `artifacts/nuget`) before restoring.

