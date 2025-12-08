using MCA.Domain.Entities;
using Microsoft.EntityFrameworkCore;
#if (UseSecurity)
using MinimalCleanArch.Security.Encryption;
#endif
using MinimalCleanArch.Domain.Entities;

namespace MCA.Infrastructure.Data;

/// <summary>
/// Application database context.
/// </summary>
public class AppDbContext : DbContext
{
#if (UseSecurity)
    private readonly IEncryptionService? _encryptionService;
#endif

    public DbSet<Todo> Todos => Set<Todo>();

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

        // Configure Todo entity
        modelBuilder.Entity<Todo>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.HasQueryFilter(e => !e.IsDeleted);
        });
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
        var entries = ChangeTracker.Entries()
            .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified);

        foreach (var entry in entries)
        {
            if (entry.Entity is IAuditableEntity entity)
            {
                if (entry.State == EntityState.Added)
                {
                    entity.CreatedAt = DateTime.UtcNow;
                }
                entity.LastModifiedAt = DateTime.UtcNow;
            }
        }
    }
}
