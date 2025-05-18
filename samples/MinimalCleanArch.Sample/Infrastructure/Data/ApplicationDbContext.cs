using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MinimalCleanArch.EntityFramework;
using MinimalCleanArch.Sample.Domain.Entities;
using MinimalCleanArch.Security.Encryption;
using MinimalCleanArch.Security.EntityFramework;

namespace MinimalCleanArch.Sample.Infrastructure.Data;

/// <summary>
/// Application DbContext
/// </summary>
public class ApplicationDbContext : DbContextBase
{
    private readonly IEncryptionService _encryptionService;

    /// <summary>
    /// Initializes a new instance of the <see cref="ApplicationDbContext"/> class
    /// </summary>
    /// <param name="options">The options to be used by the DbContext</param>
    /// <param name="encryptionService">The encryption service</param>
    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options,
        IEncryptionService encryptionService)
        : base(options)
    {
        _encryptionService = encryptionService;
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

        // Apply encryption
        modelBuilder.UseEncryption(_encryptionService);
    }

    /// <summary>
    /// Gets the current user ID
    /// </summary>
    /// <returns>The current user ID</returns>
    protected override string? GetCurrentUserId()
    {
        // In a real application, this would get the user ID from the current user context
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

        builder.Property(t => t.IsCompleted)
            .IsRequired();

        builder.Property(t => t.Priority)
            .IsRequired();

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
            .IsRequired();
    }
}
