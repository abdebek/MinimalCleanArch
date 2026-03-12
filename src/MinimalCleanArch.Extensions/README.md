# MinimalCleanArch.Extensions

Minimal API extensions for MinimalCleanArch.

## Version
- 0.1.17 (stable, net9.0, net10.0). Use with `MinimalCleanArch` 0.1.17.

## Overview
- Validation: request/body validation helpers (e.g., `WithValidation<T>()`).
- Error handling: standard error pipeline middleware helpers, including structured `DomainException`/`Error` mapping to RFC 7807.
- Result mapping: `Result`/`Error` to `IResult` helpers for expected API outcomes (`MatchHttp`, `ToProblem`).
- OpenAPI helpers: standard response definitions and filters.
- Rate limiting: global and named endpoint policies with consistent `429` responses.
- Misc: path parameter validation, minimal API conveniences.

## Usage
```bash
dotnet add package MinimalCleanArch.Extensions --version 0.1.17
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

