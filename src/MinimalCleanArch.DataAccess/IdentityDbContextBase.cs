using System.Linq.Expressions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using MinimalCleanArch.Domain.Entities;

namespace MinimalCleanArch.DataAccess;

/// <summary>
/// Base Identity DbContext with support for auditing and soft delete (User only, no roles)
/// Use this for simple scenarios where you don't need role-based authorization
/// </summary>
/// <typeparam name="TUser">The type of the user entity</typeparam>
public abstract class IdentityDbContextBase<TUser> : IdentityDbContext<TUser> 
    where TUser : IdentityUser
{
    /// <summary>
    /// Initializes a new instance of the <see cref="IdentityDbContextBase{TUser}"/> class
    /// </summary>
    /// <param name="options">The options to be used by the DbContext</param>
    protected IdentityDbContextBase(DbContextOptions options) 
        : base(options)
    {
    }

    /// <summary>
    /// Configures the model
    /// </summary>
    /// <param name="modelBuilder">The builder being used to construct the model for this context</param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply global query filter for soft delete to all entities that implement ISoftDelete
        ApplySoftDeleteQueryFilters(modelBuilder);
        
        // Configure Identity entities if they implement our interfaces
        ConfigureIdentityEntitiesIfNeeded(modelBuilder);
    }

    /// <summary>
    /// Applies soft delete query filters to all entities that implement ISoftDelete
    /// </summary>
    /// <param name="modelBuilder">The model builder</param>
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

    /// <summary>
    /// Configures Identity entities if they implement our custom interfaces
    /// </summary>
    /// <param name="modelBuilder">The model builder</param>
    private void ConfigureIdentityEntitiesIfNeeded(ModelBuilder modelBuilder)
    {
        // Configure User entity if it implements our interfaces
        if (typeof(IAuditableEntity).IsAssignableFrom(typeof(TUser)))
        {
            modelBuilder.Entity<TUser>(entity =>
            {
                entity.Property("CreatedAt").IsRequired();
                entity.Property("CreatedBy").HasMaxLength(450); // Standard Identity ID length
                entity.Property("LastModifiedAt");
                entity.Property("LastModifiedBy").HasMaxLength(450);
            });
        }

        if (typeof(ISoftDelete).IsAssignableFrom(typeof(TUser)))
        {
            modelBuilder.Entity<TUser>(entity =>
            {
                entity.Property("IsDeleted").IsRequired().HasDefaultValue(false);
                entity.HasIndex("IsDeleted");
            });
        }
    }

    /// <summary>
    /// Saves all changes made in this context to the database
    /// </summary>
    /// <param name="cancellationToken">A token to observe while waiting for the task to complete</param>
    /// <returns>A task that represents the asynchronous save operation</returns>
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ApplyAuditInfo();
        return await base.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Saves all changes made in this context to the database
    /// </summary>
    /// <returns>The number of state entries written to the database</returns>
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
            .ToList(); // Materialize to avoid multiple enumeration

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
                    // Prevent modification of creation audit fields
                    Entry(auditableEntity).Property(x => x.CreatedAt).IsModified = false;
                    Entry(auditableEntity).Property(x => x.CreatedBy).IsModified = false;
                }

                // Always update modification fields
                auditableEntity.LastModifiedAt = now;
                auditableEntity.LastModifiedBy = userId;
            }
        }
    }

    /// <summary>
    /// Gets the current user ID from the application context
    /// </summary>
    /// <returns>The current user ID</returns>
    protected virtual string? GetCurrentUserId()
    {
        // This should be overridden in derived classes to get the actual user ID
        // from the application's authentication system
        return null;
    }
}

