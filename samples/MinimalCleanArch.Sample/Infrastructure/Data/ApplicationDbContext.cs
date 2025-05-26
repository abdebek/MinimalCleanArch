using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MinimalCleanArch.DataAccess;
using MinimalCleanArch.Sample.Domain.Entities;
using MinimalCleanArch.Security.Encryption;
using MinimalCleanArch.Security.EntityEncryption;
using System.Security.Claims;

namespace MinimalCleanArch.Sample.Infrastructure.Data;

/// <summary>
/// Application DbContext with Identity and MinimalCleanArch features
/// </summary>
public class ApplicationDbContext : IdentityDbContextBase<User>
{
    private readonly IEncryptionService _encryptionService;
    private readonly IServiceProvider _serviceProvider;
    private readonly IHttpContextAccessor? _httpContextAccessor;

    /// <summary>
    /// Initializes a new instance of the <see cref="ApplicationDbContext"/> class
    /// </summary>
    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options,
        IEncryptionService encryptionService,
        IServiceProvider serviceProvider,
        IHttpContextAccessor? httpContextAccessor = null)
        : base(options)
    {
        _encryptionService = encryptionService;
        _serviceProvider = serviceProvider;
        _httpContextAccessor = httpContextAccessor;
    }

    // Application entities
    public DbSet<Todo> Todos => Set<Todo>();

    /// <summary>
    /// Configures the model
    /// </summary>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure Identity entities
        ConfigureIdentityEntities(modelBuilder);

        // Configure application entities
        modelBuilder.ApplyConfiguration(new TodoConfiguration());

        // Apply encryption with enhanced logging
        modelBuilder.UseEncryption(_encryptionService, _serviceProvider);
    }

    private void ConfigureIdentityEntities(ModelBuilder modelBuilder)
    {
        // Configure the User entity
        modelBuilder.Entity<User>(entity =>
        {
            // Custom properties
            entity.Property(e => e.FullName).HasMaxLength(200);
            entity.Property(e => e.DateOfBirth);
            entity.Property(e => e.PersonalNotes).HasMaxLength(1000);
            // Note: PersonalNotes encryption is configured automatically via [Encrypted] attribute
            
            // Audit fields (inherited from IAuditableEntity)
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.CreatedBy).HasMaxLength(450); // Identity ID length
            entity.Property(e => e.LastModifiedAt);
            entity.Property(e => e.LastModifiedBy).HasMaxLength(450);
            
            // Soft delete (inherited from ISoftDelete)
            entity.Property(e => e.IsDeleted).IsRequired().HasDefaultValue(false);
            entity.HasIndex(e => e.IsDeleted);
            
            // Additional indexes for performance
            entity.HasIndex(e => e.Email).IsUnique().HasFilter("[Email] IS NOT NULL");
            entity.HasIndex(e => e.UserName).IsUnique().HasFilter("[UserName] IS NOT NULL");
        });

        // 🔥 FIXED: Only configure tables that exist with AddIdentityApiEndpoints
        // Remove role-related table configurations since we're not using full Identity
        
        // Customize core Identity table names (optional)
        modelBuilder.Entity<User>().ToTable("Users");
        modelBuilder.Entity<IdentityUserClaim<string>>().ToTable("UserClaims");
        modelBuilder.Entity<IdentityUserLogin<string>>().ToTable("UserLogins");
        modelBuilder.Entity<IdentityUserToken<string>>().ToTable("UserTokens");
        
        // 🚫 REMOVED: These tables don't exist with AddIdentityApiEndpoints
        // modelBuilder.Entity<IdentityRole>().ToTable("Roles");
        // modelBuilder.Entity<IdentityUserRole<string>>().ToTable("UserRoles");
        // modelBuilder.Entity<IdentityRoleClaim<string>>().ToTable("RoleClaims");
    }

    /// <summary>
    /// Gets the current user ID from the HTTP context
    /// </summary>
    protected override string? GetCurrentUserId()
    {
        // Get user ID from the current HTTP context
        var user = _httpContextAccessor?.HttpContext?.User;
        if (user?.Identity?.IsAuthenticated == true)
        {
            return user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        }
        
        // Fallback for system operations
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
            .HasMaxLength(450); // Match Identity ID length

        builder.Property(t => t.LastModifiedAt);

        builder.Property(t => t.LastModifiedBy)
            .HasMaxLength(450); // Match Identity ID length

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
        
        // Index for user-specific queries
        builder.HasIndex(t => t.CreatedBy);
    }
}