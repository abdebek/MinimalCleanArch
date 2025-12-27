# MinimalCleanArch.Validation

Validation components for MinimalCleanArch.

## Version
-0.1.10-preview (net9.0, net10.0). Use with `MinimalCleanArch`0.1.10-preview and `MinimalCleanArch.Extensions`.

## Overview
- FluentValidation registration helpers.
- Integration with MinimalCleanArch.Extensions for endpoint validation.

## Usage
```bash
dotnet add package MinimalCleanArch.Validation --version0.1.10-preview
```

In Program.cs:
```csharp
builder.Services.AddValidatorsFromAssemblyContaining<Program>();
builder.Services.AddMinimalCleanArchExtensions(); // to hook validation into Minimal APIs
```

## Key Components

- ValidationExtensions - Extension methods for registering validators
- Integration with MinimalCleanArch.Extensions for endpoint validation
