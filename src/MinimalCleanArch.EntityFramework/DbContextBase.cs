using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using MinimalCleanArch.Domain.Entities;

namespace MinimalCleanArch.EntityFramework;

/// <summary>
/// Base DbContext with support for auditing and soft delete
/// </summary>
public abstract class DbContextBase : DbContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DbContextBase"/> class
    /// </summary>
    /// <param name="options">The options to be used by the DbContext</param>
    protected DbContextBase(DbContextOptions options) 
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

    private void ApplyAuditInfo()
    {
        var entries = ChangeTracker.Entries()
            .Where(e => e.Entity is IAuditableEntity && 
                       (e.State == EntityState.Added || e.State == EntityState.Modified));

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
                else
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
        // This should be overridden in derived classes to get the actual user ID
        // from the application's authentication system
        return null;
    }
}
