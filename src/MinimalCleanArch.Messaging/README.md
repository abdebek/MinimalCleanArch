# MinimalCleanArch.Messaging

Messaging and domain event helpers for MinimalCleanArch (Wolverine integration).

## Version
- 0.1.16-preview (net9.0, net10.0). Use with `MinimalCleanArch` 0.1.16-preview and companions.

## What's included
- Domain event contracts and helpers.
- Wolverine integration extensions.
- DI extensions to wire messaging into your MinimalCleanArch app.

## Usage
```bash
dotnet add package MinimalCleanArch.Messaging --version 0.1.16-preview
```

Recommended bootstrap:

```csharp
builder.AddMinimalCleanArchMessaging(options =>
{
    options.IncludeAssembly(typeof(AssemblyReference).Assembly);
    options.ServiceName = "MyApp";
});
```

Durable transports:

```csharp
builder.AddMinimalCleanArchMessagingWithPostgres(connectionString, options =>
{
    options.IncludeAssembly(typeof(AssemblyReference).Assembly);
    options.ServiceName = "MyApp";
});
```

Use this package when:
- handlers and domain events are part of the application model
- you want domain event publishing wired through EF Core save operations
- you want an app-level entry point over raw Wolverine setup

Preferred guidance:
- use the `AddMinimalCleanArchMessaging...` extensions as the entry point
- keep app-specific queue, retry, and transport tuning in the provided callback
- avoid duplicating domain event publishing logic in application DbContexts

When using a local feed, add a `nuget.config` pointing to your local packages folder and keep `nuget.org` available unless your feed mirrors all external dependencies.

