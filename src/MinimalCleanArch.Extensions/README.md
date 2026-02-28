# MinimalCleanArch.Extensions

Minimal API extensions for MinimalCleanArch.

## Version
-0.1.12-preview (net9.0, net10.0). Use with `MinimalCleanArch`0.1.12-preview.

## Overview
- Validation: request/body validation helpers (e.g., `WithValidation<T>()`).
- Error handling: standard error pipeline middleware helpers, including structured `DomainException`/`Error` mapping to RFC 7807.
- OpenAPI helpers: standard response definitions and filters.
- Misc: path parameter validation, minimal API conveniences.

## Usage
```bash
dotnet add package MinimalCleanArch.Extensions --version0.1.12-preview
```

Register in your API:
```csharp
builder.Services.AddMinimalCleanArchExtensions();
// Optionally add validators:
builder.Services.AddValidatorsFromAssemblyContaining<Program>();
```
