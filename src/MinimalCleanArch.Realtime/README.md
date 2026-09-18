# MinimalCleanArch.Realtime

Realtime publish port: channel, payload, optional tenant/user audience. No SignalR types on the port.

## Version
- Current: 0.1.20-preview (net9.0, net10.0).

## Why Use It
- push domain events to connected clients without taking a SignalR dependency in application code
- swap the no-op publisher for the SignalR adapter in `MinimalCleanArch.Extensions`

## When to Use It
- generated `--realtime` apps (Todo writes publish on channel `todos`)
- any host that needs a small pub/sub port

## Dependency Direction
- Depends on: no other MCA package
- Typically referenced by: application (publish) and the HTTP host (adapter)
- Do not reference from: domain projects
- SignalR adapter: `AddMinimalCleanArchRealtime` / `MapMinimalCleanArchRealtime` in `MinimalCleanArch.Extensions`

## Usage

```bash
dotnet add package MinimalCleanArch.Realtime --version 0.1.20-preview
```

```csharp
builder.Services.AddRealtime();
builder.Services.AddMinimalCleanArchRealtime(); // SignalR adapter (Extensions)
app.MapMinimalCleanArchRealtime();              // /hubs/realtime
```
