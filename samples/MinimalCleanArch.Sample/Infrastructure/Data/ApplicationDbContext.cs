using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MinimalCleanArch.Sample.Domain.Entities;
using MinimalCleanArch.Security.Encryption;
using MinimalCleanArch.Security.EntityEncryption;
using System.Security.Claims;
using System.Linq.Expressions;
using MinimalCleanArch.Domain.Entities;
using MinimalCleanArch.Audit.Configuration;
using MinimalCleanArch.Audit.Entities;
using MinimalCleanArch.Audit.Extensions;

namespace MinimalCleanArch.Sample.Infrastructure.Data;

/// <summary>
/// Application DbContext with Identity API endpoints and MinimalCleanArch features
/// </summary>
public class ApplicationDbContext : IdentityDbContext<User, IdentityRole, string>
{
    private readonly IEncryptionService _encryptionService;
    private readonly IServiceProvider _serviceProvider;
    private readonly IHttpContextAccessor? _httpContextAccessor;
    private readonly AuditOptions? _auditOptions;

    /// <summary>
    /// Initializes a new instance of the <see cref="ApplicationDbContext"/> class
    /// </summary>
    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options,
        IEncryptionService encryptionService,
        IServiceProvider serviceProvider,
        IHttpContextAccessor? httpContextAccessor = null,
        AuditOptions? auditOptions = null)
        : base(options)
    {
        _encryptionService = encryptionService;
        _serviceProvider = serviceProvider;
        _httpContextAccessor = httpContextAccessor;
        _auditOptions = auditOptions;
    }

    // Application entities
    public DbSet<Todo> Todos => Set<Todo>();

    // Audit logs (opt-in via AddAuditLogging)
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    /// <summary>
    /// Configures the model
    /// </summary>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply soft delete query filters
        ApplySoftDeleteQueryFilters(modelBuilder);

        // Configure Identity entities with custom table names
        ConfigureIdentityEntities(modelBuilder);

        // Configure application entities
        modelBuilder.ApplyConfiguration(new TodoConfiguration());

        // Apply encryption with enhanced logging
        modelBuilder.UseEncryption(_encryptionService, _serviceProvider);

        // Configure audit log table (only if audit logging is enabled)
        if (_auditOptions != null)
        {
            modelBuilder.UseAuditLog(_auditOptions);
        }
    }

    /// <summary>
    /// Applies soft delete query filters to all entities that implement ISoftDelete
    /// </summary>
    private static void ApplySoftDeleteQueryFilters(ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(ISoftDelete).IsAssignableFrom(entityType.ClrType))
            {
                var parameter = Expression.Parameter(entityType.ClrType, "p");
                var property = Expression.Property(parameter, nameof(ISoftDelete.IsDeleted));
                var condition = Expression.Equal(property, Expression.Constant(false));
                var lambda = Expression.Lambda(condition, parameter);

                modelBuilder.Entity(entityType.ClrType).HasQueryFilter(lambda);
            }
        }
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
            
            // Audit fields (inherited from IAuditableEntity)
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.CreatedBy).HasMaxLength(450);
            entity.Property(e => e.LastModifiedAt);
            entity.Property(e => e.LastModifiedBy).HasMaxLength(450);
            
            // Soft delete (inherited from ISoftDelete)
            entity.Property(e => e.IsDeleted).IsRequired().HasDefaultValue(false);
            entity.HasIndex(e => e.IsDeleted);
            
            // Additional indexes for performance
            entity.HasIndex(e => e.Email).IsUnique().HasFilter("[Email] IS NOT NULL");
            entity.HasIndex(e => e.UserName).IsUnique().HasFilter("[UserName] IS NOT NULL");
            entity.HasIndex(e => new { e.IsDeleted, e.EmailConfirmed });
        });

        // Configure Identity tables with custom names
        modelBuilder.Entity<User>().ToTable("Users");
        modelBuilder.Entity<IdentityRole>().ToTable("Roles");
        modelBuilder.Entity<IdentityUserRole<string>>().ToTable("UserRoles");
        modelBuilder.Entity<IdentityUserClaim<string>>().ToTable("UserClaims");
        modelBuilder.Entity<IdentityUserLogin<string>>().ToTable("UserLogins");
        modelBuilder.Entity<IdentityUserToken<string>>().ToTable("UserTokens");
        modelBuilder.Entity<IdentityRoleClaim<string>>().ToTable("RoleClaims");

        // Configure additional indexes
        modelBuilder.Entity<IdentityRole>(entity =>
        {
            entity.HasIndex(e => e.NormalizedName).IsUnique().HasFilter("[NormalizedName] IS NOT NULL");
        });

        modelBuilder.Entity<IdentityUserRole<string>>(entity =>
        {
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.RoleId);
        });

        modelBuilder.Entity<IdentityUserClaim<string>>(entity =>
        {
            entity.HasIndex(e => e.UserId);
        });

        modelBuilder.Entity<IdentityUserLogin<string>>(entity =>
        {
            entity.HasIndex(e => e.UserId);
        });

        modelBuilder.Entity<IdentityUserToken<string>>(entity =>
        {
            entity.HasIndex(e => e.UserId);
        });

        modelBuilder.Entity<IdentityRoleClaim<string>>(entity =>
        {
            entity.HasIndex(e => e.RoleId);
        });
    }

    /// <summary>
    /// Saves all changes made in this context to the database with audit information
    /// </summary>
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ApplyAuditInfo();
        return await base.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Saves all changes made in this context to the database with audit information
    /// </summary>
    public override int SaveChanges()
    {
        ApplyAuditInfo();
        return base.SaveChanges();
    }

    /// <summary>
    /// Applies audit information to entities that implement IAuditableEntity
    /// </summary>
    private void ApplyAuditInfo()
    {
        var entries = ChangeTracker.Entries()
            .Where(e => e.Entity is IAuditableEntity && 
                       (e.State == EntityState.Added || e.State == EntityState.Modified))
            .ToList();

        if (!entries.Any())
            return;

        string? userId = GetCurrentUserId();
        DateTime now = DateTime.UtcNow;

        foreach (var entityEntry in entries)
        {
            if (entityEntry.Entity is IAuditableEntity auditableEntity)
            {
                if (entityEntry.State == EntityState.Added)
                {
                    auditableEntity.CreatedAt = now;
                    auditableEntity.CreatedBy = userId;
                }
                else if (entityEntry.State == EntityState.Modified)
                {
                    Entry(auditableEntity).Property(x => x.CreatedAt).IsModified = false;
                    Entry(auditableEntity).Property(x => x.CreatedBy).IsModified = false;
                }

                auditableEntity.LastModifiedAt = now;
                auditableEntity.LastModifiedBy = userId;
            }
        }
    }

    /// <summary>
    /// Gets the current user ID from the HTTP context
    /// </summary>
    private string? GetCurrentUserId()
    {
        var user = _httpContextAccessor?.HttpContext?.User;
        if (user?.Identity?.IsAuthenticated == true)
        {
            return user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        }
        
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
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(t => t.Priority)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(t => t.DueDate);

        // Audit fields
        builder.Property(t => t.CreatedAt).IsRequired();
        builder.Property(t => t.CreatedBy).HasMaxLength(450);
        builder.Property(t => t.LastModifiedAt);
        builder.Property(t => t.LastModifiedBy).HasMaxLength(450);

        // Soft delete
        builder.Property(t => t.IsDeleted)
            .IsRequired()
            .HasDefaultValue(false);

        // Indexes for better query performance
        builder.HasIndex(t => t.IsCompleted);
        builder.HasIndex(t => t.Priority);
        builder.HasIndex(t => t.DueDate);
        builder.HasIndex(t => t.IsDeleted);
        builder.HasIndex(t => new { t.IsCompleted, t.Priority, t.IsDeleted });
        builder.HasIndex(t => t.CreatedBy);
        builder.HasIndex(t => t.CreatedAt);
    }
}