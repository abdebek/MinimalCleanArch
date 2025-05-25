# MinimalCleanArch

A comprehensive library for implementing Clean Architecture with Minimal API in .NET 8+.

## 🚀 Features

- **Clean Architecture Foundation**: Domain entities, repositories, specifications, and unit of work patterns
- **Minimal API Extensions**: Fluent validation, error handling, and standardized responses
- **Security & Encryption**: Column-level encryption with Microsoft Data Protection API
- **Soft Delete & Auditing**: Automatic tracking of creation, modification, and deletion
- **Specification Pattern**: Encapsulate complex queries in reusable, testable objects
- **Result Pattern**: Type-safe error handling without exceptions
- **Entity Framework Integration**: Complete EF Core implementation with best practices

## 📦 Packages

| Package | Description |
|---------|-------------|
| **MinimalCleanArch** | Core interfaces and base classes |
| **MinimalCleanArch.DataAccess** | EF Core implementation |
| **MinimalCleanArch.Extensions** | Minimal API extensions and validation |
| **MinimalCleanArch.Validation** | FluentValidation integration |
| **MinimalCleanArch.Security** | Data encryption and security features |

## 🔧 Quick Start

### 1. Install Packages

```bash
dotnet add package MinimalCleanArch
dotnet add package MinimalCleanArch.DataAccess
dotnet add package MinimalCleanArch.Extensions
dotnet add package MinimalCleanArch.Validation
dotnet add package MinimalCleanArch.Security
```

### 2. Define Your Domain Entity

```csharp
public class Todo : BaseSoftDeleteEntity
{
    public string Title { get; private set; }
    
    [Encrypted] // Automatically encrypted in database
    public string Description { get; private set; }
    
    public int Priority { get; private set; }
    public DateTime? DueDate { get; private set; }
    public bool IsCompleted { get; private set; }

    public Todo(string title, string description, int priority = 0, DateTime? dueDate = null)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new DomainException("Title cannot be empty");
        
        if (priority < 0 || priority > 5)
            throw new DomainException("Priority must be between 0 and 5");

        Title = title;
        Description = description;
        Priority = priority;
        DueDate = dueDate;
    }

    public void Update(string title, string description, int priority, DateTime? dueDate)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new DomainException("Title cannot be empty");
            
        Title = title;
        Description = description;
        Priority = priority;
        DueDate = dueDate;
    }

    public void MarkAsCompleted() => IsCompleted = true;
}
```

### 3. Create Your DbContext

```csharp
public class ApplicationDbContext : DbContextBase
{
    public DbSet<Todo> Todos => Set<Todo>();

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) 
        : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Configure entities
        modelBuilder.Entity<Todo>(entity =>
        {
            entity.Property(e => e.Title).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.HasIndex(e => e.Priority);
        });

        base.OnModelCreating(modelBuilder);
    }

    protected override string? GetCurrentUserId()
    {
        // Return current user ID from your auth system
        return "system"; // or get from HttpContext
    }
}
```

### 4. Configure Services

```csharp
var builder = WebApplication.CreateBuilder(args);

// Add database with encryption
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

// Add MinimalCleanArch services
builder.Services.AddMinimalCleanArch<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

// Add encryption (choose one approach)
// Option 1: File-based key storage (good for single server)
builder.Services.AddDataProtectionEncryption("./keys", "MyApp");

// Option 2: Azure Key Vault (recommended for production)
// builder.Services.AddDataProtectionEncryptionWithAzureKeyVault(
//     "https://myvault.vault.azure.net/", "my-key", "MyApp");

// Add validation
builder.Services.AddValidatorsFromAssemblyContaining<CreateTodoValidator>();

var app = builder.Build();

// Add error handling middleware
app.UseMiddleware<ErrorHandlingMiddleware>();
```

### 5. Create API Endpoints

