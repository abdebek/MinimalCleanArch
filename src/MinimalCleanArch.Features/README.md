# MinimalCleanArch.Features

Feature-flag port: `IFeatureGate.IsEnabled(feature, tenant)`. Configuration backing; optional `IFeatureStore` for table overrides. No ASP.NET types on the port.

## Version
- Current: 0.1.20-preview (net9.0, net10.0).

## Why Use It
- gate an endpoint or workflow without putting flag checks in domain handlers
- fail closed: a missing flag is disabled
- swap config for a table/store without changing callers

## When to Use It
- generated `--features` apps (`GET /api/todos/export` gated by `todo-export`)
- any host that needs tenant-aware entitlements

## Dependency Direction
- Depends on: no other MCA package
- Typically referenced by: the HTTP host (filter) and optionally application code
- Do not reference from: domain projects
- HTTP adapter: `RequireFeature` in `MinimalCleanArch.Extensions`

## Usage

```bash
dotnet add package MinimalCleanArch.Features --version 0.1.20-preview
```

```json
{
  "Features": {
    "Flags": {
      "todo-export": false
    }
  }
}
```

```csharp
builder.Services.AddFeatures(builder.Configuration);
app.MapGet("/api/todos/export", Export).RequireFeature("todo-export");
```
