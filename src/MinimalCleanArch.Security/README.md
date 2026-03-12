# MinimalCleanArch.Security

Security components for MinimalCleanArch.

## Version
- 0.1.17 (stable, net9.0, net10.0). Use with `MinimalCleanArch` 0.1.17.

## Overview
- Column-level encryption for EF Core.
- AES and Data Protection implementations of `IEncryptionService`.
- Value converters for encrypted properties.
- Extensions to configure encryption on your model.

## Usage
```bash
dotnet add package MinimalCleanArch.Security --version 0.1.17
```

Recommended service registration:
```csharp
builder.Services.AddDataProtectionEncryptionForDevelopment("YourApp");
```

Production-style registration:

```csharp
builder.Services.AddEncryption(new EncryptionOptions
{
    Key = "YOUR_SECURE_AES_KEY"
});
```

Model configuration:

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    base.OnModelCreating(modelBuilder);
    modelBuilder.UseEncryption(encryptionService);
}
```

Recommended guidance:
- use Data Protection-based encryption for new application development
- use `AddDataProtectionEncryptionForDevelopment(...)` only for local development
- use persistent key storage in production
- use the hybrid Data Protection and AES support when migrating legacy encrypted data
- keep encryption registration in infrastructure, not in domain or application layers

## Key Components

- EncryptedAttribute - Marks properties for encryption
- IEncryptionService - Interface for encryption services
- AesEncryptionService - AES implementation of IEncryptionService
- EncryptedConverter - Value converter for encrypted properties
