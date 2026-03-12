# MinimalCleanArch.Extensions

Minimal API extensions for MinimalCleanArch.

## Version
- Current stable: 0.1.18 (net9.0, net10.0).

## Why Use It
- bootstrap a Minimal API host with a consistent pipeline instead of wiring validation, errors, OpenAPI/Scalar, rate limiting, Serilog, and related concerns by hand
- keep HTTP-specific concerns out of your domain and infrastructure packages
- standardize API behavior across generated and hand-built MCA applications

## When to Use It
- use it in the API/host project when you want the MCA HTTP pipeline and service-registration defaults over ASP.NET Core Minimal APIs
- choose it when your app exposes Minimal API endpoints and you want structured error mapping and endpoint conventions
- skip it in non-HTTP projects or when you intentionally want to assemble the host without MCA API helpers

## Dependency Direction
- Depends on: `MinimalCleanArch`
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

## Usage
```bash
dotnet add package MinimalCleanArch.Extensions --version 0.1.18
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
```

Middleware:
```csharp
app.UseMinimalCleanArchApiDefaults(options =>
{
    options.UseRateLimiting = true;
});
```

`AddMinimalCleanArchApi(...)` is the preferred entry point when you want a single bootstrap method. Use the explicit registrations when you need tighter control over the service graph.

Execution-context claim mapping can be customized without replacing `IExecutionContext`:

```csharp
builder.Services.Configure<ExecutionContextOptions>(options =>
{
    options.TenantIdClaimTypes.Clear();
    options.TenantIdClaimTypes.Add("business_id");
});
```