```csharp
// Create Todo
app.MapPost("/api/todos", async (
    CreateTodoRequest request,
    IRepository<Todo> repository,
    IUnitOfWork unitOfWork) =>
{
    var todo = new Todo(request.Title, request.Description, request.Priority, request.DueDate);
    
    await repository.AddAsync(todo);
    await unitOfWork.SaveChangesAsync();
    
    return Results.Created($"/api/todos/{todo.Id}", new TodoResponse(todo));
})
.WithValidation<CreateTodoRequest>()
.WithErrorHandling()
.WithStandardResponses<TodoResponse>();

// Get Todo with specification
app.MapGet("/api/todos", async (
    int? priority,
    bool? isCompleted,
    string? searchTerm,
    int pageIndex = 1,
    int pageSize = 10,
    IRepository<Todo> repository) =>
{
    var filterSpec = new TodoFilterSpecification(priority, isCompleted, searchTerm);
    var paginatedSpec = new TodoPaginatedSpecification(pageSize, pageIndex, filterSpec);
    
    var todos = await repository.GetAsync(paginatedSpec);
    var totalCount = await repository.CountAsync(filterSpec.Criteria);
    
    return Results.Ok(new
    {
        Items = todos.Select(t => new TodoResponse(t)),
        Pagination = new
        {
            CurrentPage = pageIndex,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
        }
    });
});

// Update Todo
app.MapPut("/api/todos/{id:int}", async (
    int id,
    UpdateTodoRequest request,
    IRepository<Todo> repository,
    IUnitOfWork unitOfWork) =>
{
    var todo = await repository.GetByIdAsync(id);
    if (todo == null)
        return Results.NotFound();
    
    todo.Update(request.Title, request.Description, request.Priority, request.DueDate);
    
    await repository.UpdateAsync(todo);
    await unitOfWork.SaveChangesAsync();
    
    return Results.Ok(new TodoResponse(todo));
})
.WithValidation<UpdateTodoRequest>()
.WithErrorHandling();

// Soft Delete Todo
app.MapDelete("/api/todos/{id:int}", async (
    int id,
    IRepository<Todo> repository,
    IUnitOfWork unitOfWork) =>
{
    var todo = await repository.GetByIdAsync(id);
    if (todo == null)
        return Results.NotFound();
    
    await repository.DeleteAsync(todo); // Soft delete
    await unitOfWork.SaveChangesAsync();
    
    return Results.NoContent();
});
```

### 6. Create Specifications for Complex Queries

```csharp
public class TodoFilterSpecification : BaseSpecification<Todo>
{
    public TodoFilterSpecification(
        int? priority = null,
        bool? isCompleted = null,
        string? searchTerm = null,
        DateTime? dueBefore = null,
        DateTime? dueAfter = null)
    {
        // Add filters
        if (priority.HasValue)
            AddCriteria(t => t.Priority == priority.Value);
            
        if (isCompleted.HasValue)
            AddCriteria(t => t.IsCompleted == isCompleted.Value);
            
        if (!string.IsNullOrWhiteSpace(searchTerm))
            AddCriteria(t => t.Title.Contains(searchTerm) || t.Description.Contains(searchTerm));
            
        if (dueBefore.HasValue)
            AddCriteria(t => t.DueDate != null && t.DueDate <= dueBefore.Value);
            
        if (dueAfter.HasValue)
            AddCriteria(t => t.DueDate != null && t.DueDate >= dueAfter.Value);
        
        // Default ordering
        ApplyOrderByDescending(t => t.Priority);
        ApplyThenByDescending(t => t.CreatedAt);
    }
}

public class TodoPaginatedSpecification : BaseSpecification<Todo>
{
    public TodoPaginatedSpecification(int pageSize, int pageIndex, TodoFilterSpecification filterSpec)
    {
        if (filterSpec.Criteria != null)
            AddCriteria(filterSpec.Criteria);
            
        ApplyOrderByDescending(t => t.Priority);
        ApplyThenByDescending(t => t.CreatedAt);
        ApplyPaging((pageIndex - 1) * pageSize, pageSize);
    }
}
```

### 7. Add Validation

