using MCA.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using MinimalCleanArch.DataAccess;
using MinimalCleanArch.Execution;
#if (UseSecurity)
using MinimalCleanArch.Security.Encryption;
#endif
using MinimalCleanArch.Domain.Entities;
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
public class AppDbContext : IdentityDbContextBase<ApplicationUser, IdentityRole<Guid>, Guid>
#else
public class AppDbContext : DbContextBase
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
    public AppDbContext(
        DbContextOptions<AppDbContext> options,
        IEncryptionService? encryptionService = null,
        IExecutionContext? executionContext = null)
        : base(options, executionContext)
    {
        _encryptionService = encryptionService;
    }
#else
    public AppDbContext(
        DbContextOptions<AppDbContext> options,
        IExecutionContext? executionContext = null)
        : base(options, executionContext)
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

        // Configure Todo entity
        modelBuilder.Entity<Todo>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(1000);
        });

#if (UseAudit)
        modelBuilder.UseAuditLog();
#endif
    }
}