/// <summary>
/// Extended version that supports roles and additional Identity entities with custom key types
/// Use this when you need custom role entities or non-string keys
/// </summary>
/// <typeparam name="TUser">The type of the user entity</typeparam>
/// <typeparam name="TRole">The type of the role entity</typeparam>
/// <typeparam name="TKey">The type of the primary key</typeparam>
public abstract class IdentityDbContextBase<TUser, TRole, TKey> : IdentityDbContext<TUser, TRole, TKey>
    where TUser : IdentityUser<TKey>
    where TRole : IdentityRole<TKey>
    where TKey : IEquatable<TKey>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="IdentityDbContextBase{TUser, TRole, TKey}"/> class
    /// </summary>
    /// <param name="options">The options to be used by the DbContext</param>
    protected IdentityDbContextBase(DbContextOptions options) 
        : base(options)
    {
    }

    /// <summary>
    /// Configures the model
    /// </summary>
    /// <param name="modelBuilder">The builder being used to construct the model for this context</param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply global query filter for soft delete
        ApplySoftDeleteQueryFilters(modelBuilder);

        // Configure Identity entities if they implement our interfaces
        ConfigureIdentityEntitiesIfNeeded(modelBuilder);
        
        // Configure Identity tables with optimized indexes
        ConfigureIdentityTablesOptimized(modelBuilder);
    }

    /// <summary>
    /// Applies soft delete query filters to all entities that implement ISoftDelete
    /// </summary>
    /// <param name="modelBuilder">The model builder</param>
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

    /// <summary>
    /// Configures Identity entities if they implement our custom interfaces
    /// </summary>
    /// <param name="modelBuilder">The model builder</param>
    private void ConfigureIdentityEntitiesIfNeeded(ModelBuilder modelBuilder)
    {
        // Configure User entity
        if (typeof(IAuditableEntity).IsAssignableFrom(typeof(TUser)))
        {
            modelBuilder.Entity<TUser>(entity =>
            {
                entity.Property("CreatedAt").IsRequired();
                entity.Property("CreatedBy").HasMaxLength(450);
                entity.Property("LastModifiedAt");
                entity.Property("LastModifiedBy").HasMaxLength(450);
                
                // Add indexes for audit queries
                entity.HasIndex("CreatedAt");
                entity.HasIndex("CreatedBy");
            });
        }

        if (typeof(ISoftDelete).IsAssignableFrom(typeof(TUser)))
        {
            modelBuilder.Entity<TUser>(entity =>
            {
                entity.Property("IsDeleted").IsRequired().HasDefaultValue(false);
                entity.HasIndex("IsDeleted");
            });
        }

        // Configure Role entity
        if (typeof(IAuditableEntity).IsAssignableFrom(typeof(TRole)))
        {
            modelBuilder.Entity<TRole>(entity =>
            {
                entity.Property("CreatedAt").IsRequired();
                entity.Property("CreatedBy").HasMaxLength(450);
                entity.Property("LastModifiedAt");
                entity.Property("LastModifiedBy").HasMaxLength(450);
                
                // Add indexes for audit queries
                entity.HasIndex("CreatedAt");
                entity.HasIndex("CreatedBy");
            });
        }

        if (typeof(ISoftDelete).IsAssignableFrom(typeof(TRole)))
        {
            modelBuilder.Entity<TRole>(entity =>
            {
                entity.Property("IsDeleted").IsRequired().HasDefaultValue(false);
                entity.HasIndex("IsDeleted");
            });
        }
    }

    /// <summary>
    /// Configures Identity tables with performance optimizations
    /// </summary>
    /// <param name="modelBuilder">The model builder</param>
    private static void ConfigureIdentityTablesOptimized(ModelBuilder modelBuilder)
    {
        // Configure additional indexes for performance
        modelBuilder.Entity<TUser>(entity =>
        {
            entity.HasIndex(e => e.Email).IsUnique().HasFilter("[Email] IS NOT NULL");
            entity.HasIndex(e => e.UserName).IsUnique().HasFilter("[UserName] IS NOT NULL");
            entity.HasIndex(e => e.NormalizedEmail).HasFilter("[NormalizedEmail] IS NOT NULL");
            entity.HasIndex(e => e.NormalizedUserName).IsUnique().HasFilter("[NormalizedUserName] IS NOT NULL");
        });

        modelBuilder.Entity<TRole>(entity =>
        {
            entity.HasIndex(e => e.NormalizedName).IsUnique().HasFilter("[NormalizedName] IS NOT NULL");
        });

        // Configure relationship tables with indexes
        modelBuilder.Entity<IdentityUserRole<TKey>>(entity =>
        {
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.RoleId);
        });

        modelBuilder.Entity<IdentityUserClaim<TKey>>(entity =>
        {
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => new { e.UserId, e.ClaimType });
            entity.HasIndex(e => new { e.ClaimType, e.ClaimValue });
        });

        modelBuilder.Entity<IdentityUserLogin<TKey>>(entity =>
        {
            entity.HasIndex(e => e.UserId);
        });

        modelBuilder.Entity<IdentityUserToken<TKey>>(entity =>
        {
            entity.HasIndex(e => e.UserId);
        });

        modelBuilder.Entity<IdentityRoleClaim<TKey>>(entity =>
        {
            entity.HasIndex(e => e.RoleId);
            entity.HasIndex(e => new { e.RoleId, e.ClaimType });
            entity.HasIndex(e => new { e.ClaimType, e.ClaimValue });
        });
    }

    /// <summary>
    /// Saves all changes made in this context to the database
    /// </summary>
    /// <param name="cancellationToken">A token to observe while waiting for the task to complete</param>
    /// <returns>A task that represents the asynchronous save operation</returns>
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ApplyAuditInfo();
        return await base.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Saves all changes made in this context to the database
    /// </summary>
    /// <returns>The number of state entries written to the database</returns>
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
    /// Gets the current user ID from the application context
    /// </summary>
    /// <returns>The current user ID</returns>
    protected virtual string? GetCurrentUserId()
    {
        return null;
    }
}