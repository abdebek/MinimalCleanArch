using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MinimalCleanArch.DataAccess;
using MinimalCleanArch.Sample.Domain.Entities;
using MinimalCleanArch.Security.Encryption;
using MinimalCleanArch.Security.EntityEncryption;

namespace MinimalCleanArch.Sample.Infrastructure.Data;

/// <summary>
/// Application DbContext
/// </summary>
public class ApplicationDbContext : DbContextBase
{
    private readonly IEncryptionService _encryptionService;
    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="ApplicationDbContext"/> class
    /// </summary>
    /// <param name="options">The options to be used by the DbContext</param>
    /// <param name="encryptionService">The encryption service</param>
    /// <param name="serviceProvider">The service provider for logging</param>
    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options,
        IEncryptionService encryptionService,
        IServiceProvider serviceProvider)
        : base(options)
    {
        _encryptionService = encryptionService;
        _serviceProvider = serviceProvider;
    }

    /// <summary>
    /// Gets or sets the todos
    /// </summary>
    public DbSet<Todo> Todos => Set<Todo>();

    /// <summary>
    /// Configures the model
    /// </summary>
    /// <param name="modelBuilder">The builder being used to construct the model for this context</param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply entity configurations
        modelBuilder.ApplyConfiguration(new TodoConfiguration());

        // Apply encryption with enhanced logging
        modelBuilder.UseEncryption(_encryptionService, _serviceProvider);
    }

    /// <summary>
    /// Gets the current user ID from the application context
    /// </summary>
    /// <returns>The current user ID</returns>
    protected override string? GetCurrentUserId()
    {
        // In a real application, this would get the user ID from the current user context
        // For now, we'll use a simple system user
        return "system";
    }
}

/// <summary>
/// Configuration for the <see cref="Todo"/> entity
/// </summary>
public class TodoConfiguration : IEntityTypeConfiguration<Todo>
{
    /// <summary>
    /// Configures the entity
    /// </summary>
    /// <param name="builder">The entity type builder</param>
    public void Configure(EntityTypeBuilder<Todo> builder)
    {
        builder.ToTable("Todos");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Title)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(t => t.Description)
            .HasMaxLength(500);
        // Note: Encryption is configured automatically via the [Encrypted] attribute

        builder.Property(t => t.IsCompleted)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(t => t.Priority)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(t => t.DueDate);

        // Audit fields
        builder.Property(t => t.CreatedAt)
            .IsRequired();

        builder.Property(t => t.CreatedBy)
            .HasMaxLength(100);

        builder.Property(t => t.LastModifiedAt);

        builder.Property(t => t.LastModifiedBy)
            .HasMaxLength(100);

        // Soft delete
        builder.Property(t => t.IsDeleted)
            .IsRequired()
            .HasDefaultValue(false);

        // Indexes for better query performance
        builder.HasIndex(t => t.IsCompleted);
        builder.HasIndex(t => t.Priority);
        builder.HasIndex(t => t.DueDate);
        builder.HasIndex(t => t.IsDeleted);
        
        // Composite index for common queries
        builder.HasIndex(t => new { t.IsCompleted, t.Priority, t.IsDeleted });
    }
}