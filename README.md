# MinimalCleanArch

A comprehensive library for implementing Clean Architecture with Minimal API in .NET 9. It offers a solid foundation with built-in support for repositories, unit of work, specifications, domain-driven design patterns, security features like data encryption, and extensions for modern ASP.NET Core development.

## 🚀 Core Features

-   **Clean Architecture Foundation**: Robust base classes and interfaces for domain entities, repositories, specifications, and the unit of work pattern.
-   **Minimal API Extensions**: Streamline your Minimal API development with integrated FluentValidation, standardized error handling, and OpenAPI response definitions.
-   **Security & Encryption**: Protect sensitive data with column-level encryption using either the Microsoft Data Protection API (recommended) or AES. Includes helpers for key management and EF Core integration.
-   **Soft Delete & Auditing**: Automatically manage `IsDeleted` flags and track `CreatedAt`, `CreatedBy`, `LastModifiedAt`, `LastModifiedBy` for entities.
-   **Specification Pattern**: Encapsulate complex query logic into reusable and testable specification objects, promoting cleaner data access.
-   **Result Pattern**: Enhance error handling with a type-safe `Result<T>` pattern, reducing reliance on exceptions for control flow.
-   **Entity Framework Integration**: Provides EF Core implementations for repositories, unit of work, and automated handling of auditing, soft delete, and encryption.

## Version & Templates

- Current package version: `0.1.6` (targets .NET 9).
- Templates: install with `dotnet new install MinimalCleanArch.Templates` (or the local nupkg) and scaffold via `dotnet new mca -n MyApp` (multi-project default) or `--single-project`.
- Template launch settings now default to Swagger and use randomized ports in the 5000–8000 range; adjust in `Properties/launchSettings.json` if you need fixed ports.
- Using local nupkgs? Add a `nuget.config` with a `packageSources` entry pointing to your local folder (e.g., `D:\C\repos\MinimalCleanArch\artifacts\nuget`) before restoring.

## 📦 Packages

| Package                       | Description                                  |
| :---------------------------- | :------------------------------------------- |
| `MinimalCleanArch`              | Core interfaces and base classes (Entities, Repositories, Specifications, Result pattern). |
| `MinimalCleanArch.DataAccess`   | Entity Framework Core implementation (DbContextBase, Repository, UnitOfWork, SpecificationEvaluator). |
| `MinimalCleanArch.Extensions`   | Minimal API enhancements (validation filters, error handling, standard responses). |
| `MinimalCleanArch.Validation`   | FluentValidation integration components (Note: often used via `MinimalCleanArch.Extensions`). |
| `MinimalCleanArch.Security`     | Data encryption services (AES, Data Protection) and EF Core integration for encrypted properties. |

## 🔧 Quick Start

1.  **Install Packages** (from NuGet or your local feed):
    ```bash
    dotnet add package MinimalCleanArch
    dotnet add package MinimalCleanArch.DataAccess
    dotnet add package MinimalCleanArch.Extensions
    dotnet add package MinimalCleanArch.Security
    ```

2.  **Define Domain Entity** (e.g., `Todo.cs`):
    ```csharp
    using MinimalCleanArch.Domain.Entities;
    using MinimalCleanArch.Domain.Exceptions;
    using MinimalCleanArch.Security.Encryption;

    public class Todo : BaseSoftDeleteEntity // Includes Id, Auditing, SoftDelete
    {
        public string Title { get; private set; }
        [Encrypted] public string Description { get; private set; } // Will be encrypted
        public int Priority { get; private set; }
        public DateTime? DueDate { get; private set; }
        public bool IsCompleted { get; private set; }

        private Todo() { /* Required for EF Core */ }
        public Todo(string title, string description, int priority = 0, DateTime? dueDate = null)
        {
            SetTitle(title);
            Description = description ?? string.Empty;
            SetPriority(priority);
            DueDate = dueDate;
        }
        // Methods like Update, MarkAsCompleted, SetTitle, SetPriority...
        public void SetTitle(string title) {
            if (string.IsNullOrWhiteSpace(title)) throw new DomainException("Title is required.");
            Title = title;
        }
        public void SetPriority(int priority) {
            if (priority < 0 || priority > 5) throw new DomainException("Priority must be 0-5.");
            Priority = priority;
        }
    }
    ```

