# MinimalCleanArch.Email

SMTP and HTTP API email adapters behind a framework-neutral port.

## Version
- Current: 0.1.20-preview (net9.0, net10.0).

## Why Use It
- send templated messages (to, subject, html/text, correlation) without ASP.NET types on the port
- swap SMTP vs HTTP API with `EmailSettings:Provider`

## When to Use It
- generated `--auth` apps (confirm / forgot / reset)
- any host that needs outbound email without copying SmtpClient wrappers

## Dependency Direction
- Depends on: no other MCA package
- Typically referenced by: infrastructure and composition roots
- Do not reference from: domain projects

## Usage

```bash
dotnet add package MinimalCleanArch.Email --version 0.1.20-preview
```

```csharp
builder.Services.AddEmail(builder.Configuration);
```

`EmailSettings:Provider` = `Smtp` (default) or `Api`. Bind the same section the generated template already uses.
