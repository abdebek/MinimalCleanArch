# Context: Security (encryption)

**Boundary:** NuGet `MinimalCleanArch.Security`. HTTP headers and CORS live in Extensions / the host, not this package.  
**Kind:** Infrastructure.  
**Database:** encrypted column values in the consumer store.  
**UI:** none.

## What it owns

| Type | Path |
|---|---|
| `IEncryptionService` | `src/MinimalCleanArch.Security/Encryption/IEncryptionService.cs` |
| `AesEncryptionService` | `Encryption/AesEncryptionService.cs` |
| `DataProtectionEncryptionService` | `Encryption/DataProtectionEncryptionService.cs` |
| `EncryptedAttribute` | `Encryption/EncryptedAttribute.cs` |
| `EncryptedConverter` | `EntityEncryption/EncryptedConverter.cs` |
| `UseEncryption` | `EntityEncryption/ModelBuilderExtensions.cs` |
| `AddEncryption`, `AddDataProtectionEncryptionForDevelopment` | `Extensions/` |
| `EncryptionOptions` | `Configuration/EncryptionOptions.cs` |

## Aggregates

None. `[Encrypted]` is an EF conversion. The domain still sees plaintext `string` after materialization.

## Hosts

No host. Sample registers `AddEncryption` before the DbContext and calls `modelBuilder.UseEncryption`. Template `--security` registers Data Protection in Development (or requires `Encryption:Key` outside Development) but `AppDbContext` does not call `UseEncryption`, so no generated property is encrypted unless the consumer adds that call.

## Integration

Sample encrypted fields: `Todo.Description`, `User.PersonalNotes`.

Security headers (`SecurityHeadersMiddleware`) and CORS (`Cors:AllowedOrigins`) belong to the HTTP host, not this package. `--auth` forces `--security` in the template.

## Honesty

- Domain entities in the sample reference `[Encrypted]`, which couples Domain to `MinimalCleanArch.Security`. The template Todo does not.
- A Development sample without `Encryption:Key` generates a throwaway key at startup (`Program.cs`).
