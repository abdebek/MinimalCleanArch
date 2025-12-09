# MinimalCleanArch.Validation

Validation components for MinimalCleanArch.

## Version
- 0.1.6 (net9.0). Use with `MinimalCleanArch` 0.1.6 and `MinimalCleanArch.Extensions`.

## Overview
- FluentValidation registration helpers.
- Integration with MinimalCleanArch.Extensions for endpoint validation.

## Usage
```bash
dotnet add package MinimalCleanArch.Validation --version 0.1.6
```

In Program.cs:
```csharp
builder.Services.AddValidatorsFromAssemblyContaining<Program>();
builder.Services.AddMinimalCleanArchExtensions(); // to hook validation into Minimal APIs
```

## Key Components

- ValidationExtensions - Extension methods for registering validators
- Integration with MinimalCleanArch.Extensions for endpoint validation
