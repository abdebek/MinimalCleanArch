using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using MinimalCleanArch.DataAccess;
using MinimalCleanArch.Domain.Entities;
using MinimalCleanArch.Execution;

namespace MinimalCleanArch.UnitTests.DataAccess;

public class TenantQueryFilterTests
{
    [Fact]
    public async Task TenantA_CannotRead_TenantB_Rows()
    {
        var dbName = Guid.NewGuid().ToString("N");
        var tenantA = new MutableExecutionContext { TenantId = "tenant-a" };
        var tenantB = new MutableExecutionContext { TenantId = "tenant-b" };

        await using (var contextA = CreateContext(dbName, tenantA))
        {
            contextA.Notes.Add(new TenantNote { Title = "secret-A" });
            await contextA.SaveChangesAsync();
        }

        await using (var contextB = CreateContext(dbName, tenantB))
        {
            contextB.Notes.Add(new TenantNote { Title = "secret-B" });
            await contextB.SaveChangesAsync();
        }

        await using (var contextA = CreateContext(dbName, tenantA))
        {
            var titles = await contextA.Notes.Select(n => n.Title).ToListAsync();
            titles.Should().Equal("secret-A");
        }

        await using (var contextB = CreateContext(dbName, tenantB))
        {
            var titles = await contextB.Notes.Select(n => n.Title).ToListAsync();
            titles.Should().Equal("secret-B");
        }
    }

    [Fact]
    public async Task Filter_IsEvaluatedPerContext_NotPinnedAtModelCompile()
    {
        var dbName = Guid.NewGuid().ToString("N");
        var tenantA = new MutableExecutionContext { TenantId = "tenant-a" };
        var tenantB = new MutableExecutionContext { TenantId = "tenant-b" };

        await using var first = CreateContext(dbName, tenantA);
        first.Notes.Add(new TenantNote { Title = "from-a" });
        await first.SaveChangesAsync();

        await using var second = CreateContext(dbName, tenantB);
        second.Notes.Add(new TenantNote { Title = "from-b" });
        await second.SaveChangesAsync();

        (await first.Notes.Select(n => n.Title).ToListAsync()).Should().Equal("from-a");
        (await second.Notes.Select(n => n.Title).ToListAsync()).Should().Equal("from-b");
    }

    [Fact]
    public async Task SoftDelete_StillHidesDeletedRows_ForCurrentTenant()
    {
        var dbName = Guid.NewGuid().ToString("N");
        var tenant = new MutableExecutionContext { TenantId = "tenant-a" };

        await using (var context = CreateContext(dbName, tenant))
        {
            context.Notes.Add(new TenantNote { Title = "keep" });
            context.Notes.Add(new TenantNote { Title = "drop", IsDeleted = true });
            await context.SaveChangesAsync();
        }

        await using (var context = CreateContext(dbName, tenant))
        {
            var titles = await context.Notes.Select(n => n.Title).ToListAsync();
            titles.Should().Equal("keep");
            (await context.Notes.IgnoreQueryFilters().CountAsync()).Should().Be(2);
        }
    }

    [Fact]
    public async Task IgnoreSoftDelete_DoesNotExposeOtherTenants()
    {
        var dbName = Guid.NewGuid().ToString("N");
        var tenantA = new MutableExecutionContext { TenantId = "tenant-a" };
        var tenantB = new MutableExecutionContext { TenantId = "tenant-b" };

        await using (var context = CreateContext(dbName, tenantA))
        {
            context.Notes.Add(new TenantNote { Title = "a-live" });
            context.Notes.Add(new TenantNote { Title = "a-dead", IsDeleted = true });
            await context.SaveChangesAsync();
        }

        await using (var context = CreateContext(dbName, tenantB))
        {
            context.Notes.Add(new TenantNote { Title = "b-dead", IsDeleted = true });
            await context.SaveChangesAsync();
        }

        await using (var context = CreateContext(dbName, tenantA))
        {
            var titles = await context.Notes.IgnoreSoftDelete().Select(n => n.Title).ToListAsync();
            titles.Should().BeEquivalentTo("a-live", "a-dead");
        }
    }

    [Fact]
    public async Task Insert_WithoutTenant_Throws()
    {
        var dbName = Guid.NewGuid().ToString("N");
        await using var context = CreateContext(dbName, new MutableExecutionContext());

        context.Notes.Add(new TenantNote { Title = "orphan" });
        var act = async () => await context.SaveChangesAsync();

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*without a tenant*");
    }

