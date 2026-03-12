using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using MinimalCleanArch.DataAccess;

namespace MinimalCleanArch.UnitTests.DataAccess;

public class IdentityDbContextBaseTests
{
    [Fact]
    public void GenericIdentityContext_UsesSqlServerFilters_ForNullableIdentityIndexes()
    {
        using var context = new TestIdentityDbContext(CreateSqlServerOptions<TestIdentityDbContext>());

        GetIndexFilter(context, typeof(TestUser), "EmailIndex")
            .Should().Be("[NormalizedEmail] IS NOT NULL");
        GetIndexFilter(context, typeof(TestUser), "UserNameIndex")
            .Should().Be("[NormalizedUserName] IS NOT NULL");
        GetIndexFilter(context, typeof(TestRole), "RoleNameIndex")
            .Should().Be("[NormalizedName] IS NOT NULL");
    }

    [Fact]
    public void GenericIdentityContext_ClearsSqlServerSpecificFilters_ForPostgres()
    {
        using var context = new TestIdentityDbContext(CreateNpgsqlOptions<TestIdentityDbContext>());

        GetIndexFilter(context, typeof(TestUser), "EmailIndex")
            .Should().BeNull();
        GetIndexFilter(context, typeof(TestUser), "UserNameIndex")
            .Should().BeNull();
        GetIndexFilter(context, typeof(TestRole), "RoleNameIndex")
            .Should().BeNull();
    }

    [Fact]
    public void SimpleIdentityContext_ClearsSqlServerSpecificFilters_ForPostgres()
    {
        using var context = new SimpleTestIdentityDbContext(CreateNpgsqlOptions<SimpleTestIdentityDbContext>());

        GetIndexFilter(context, typeof(SimpleTestUser), "EmailIndex")
            .Should().BeNull();
        GetIndexFilter(context, typeof(SimpleTestUser), "UserNameIndex")
            .Should().BeNull();
        GetIndexFilter(context, typeof(IdentityRole), "RoleNameIndex")
            .Should().BeNull();
    }

    private static DbContextOptions<TContext> CreateSqlServerOptions<TContext>()
        where TContext : DbContext
    {
        return new DbContextOptionsBuilder<TContext>()
            .UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=MinimalCleanArchTests;Trusted_Connection=True;")
            .Options;
    }

    private static DbContextOptions<TContext> CreateNpgsqlOptions<TContext>()
        where TContext : DbContext
    {
        return new DbContextOptionsBuilder<TContext>()
            .UseNpgsql("Host=localhost;Database=minimalcleanarchtests;Username=postgres;Password=postgres")
            .Options;
    }

    private static string? GetIndexFilter(DbContext context, Type entityType, string indexName)
    {
        return context.Model
            .FindEntityType(entityType)!
            .GetIndexes()
            .Single(i => string.Equals(i.GetDatabaseName(), indexName, StringComparison.Ordinal))
            .GetFilter();
    }

    private sealed class TestIdentityDbContext : IdentityDbContextBase<TestUser, TestRole, Guid>
    {
        public TestIdentityDbContext(DbContextOptions<TestIdentityDbContext> options)
            : base(options)
        {
        }
    }

    private sealed class SimpleTestIdentityDbContext : IdentityDbContextBase<SimpleTestUser>
    {
        public SimpleTestIdentityDbContext(DbContextOptions<SimpleTestIdentityDbContext> options)
            : base(options)
        {
        }
    }

    private sealed class TestUser : IdentityUser<Guid>;

    private sealed class TestRole : IdentityRole<Guid>;

    private sealed class SimpleTestUser : IdentityUser;
}
