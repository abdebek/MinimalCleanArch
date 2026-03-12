# MinimalCleanArch.Validation

Validation components for MinimalCleanArch.

## Version
- 0.1.17 (stable, net9.0, net10.0). Use with `MinimalCleanArch` 0.1.17 and `MinimalCleanArch.Extensions`.

## Why Use It
- register FluentValidation validators with short, MCA-oriented extension methods
- plug validators into the MCA Minimal API pipeline without repeating setup code across hosts
- keep validator discovery and registration consistent between generated apps and hand-built applications

## When to Use It
- use it when your API project relies on FluentValidation and MCA endpoint validation
- keep it at the API/host or composition-root boundary where validators are registered
- skip it if you are not using FluentValidation or you are not using the MCA HTTP pipeline

## Dependency Direction
- Depends on: `MinimalCleanArch`, `MinimalCleanArch.Extensions`
- Typically referenced by: API/host projects or composition roots
- Do not reference from: domain projects; infrastructure projects generally do not need it
- Note: this package exists to support host-side validator registration, not domain modeling

## Overview
- FluentValidation registration helpers.
- Integration with `MinimalCleanArch.Extensions` endpoint validation.
- Short registration methods for application validators.

## Usage
```bash
dotnet add package MinimalCleanArch.Validation --version 0.1.17
```

Recommended registration:
```csharp
builder.Services.AddValidationFromAssemblyContaining<CreateTodoCommandValidator>();
builder.Services.AddMinimalCleanArchExtensions();
```

If you use the higher-level API bootstrap from `MinimalCleanArch.Extensions`, you can register validators there instead:

```csharp
builder.Services.AddMinimalCleanArchApi(options =>
{
    options.AddValidatorsFromAssemblyContaining<CreateTodoCommandValidator>();
});
```

Recommended methods:
- `AddValidation(...)` registers validators from one or more assemblies.
- `AddValidationFromAssemblyContaining<T>()` registers validators from the assembly containing `T`.
- `AddMinimalCleanArchValidation(...)` and `AddMinimalCleanArchValidationFromAssemblyContaining<T>()` remain available as compatibility aliases.

