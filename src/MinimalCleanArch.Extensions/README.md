# MinimalCleanArch.Extensions

Minimal API extensions for MinimalCleanArch.

## Version
- Current: 0.1.20-preview (net9.0, net10.0).

## Why Use It
- bootstrap a Minimal API host with a consistent pipeline instead of wiring validation, errors, OpenAPI/Scalar, rate limiting, Serilog, and related concerns by hand
- keep HTTP-specific concerns out of your domain and infrastructure packages
- standardize API behavior across generated and hand-built MCA applications

## When to Use It
- use it in the API/host project when you want the MCA HTTP pipeline and service-registration defaults over ASP.NET Core Minimal APIs
- choose it when your app exposes Minimal API endpoints and you want structured error mapping and endpoint conventions
- skip it in non-HTTP projects or when you intentionally want to assemble the host without MCA API helpers

## Dependency Direction
- Depends on: `MinimalCleanArch`, `MinimalCleanArch.Realtime` (SignalR adapter for `IRealtimePublisher`), `MinimalCleanArch.Features` (`RequireFeature`)
- Typically referenced by: API/host projects
- Used by: `MinimalCleanArch.Validation` for validation integration
- Do not reference from: domain projects; infrastructure projects should not need it except in very host-specific composition code

## Overview
- Validation: request/body validation helpers (e.g., `WithValidation<T>()`).
- Error handling: standard error pipeline middleware helpers, including structured `DomainException`/`Error` mapping to RFC 7807.
- Result mapping: `Result`/`Error` to `IResult` helpers for expected API outcomes (`MatchHttp`, `ToProblem`).
- OpenAPI helpers: standard response definitions and filters.
- Rate limiting: global and named endpoint policies with consistent `429` responses.
- Misc: path parameter validation, minimal API conveniences.
- Caching: `AddMinimalCleanArchCaching` / `AddMinimalCleanArchDistributedCaching` register `ICacheService` (memory or Redis). Cache DTOs, not domain entities with private setters.
- Realtime: `AddMinimalCleanArchRealtime` / `MapMinimalCleanArchRealtime` is the SignalR adapter for `IRealtimePublisher` (port in `MinimalCleanArch.Realtime`).
- Features: `RequireFeature` is the HTTP adapter for `IFeatureGate` (port in `MinimalCleanArch.Features`).

## Usage
```bash
dotnet add package MinimalCleanArch.Extensions --version 0.1.20-preview
```

Recommended API bootstrap:
```csharp
builder.Services.AddMinimalCleanArchApi(options =>
{
    options.AddValidatorsFromAssemblyContaining<CreateTodoCommandValidator>();
    options.EnableRateLimiting = true;
});
```

Equivalent explicit registration:
```csharp
builder.Services.AddMinimalCleanArchExtensions();
builder.Services.AddValidationFromAssemblyContaining<CreateTodoCommandValidator>();
builder.Services.AddMinimalCleanArchRateLimiting();
builder.Services.AddMinimalCleanArchApiVersioning();
```

Middleware:
```csharp
app.UseMinimalCleanArchApiDefaults(options =>
{
    options.UseRateLimiting = true;
});
```

Result mapping and in-handler validation:
```csharp
// Map Result / Result<T> failures to RFC 7807 ProblemDetails
return result.MatchHttp(httpContext, value => Results.Ok(value));

// Validate a command/query constructed in the handler (path ids, mapped bodies, etc.)
if (await httpContext.ValidateAsync(command, cancellationToken) is { } invalid)
{
    return invalid;
}

// Or validate a body parameter at the endpoint filter layer
group.MapPost("/", handler).WithValidation<CreateTodoRequest>();
```

`AddMinimalCleanArchApi(...)` is the preferred entry point when you want a single bootstrap method. Use the explicit registrations when you need tighter control over the service graph. `AddMinimalCleanArchExecutionContext()` registers HTTP `IExecutionContext` (claim → `TenantId`) without the rest of the API bootstrap.

Execution-context claim mapping can be customized without replacing `IExecutionContext`:

```csharp
builder.Services.Configure<ExecutionContextOptions>(options =>
{
    options.TenantIdClaimTypes.Clear();
    options.TenantIdClaimTypes.Add("business_id");
});
```


