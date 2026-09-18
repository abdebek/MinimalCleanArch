using MCA.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using MinimalCleanArch.DataAccess;
using MinimalCleanArch.Execution;
#if (UseSecurity)
using MinimalCleanArch.Security.Encryption;
#endif
#if (UseAudit)
using MinimalCleanArch.Audit.Entities;
using MinimalCleanArch.Audit.Extensions;
#endif
#if (UseAuth)
using MCA.Application.Identity;
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
#if (UseAuth && UseMultiTenant)
    public DbSet<Organization> Organizations => Set<Organization>();
    public DbSet<OrganizationMembership> OrganizationMemberships => Set<OrganizationMembership>();
    public DbSet<OrganizationInvitation> OrganizationInvitations => Set<OrganizationInvitation>();
    public DbSet<OrganizationRole> OrganizationRoles => Set<OrganizationRole>();
#endif
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
#if (UseMultiTenant)
            entity.Property(e => e.TenantId).HasMaxLength(64).IsRequired();
            entity.HasIndex(e => e.TenantId);
#endif
        });
#endif

        modelBuilder.Entity<Todo>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(1000);
#if (UseMultiTenant)
            entity.Property(e => e.TenantId).HasMaxLength(64).IsRequired();
            entity.HasIndex(e => e.TenantId);
#endif
        });

#if (UseAuth && UseMultiTenant)
        modelBuilder.Entity<Organization>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
            entity.Property(e => e.TenantId).HasMaxLength(64).IsRequired();
            entity.HasIndex(e => e.TenantId).IsUnique();
        });
        modelBuilder.Entity<OrganizationRole>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(64).IsRequired();
            entity.Property(e => e.TenantId).HasMaxLength(64).IsRequired();
            entity.HasIndex(e => new { e.TenantId, e.Name }).IsUnique();
        });
        modelBuilder.Entity<OrganizationMembership>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Role).HasMaxLength(64).IsRequired();
            entity.Property(e => e.TenantId).HasMaxLength(64).IsRequired();
            entity.HasIndex(e => new { e.OrganizationId, e.UserId }).IsUnique();
        });
        modelBuilder.Entity<OrganizationInvitation>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Code).HasMaxLength(32).IsRequired();
            entity.Property(e => e.Role).HasMaxLength(64).IsRequired();
            entity.Property(e => e.TenantId).HasMaxLength(64).IsRequired();
            entity.Property(e => e.Email).HasMaxLength(256);
            entity.HasIndex(e => e.Code).IsUnique();
        });
#endif

#if (UseAudit)
        modelBuilder.UseAuditLog();
#endif
    }
}