    [Fact]
    public async Task Insert_WithExplicitTenantId_DoesNotRequireContextTenant()
    {
        var dbName = Guid.NewGuid().ToString("N");
        await using (var context = CreateContext(dbName, new MutableExecutionContext()))
        {
            context.Notes.Add(new TenantNote { Title = "seeded", TenantId = "tenant-a" });
            await context.SaveChangesAsync();
        }

        await using (var context = CreateContext(dbName, new MutableExecutionContext { TenantId = "tenant-a" }))
        {
            (await context.Notes.Select(n => n.Title).ToListAsync()).Should().Equal("seeded");
        }

        await using (var context = CreateContext(dbName, new MutableExecutionContext { TenantId = "tenant-b" }))
        {
            (await context.Notes.ToListAsync()).Should().BeEmpty();
        }
    }

    [Fact]
    public async Task UnauthenticatedContext_SeesNoTenantRows()
    {
        var dbName = Guid.NewGuid().ToString("N");
        await using (var context = CreateContext(dbName, new MutableExecutionContext { TenantId = "tenant-a" }))
        {
            context.Notes.Add(new TenantNote { Title = "hidden" });
            await context.SaveChangesAsync();
        }

        await using var anonymous = CreateContext(dbName, new MutableExecutionContext());
        (await anonymous.Notes.ToListAsync()).Should().BeEmpty();
    }

    [Fact]
    public async Task IdentityContext_AppliesTheSameTenantFilter()
    {
        var dbName = Guid.NewGuid().ToString("N");
        var tenantA = new MutableExecutionContext { TenantId = "tenant-a" };
        var tenantB = new MutableExecutionContext { TenantId = "tenant-b" };

        await using (var context = CreateIdentityContext(dbName, tenantA))
        {
            context.Notes.Add(new TenantNote { Title = "id-a" });
            await context.SaveChangesAsync();
        }

        await using (var context = CreateIdentityContext(dbName, tenantB))
        {
            (await context.Notes.ToListAsync()).Should().BeEmpty();
            context.Notes.Add(new TenantNote { Title = "id-b" });
            await context.SaveChangesAsync();
        }

        await using (var context = CreateIdentityContext(dbName, tenantA))
        {
            (await context.Notes.Select(n => n.Title).ToListAsync()).Should().Equal("id-a");
        }
    }

    private static TestTenantDbContext CreateContext(string dbName, IExecutionContext executionContext)
    {
        var options = new DbContextOptionsBuilder<TestTenantDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new TestTenantDbContext(options, executionContext);
    }

    private static TestTenantIdentityDbContext CreateIdentityContext(string dbName, IExecutionContext executionContext)
    {
        var options = new DbContextOptionsBuilder<TestTenantIdentityDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new TestTenantIdentityDbContext(options, executionContext);
    }

    private sealed class TestTenantDbContext : DbContextBase
    {
        public TestTenantDbContext(DbContextOptions options, IExecutionContext executionContext)
            : base(options, executionContext)
        {
        }

        public DbSet<TenantNote> Notes => Set<TenantNote>();
    }

    private sealed class TestTenantIdentityDbContext : IdentityDbContextBase<TestIdentityUser>
    {
        public TestTenantIdentityDbContext(DbContextOptions options, IExecutionContext executionContext)
            : base(options, executionContext)
        {
        }

        public DbSet<TenantNote> Notes => Set<TenantNote>();
    }

    private sealed class TestIdentityUser : Microsoft.AspNetCore.Identity.IdentityUser;

    private sealed class TenantNote : BaseSoftDeleteEntity, ITenantEntity
    {
        public string Title { get; set; } = string.Empty;
        public string TenantId { get; set; } = string.Empty;
    }

    private sealed class MutableExecutionContext : IExecutionContext
    {
        public string? UserId { get; set; }
        public string? UserName { get; set; }
        public string? TenantId { get; set; }
        public string? CorrelationId { get; set; }
        public string? ClientIpAddress { get; set; }
        public string? UserAgent { get; set; }
        public IReadOnlyDictionary<string, string> Metadata { get; } = new Dictionary<string, string>();
    }
}
