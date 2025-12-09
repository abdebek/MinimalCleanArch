# MinimalCleanArch.Extensions

Minimal API extensions for MinimalCleanArch.

## Version
- 0.1.6 (net9.0). Use with `MinimalCleanArch` 0.1.6.

## Overview
- Validation: request/body validation helpers (e.g., `WithValidation<T>()`).
- Error handling: standard error pipeline middleware helpers.
- OpenAPI helpers: standard response definitions and filters.
- Misc: path parameter validation, minimal API conveniences.

## Usage
```bash
dotnet add package MinimalCleanArch.Extensions --version 0.1.6
```

Register in your API:
```csharp
builder.Services.AddMinimalCleanArchExtensions();
// Optionally add validators:
builder.Services.AddValidatorsFromAssemblyContaining<Program>();
```
