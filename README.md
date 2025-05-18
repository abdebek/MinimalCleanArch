# MinimalCleanArch

A comprehensive library for implementing Clean Architecture with Minimal API in .NET.

## Features

- **Domain-Driven Design support**: Base entity classes, domain exceptions, and more
- **Repository pattern**: Generic repositories with rich query capabilities
- **Specification pattern**: Encapsulate complex queries in reusable objects
- **Soft delete support**: Automatic soft delete with global query filters
- **Auditing**: Automatic tracking of creation and modification
- **Encryption**: Transparent column-level encryption for sensitive data
- **Validation**: FluentValidation integration with custom minimal API extensions
- **Minimal API utilities**: Extensions for cleaner endpoint definitions

## Packages

- **MinimalCleanArch**: Core interfaces and base classes
- **MinimalCleanArch.EntityFramework**: EF Core implementation of repositories and DB context
- **MinimalCleanArch.Extensions**: Extensions for Minimal API endpoints
- **MinimalCleanArch.Validation**: Validation for Minimal API using FluentValidation
- **MinimalCleanArch.Security**: Encryption and security features

## Getting Started

### 1. Define your entities

`csharp
public class Todo : BaseSoftDeleteEntity
{
    public string Title { get; private set; }
    
    [Encrypted]
    public string Description { get; private set; }
    
    public bool IsCompleted { get; private set; }
    
    // Constructor with validation
    public Todo(string title, string description)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new DomainException("Title cannot be empty");
            
        Title = title;
        Description = description;
    }
    
    // Methods for state changes
    public void MarkAsCompleted()
    {
        IsCompleted = true;
    }
}
`

### 2. Create your DbContext

`csharp
public class ApplicationDbContext : DbContextBase
{
    private readonly IEncryptionService _encryptionService;
    
    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options,
        IEncryptionService encryptionService)
        : base(options)
    {
        _encryptionService = encryptionService;
    }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Apply entity configurations
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        
        // Apply encryption
        modelBuilder.UseEncryption(_encryptionService);
        
        base.OnModelCreating(modelBuilder);
    }
}
`

## License

MIT
