# MinimalCleanArch.Extensions

Minimal API extensions for MinimalCleanArch.

## Version
- 0.1.14-preview (net9.0, net10.0). Use with `MinimalCleanArch` 0.1.14-preview.

## Overview
- Validation: request/body validation helpers (e.g., `WithValidation<T>()`).
- Error handling: standard error pipeline middleware helpers, including structured `DomainException`/`Error` mapping to RFC 7807.
- Result mapping: `Result`/`Error` to `IResult` helpers for expected API outcomes (`MatchHttp`, `ToProblem`).
- OpenAPI helpers: standard response definitions and filters.
- Rate limiting: global and named endpoint policies with consistent `429` responses.
- Misc: path parameter validation, minimal API conveniences.

## Usage
```bash
dotnet add package MinimalCleanArch.Extensions --version 0.1.14-preview
```

Register in your API:
```csharp
builder.Services.AddMinimalCleanArchExtensions();
// Optionally add validators:
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

// Optional rate limiting
builder.Services.AddMinimalCleanArchRateLimiting();
```

Then enable middleware:
```csharp
app.UseMinimalCleanArchRateLimiting();
```