3.  **Create DbContext** (e.g., `ApplicationDbContext.cs`):
    ```csharp
    using Microsoft.EntityFrameworkCore;
    using Microsoft.AspNetCore.Http; // For IHttpContextAccessor
    using System.Security.Claims;     // For ClaimTypes
    using MinimalCleanArch.DataAccess;
    using MinimalCleanArch.Security.Encryption;
    using MinimalCleanArch.Security.EntityEncryption; // For UseEncryption extension
    // using YourProject.Domain.Entities;

    public class ApplicationDbContext : DbContextBase // Handles Auditing & Soft Delete
    {
        private readonly IEncryptionService _encryptionService;
        private readonly IHttpContextAccessor? _httpContextAccessor;

        public DbSet<Todo> Todos => Set<Todo>();

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options,
                                    IEncryptionService encryptionService,
                                    IHttpContextAccessor? httpContextAccessor = null)
            : base(options)
        {
            _encryptionService = encryptionService;
            _httpContextAccessor = httpContextAccessor;
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Todo>(entity => { /* Configure Todo entity */ });
            modelBuilder.UseEncryption(_encryptionService); // Apply [Encrypted] attribute handling
            base.OnModelCreating(modelBuilder); // Applies soft delete filters
        }

        protected override string? GetCurrentUserId() =>
            _httpContextAccessor?.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier) ?? "system";
    }
    ```

4.  **Configure Services** (`Program.cs`):
    ```csharp
    using Microsoft.EntityFrameworkCore;
    using MinimalCleanArch.DataAccess.Extensions;
    using MinimalCleanArch.Security.Extensions;
    using MinimalCleanArch.Extensions.Extensions;
    using MinimalCleanArch.Extensions.Middlewares;
    // using YourProject.Infrastructure.Data;
    // using YourProject.Application.Validation; // For validator assembly scanning

    var builder = WebApplication.CreateBuilder(args);
    var connString = builder.Configuration.GetConnectionString("DefaultConnection");

    // Encryption (Choose one, Data Protection recommended)
    builder.Services.AddDataProtectionEncryptionForDevelopment("YourAppName"); //
    // Or: builder.Services.AddEncryption(new EncryptionOptions { Key = "YOUR_SECURE_AES_KEY" });

    builder.Services.AddHttpContextAccessor(); // For GetCurrentUserId in DbContext

    // MinimalCleanArch: DbContext, Repositories, UnitOfWork
    builder.Services.AddMinimalCleanArch<ApplicationDbContext>(opt => opt.UseSqlServer(connString));

    // Validation & API Extensions
    builder.Services.AddValidatorsFromAssemblyContaining<Program>(); // Or a specific validator type
    builder.Services.AddMinimalCleanArchExtensions();

    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();

    var app = builder.Build();

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
        // Seed DB or apply migrations
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        db.Database.EnsureCreated(); // For sample; use migrations in production
    }
    app.UseMiddleware<ErrorHandlingMiddleware>(); // Global error handling
    // Map endpoints...
    app.Run();
    ```

5.  **Create API Endpoints** with validation and error handling:
    ```csharp
    // using YourProject.Domain.Entities;
    // using YourProject.API.Models; // For CreateTodoRequest, TodoResponse DTOs
    // using MinimalCleanArch.Repositories;
    // using MinimalCleanArch.Extensions.Extensions;

    public static IEndpointRouteBuilder MapMyEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/todos", async (CreateTodoRequest req, IRepository<Todo> repo, IUnitOfWork uow) =>
        {
            var todo = new Todo(req.Title, req.Description, req.Priority, req.DueDate);
            await repo.AddAsync(todo);
            await uow.SaveChangesAsync();
            return Results.Created($"/todos/{todo.Id}", TodoResponse.FromEntity(todo));
        })
        .WithValidation<CreateTodoRequest>()
        .WithErrorHandling()
        .WithStandardResponses<TodoResponse>();
        return app;
    }
    // Define CreateTodoRequest and TodoResponse DTOs
    public record CreateTodoRequest(string Title, string Description, int Priority, DateTime? DueDate);
    public record TodoResponse(int Id, string Title, string Description, bool IsCompleted, int Priority, DateTime? DueDate, DateTime CreatedAt)
    {
        public static TodoResponse FromEntity(Todo todo) =>
            new(todo.Id, todo.Title, todo.Description, todo.IsCompleted, todo.Priority, todo.DueDate, todo.CreatedAt);
    }
    ```

## 📖 Sample Application & Documentation

-   **Sample Project**: A comprehensive sample application is available in the `/samples/MinimalCleanArch.Sample` directory.
-   **Documentation**: Detailed documentation is generated using DocFX and can be found in the `/docs` directory (link to hosted docs coming soon).

## 🤝 Contributing

Contributions are welcome! Please read our [Contributing Guide](CONTRIBUTING.md).

## 📄 License

This project is licensed under the MIT License. See the [LICENSE](LICENSE) file for details.

---

**MinimalCleanArch** - Clean Architecture made simple for .NET developers.
