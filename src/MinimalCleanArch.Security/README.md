# MinimalCleanArch.Security

Security components for MinimalCleanArch.

## Version
-0.1.10-preview (net9.0, net10.0). Use with `MinimalCleanArch`0.1.10-preview.

## Overview
- Column-level encryption for EF Core.
- AES and Data Protection implementations of `IEncryptionService`.
- Value converters for encrypted properties.
- Extensions to configure encryption on your model.

## Usage
```bash
dotnet add package MinimalCleanArch.Security --version0.1.10-preview
```

In Program.cs:
```csharp
builder.Services.AddDataProtectionEncryptionForDevelopment("YourApp");
// Or: builder.Services.AddEncryption(new EncryptionOptions { Key = "YOUR_SECURE_AES_KEY" });
```

## Key Components

- EncryptedAttribute - Marks properties for encryption
- IEncryptionService - Interface for encryption services
- AesEncryptionService - AES implementation of IEncryptionService
- EncryptedConverter - Value converter for encrypted properties
- ModelBuilderExtensions - Extensions for configuring encryption
