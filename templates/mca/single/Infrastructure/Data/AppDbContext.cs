using MCA.Domain.Entities;
using Microsoft.EntityFrameworkCore;
#if (UseSecurity)
using MinimalCleanArch.Security.Encryption;
#endif
#if (UseAudit)
using MinimalCleanArch.Audit.Entities;
using MinimalCleanArch.Audit.Extensions;
#endif
#if (UseAuth)
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
#endif

namespace MCA.Infrastructure.Data;

/// <summary>
/// Application database context.
/// </summary>
#if (UseAuth)
public class AppDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>
#else
public class AppDbContext : DbContext
#endif
{
#if (UseSecurity)
    private readonly IEncryptionService? _encryptionService;
#endif

    public DbSet<Todo> Todos => Set<Todo>();
#if (UseAudit)
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
#endif

#if (UseSecurity)
    public AppDbContext(DbContextOptions<AppDbContext> options, IEncryptionService? encryptionService = null)
        : base(options)
    {
        _encryptionService = encryptionService;
    }
#else
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }
#endif

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

#if (UseAuth)
        // Configure OpenIddict entities
        modelBuilder.UseOpenIddict<Guid>();

        // Configure ApplicationUser entity
        modelBuilder.Entity<ApplicationUser>(entity =>
        {
            entity.Property(e => e.FirstName).HasMaxLength(100);
            entity.Property(e => e.LastName).HasMaxLength(100);
        });
#endif

        modelBuilder.Entity<Todo>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.HasQueryFilter(e => !e.IsDeleted);
        });

#if (UseAudit)
        modelBuilder.UseAuditLog();
#endif
    }

    public override int SaveChanges()
    {
        UpdateTimestamps();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        UpdateTimestamps();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void UpdateTimestamps()
    {
        var entries = ChangeTracker.Entries<Todo>()
            .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified);

        foreach (var entry in entries)
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = DateTime.UtcNow;
            }
            entry.Entity.LastModifiedAt = DateTime.UtcNow;
        }
    }
}
