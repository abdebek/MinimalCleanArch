# MinimalCleanArch.Validation

Validation components for MinimalCleanArch.

## Version
- 0.1.17 (stable, net9.0, net10.0). Use with `MinimalCleanArch` 0.1.17 and `MinimalCleanArch.Extensions`.

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

