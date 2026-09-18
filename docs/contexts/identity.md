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
| Template `ApplicationUser` | `templates/mca/single/Application/Identity/ApplicationUser.cs` | `IdentityUser<Guid>`, `IAuditableEntity`, optional `IHasDomainEvents` | Lives in **Application** so Domain has no Identity package reference. With `--multitenant`, also has `TenantId` (not `ITenantEntity`; users are not row-filtered). |

EF and `UserManager` own persistence. MCA repositories are not used for users.

## Key types

Sample Identity is wired in `samples/MinimalCleanArch.Sample/Program.cs` (`AddIdentityApiEndpoints<User>`). The sample has no `IdentityServiceExtensions` type.

| Type | Path |
|---|---|
| `Roles` | `templates/mca/single/Domain/Constants/Roles.cs` (`Admin`, `User`, `Manager`); multi: `templates/mca/multi/MCA.Domain/Constants/Roles.cs` |
| `UserRegisteredEvent` | `templates/mca/single/Domain/Events/UserRegisteredEvent.cs` (multi: `MCA.Domain/Events/`) |
| Auth commands | template `Application/Commands/AuthCommands.cs` |
| `IAuthSessionService`, `IEmailService`, `ITokenService` | template `Application/Interfaces/` (`IEmailService` is auth-specific; transport is `MinimalCleanArch.Email.IEmailSender`) |
| `AuthSessionService` | single: `Infrastructure/Services/`; multi: `templates/mca/multi/MCA.Api/Services/` (generated `src/{Name}.Api/Services/`) |
| `EmailService`, `OpenIddictTokenService`, `PkceService` | template `Infrastructure/Services/` (multi: `MCA.Infrastructure/Services/`) |
| `OpenIddictSettings`, `EmailSettings` | template `Infrastructure/Configuration/` (multi: `MCA.Infrastructure/Configuration/`). Sample `EmailSettings` is in `samples/MinimalCleanArch.Sample/Infrastructure/Services/EmailSender.cs`. |
| `IdentityServiceExtensions` | template `--auth` only: single `templates/mca/single/Infrastructure/Configuration/IdentityServiceExtensions.cs`; multi `templates/mca/multi/MCA.Api/Configuration/IdentityServiceExtensions.cs` (generated `src/{Name}.Api/Configuration/`) |
| `AuthSettings` | leftover template source `templates/mca/**/Configuration/AuthSettings.cs`. Excluded by `template.json` (`**/Configuration/AuthSettings.cs`). Not present in generated apps. Live settings are `OpenIddictSettings` and `EmailSettings`. |

## Commands, queries, hosts

Template commands: `RegisterUserCommand`, `ConfirmEmailCommand`, `AuthLoginCommand`, `AuthLogoutCommand`, `ChangePasswordCommand`, `ForgotPasswordCommand`, `ResetPasswordCommand`, `ExternalAuthSignInCommand`.

Handlers: `RegisterUserHandler`, `ConfirmEmailHandler`, `AuthLoginHandler`, `AuthLogoutHandler`, `ChangePasswordHandler`, `ForgotPasswordHandler`, `ResetPasswordHandler`, `ExternalAuthSignInHandler`, `AuthEventHandler`.

| Host surface | Sample | Template `--auth` |
|---|---|---|
| Identity API | `MapIdentityApi<User>()` (`/register`, `/login`, ...) | not used |
| Custom users | `/api/users/*`, `/api/admin/users/*` | `/api/auth/*` |
| Tokens | cookie / Identity bearer from Identity API (no refresh grant) | OpenIddict `/connect/token` (password, authorization_code, **refresh_token**), `/connect/authorize`, `/connect/userinfo`, `/connect/revoke`, `/connect/logout` |
| Dev only | none extra | `/oauth/demo/*`, `/dev/openiddict/*` |

## UI

None beyond Scalar. Template `--auth` login HTML links to `/api/auth/external/{Google|Microsoft|GitHub}`. Those schemes register only when `Authentication:{Provider}:ClientId` and `ClientSecret` are set (user-secrets/env). The sample has no equivalent.

## Integration

| Other context | How |
|---|---|
| Todo | Shared DB. Sample lists todos by `CreatedBy`. No navigation property. `--auth --multitenant` issues `tenant_id` from `ApplicationUser.TenantId` and isolates Todo rows via the EF filter. Create org + invite-by-code switches the invitee onto that tenant so Todos are shared. |
| Messaging | `UserRegisteredEvent` after register when `--messaging`. Email moves to `AuthEventHandler`. |
| Audit | User row changes audited if interceptor is on. Sample excludes `PasswordHash`, `SecurityStamp`. |
| HTTP host | Policies `Admin` and `User` in the sample. Template uses `RequireAuthorization` on change-password. |

## Honesty

- Anemic user models.
- Application layer depends on `UserManager<ApplicationUser>` (Identity).
- Shared database with Todo and audit.
- Sample Domain references `Microsoft.AspNetCore.Identity` and `MinimalCleanArch.Security.Encryption`. The generated template deliberately does not.
- Password reset HTTP responses never return the token.
