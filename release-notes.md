## MinimalCleanArch {{TAG}}

### Summary
This release publishes MinimalCleanArch packages and templates at version `{{VERSION}}`.
Detailed commit and pull request notes are auto-generated below by GitHub.

### Compatibility
- Target frameworks: .NET 9.0 and .NET 10.0
- Delivery channels: NuGet packages + `dotnet new` templates

### Breaking Changes
- None.

### Migration Notes
- Update package references to `{{VERSION}}`.
- Re-run restore/build/tests after upgrading to confirm environment-specific behavior.

### Packages published
- `MinimalCleanArch`: core domain contracts and result/specification primitives
- `MinimalCleanArch.DataAccess`: EF Core repositories and unit of work
- `MinimalCleanArch.Extensions`: Minimal API bootstrap, OpenAPI, and operational defaults
- `MinimalCleanArch.Validation`: FluentValidation integration and registration helpers
- `MinimalCleanArch.Security`: encryption services and encrypted EF property support
- `MinimalCleanArch.Storage`: blob/object storage abstractions and Azure Blob integration
- `MinimalCleanArch.Messaging`: domain events and Wolverine integration
- `MinimalCleanArch.Audit`: audit interception and query support
- `MinimalCleanArch.Templates`: project scaffolding templates (`dotnet new mca`)

### Installation
```bash
dotnet add package MinimalCleanArch --version {{VERSION}}
dotnet add package MinimalCleanArch.DataAccess --version {{VERSION}}
dotnet add package MinimalCleanArch.Extensions --version {{VERSION}}
```

### Known Issues
- No known release-blocking issues at publish time.

See the README for full setup guidance and package-specific docs.
