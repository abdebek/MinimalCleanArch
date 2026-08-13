# Context: Identity and auth

**Boundary:** ASP.NET Identity (and OpenIddict in templates), not a Domain project.  
**Kind:** Framework owned identity. DDD inspired only at the edges (`IAuditableEntity`, optional events).  
**Database:** Identity tables (and OpenIddict) in the **same** DbContext as Todos.  
**UI:** HTTP endpoints + Scalar password flow. No MVC UI.

## Aggregates

There is no identity aggregate root in Domain.

| Model | Path | Base | Notes |
|---|---|---|---|
| Sample `User` | `samples/MinimalCleanArch.Sample/Domain/Entities/User.cs` | `IdentityUser`, `IAuditableEntity`, `ISoftDelete` | Public setters. `[Encrypted] PersonalNotes`. Anemic. |
| Template `ApplicationUser` | `templates/mca/single/Application/Identity/ApplicationUser.cs` | `IdentityUser<Guid>`, `IAuditableEntity`, optional `IHasDomainEvents` | Lives in **Application** so Domain has no Identity package reference. |

EF and `UserManager` own persistence. MCA repositories are not used for users.

## Key types

| Type | Path |
|---|---|
| `Roles` | `templates/mca/single/Domain/Constants/Roles.cs` (`Admin`, `User`, `Manager`) |
| `UserRegisteredEvent` | `templates/mca/single/Domain/Events/UserRegisteredEvent.cs` |
| Auth commands | `Application/Commands/AuthCommands.cs` |
| `IAuthSessionService`, `IEmailService`, `ITokenService` | `Application/Interfaces/` |
| `AuthSessionService`, `EmailService`, `OpenIddictTokenService`, `PkceService` | `Infrastructure/Services/` |
| `AuthSettings`, `OpenIddictSettings`, `EmailSettings` | `Infrastructure/Configuration/` |
| `IdentityServiceExtensions` | sample host / template `Infrastructure/Configuration/` or `MCA.Api/Configuration/` |

## Commands, queries, hosts

Template commands: `RegisterUserCommand`, `ConfirmEmailCommand`, `AuthLoginCommand`, `AuthLogoutCommand`, `ChangePasswordCommand`, `ForgotPasswordCommand`, `ResetPasswordCommand`, `ExternalAuthSignInCommand`.

Handlers: `RegisterUserHandler`, `ConfirmEmailHandler`, `AuthLoginHandler`, `AuthLogoutHandler`, `ChangePasswordHandler`, `ForgotPasswordHandler`, `ResetPasswordHandler`, `ExternalAuthSignInHandler`, `AuthEventHandler`.

| Host surface | Sample | Template `--auth` |
|---|---|---|
| Identity API | `MapIdentityApi<User>()` (`/register`, `/login`, ...) | not used |
| Custom users | `/api/users/*`, `/api/admin/users/*` | `/api/auth/*` |
| Tokens | cookie / Identity bearer from Identity API | OpenIddict `/connect/token`, `/connect/authorize`, `/connect/userinfo` |
| Dev only | none extra | `/oauth/demo/*`, `/dev/openiddict/*` |

## UI

None beyond Scalar. External provider buttons in `AuthEndpoints` are commented until Google/Microsoft/GitHub are uncommented in `IdentityServiceExtensions`.

## Integration

| Other context | How |
|---|---|
| Todo | Shared DB. Sample lists todos by `CreatedBy`. No navigation property. |
| Messaging | `UserRegisteredEvent` after register when `--messaging`. Email moves to `AuthEventHandler`. |
| Audit | User row changes audited if interceptor is on. Sample excludes `PasswordHash`, `SecurityStamp`. |
| HTTP host | Policies `Admin` and `User` in the sample. Template uses `RequireAuthorization` on change-password. |

## Honesty

- Anemic user models.
- Application layer depends on `UserManager<ApplicationUser>` (Identity).
- Shared database with Todo and audit.
- Sample Domain references `Microsoft.AspNetCore.Identity` and `MinimalCleanArch.Security.Encryption`. The generated template deliberately does not.
- Password reset HTTP responses never return the token.