```csharp
public class CreateTodoValidator : AbstractValidator<CreateTodoRequest>
{
    public CreateTodoValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty()
            .MaximumLength(200);
            
        RuleFor(x => x.Description)
            .MaximumLength(1000);
            
        RuleFor(x => x.Priority)
            .InclusiveBetween(0, 5);
            
        RuleFor(x => x.DueDate)
            .GreaterThan(DateTime.Now)
            .When(x => x.DueDate.HasValue);
    }
}
```

## 🔐 Security & Encryption

### Automatic Column Encryption

```csharp
public class User : BaseAuditableEntity
{
    public string Username { get; set; }
    
    [Encrypted] // Automatically encrypted/decrypted
    public string Email { get; set; }
    
    [Encrypted]
    public string? PhoneNumber { get; set; }
}
```

### Configure Encryption in DbContext

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    // Apply encryption to all [Encrypted] properties
    modelBuilder.UseEncryption(_encryptionService);
    
    // Or configure specific properties
    modelBuilder.UseEncryptionForProperty<User>(
        u => u.Email, 
        _encryptionService, 
        allowNull: false);

    base.OnModelCreating(modelBuilder);
}
```

## 🎯 Result Pattern Usage

```csharp
public class TodoService
{
    public async Task<Result<TodoResponse>> CreateTodoAsync(CreateTodoRequest request)
    {
        try
        {
            var todo = new Todo(request.Title, request.Description, request.Priority);
            await _repository.AddAsync(todo);
            await _unitOfWork.SaveChangesAsync();
            
            return Result.Success(new TodoResponse(todo));
        }
        catch (DomainException ex)
        {
            return Result.Failure<TodoResponse>(Error.Validation("INVALID_TODO", ex.Message));
        }
        catch (Exception ex)
        {
            return Result.Failure<TodoResponse>(Error.FromException(ex));
        }
    }
}

// Usage in endpoint
app.MapPost("/api/todos", async (CreateTodoRequest request, TodoService service) =>
{
    var result = await service.CreateTodoAsync(request);
    
    return result.IsSuccess 
        ? Results.Created($"/api/todos/{result.Value.Id}", result.Value)
        : Results.BadRequest(result.Error);
});
```

## 🧪 Testing

```csharp
[Fact]
public async Task Repository_ShouldSoftDelete_WhenEntityDeleted()
{
    // Arrange
    var todo = new Todo("Test", "Description");
    await _repository.AddAsync(todo);
    await _unitOfWork.SaveChangesAsync();

    // Act
    await _repository.DeleteAsync(todo);
    await _unitOfWork.SaveChangesAsync();

    // Assert
    var retrievedTodo = await _repository.GetByIdAsync(todo.Id);
    retrievedTodo.Should().BeNull(); // Soft deleted, not returned by default queries
    
    // Verify it still exists with IsDeleted = true
    var deletedTodo = await _dbContext.Todos
        .IgnoreQueryFilters()
        .FirstAsync(t => t.Id == todo.Id);
    deletedTodo.IsDeleted.Should().BeTrue();
}
```

## 🔄 Advanced Features

### Transactions

```csharp
await _unitOfWork.ExecuteInTransactionAsync(async () =>
{
    var todo1 = new Todo("Task 1", "Description 1");
    var todo2 = new Todo("Task 2", "Description 2");
    
    await _repository.AddRangeAsync(new[] { todo1, todo2 });
    await _unitOfWork.SaveChangesAsync();
    
    // Both todos are saved together or rolled back on error
});
```

### Bulk Operations with Extensions

```csharp
// Add and save in one operation
var todo = await _repository.AddAndSaveAsync(_unitOfWork, newTodo);

// Update and save in one operation  
var updatedTodo = await _repository.UpdateAndSaveAsync(_unitOfWork, existingTodo);
```

### Health Checks

```csharp
builder.Services.AddHealthChecks()
    .AddDbContextCheck<ApplicationDbContext>()
    .AddCheck<EncryptionHealthCheck>("encryption");

app.MapHealthChecks("/health");
```

## 📚 Documentation (coming soon)

## 🤝 Contributing

Contributions are welcome! Please read our [Contributing Guide](CONTRIBUTING.md) for details.

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

---

**MinimalCleanArch** - Clean Architecture made simple for .NET developers.