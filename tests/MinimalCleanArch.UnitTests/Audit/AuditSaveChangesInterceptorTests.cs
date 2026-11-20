using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using MinimalCleanArch.Audit.Configuration;
using MinimalCleanArch.Audit.Entities;
using MinimalCleanArch.Audit.Extensions;
using MinimalCleanArch.Audit.Interceptors;
using MinimalCleanArch.Audit.Services;
using MinimalCleanArch.Domain.Entities;

namespace MinimalCleanArch.UnitTests.Audit;

public class AuditSaveChangesInterceptorTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly FixedTimeProvider _timeProvider;
    private readonly AuditTestDbContext _dbContext;

    public AuditSaveChangesInterceptorTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        _timeProvider = new FixedTimeProvider(new DateTimeOffset(2026, 11, 20, 12, 0, 0, TimeSpan.Zero));
        var interceptor = new AuditSaveChangesInterceptor(
            new StaticAuditContextProvider(),
            new AuditOptions { Enabled = true },
            _timeProvider);

        var options = new DbContextOptionsBuilder<AuditTestDbContext>()
            .UseSqlite(_connection)
            .AddInterceptors(interceptor)
            .Options;

        _dbContext = new AuditTestDbContext(options);
        _dbContext.Database.EnsureCreated();
    }

    [Fact]
    public async Task SaveChangesAsync_OnCreateWithDatabaseGeneratedKey_AuditsResolvedEntityIdAndTimestamp()
    {
        var item = new AuditedItem { Name = "First" };
        _dbContext.Items.Add(item);

        await _dbContext.SaveChangesAsync();

        item.Id.Should().BeGreaterThan(0);

        var logs = await _dbContext.AuditLogs.ToListAsync();
        logs.Should().HaveCount(1);
        logs[0].Operation.Should().Be(AuditOperation.Create);
        logs[0].EntityType.Should().Be(nameof(AuditedItem));
        logs[0].EntityId.Should().Be(item.Id.ToString());
        logs[0].Timestamp.Should().Be(_timeProvider.GetUtcNow().UtcDateTime);
        logs[0].UserId.Should().Be("user-1");
        logs[0].TenantId.Should().Be("tenant-1");
        logs[0].NewValues.Should().Contain("\"Name\":\"First\"");
        logs[0].NewValues.Should().Contain($"\"Id\":{item.Id}");
    }

    [Fact]
    public void SaveChanges_SyncOnCreateWithDatabaseGeneratedKey_AuditsResolvedEntityId()
    {
        var item = new AuditedItem { Name = "SyncItem" };
        _dbContext.Items.Add(item);

        _dbContext.SaveChanges();

        var logs = _dbContext.AuditLogs.ToList();
        logs.Should().HaveCount(1);
        logs[0].Operation.Should().Be(AuditOperation.Create);
        logs[0].EntityId.Should().Be(item.Id.ToString());
    }

    [Fact]
    public async Task SaveChangesAsync_UpdateAndSoftDelete_RecordsOperations()
    {
        var item = new AuditedItem { Name = "Initial" };
        _dbContext.Items.Add(item);
        await _dbContext.SaveChangesAsync();

        item.Name = "Renamed";
        await _dbContext.SaveChangesAsync();

        item.IsDeleted = true;
        await _dbContext.SaveChangesAsync();

        var logs = await _dbContext.AuditLogs.OrderBy(l => l.Id).ToListAsync();
        logs.Select(l => l.Operation).Should().ContainInOrder(
            AuditOperation.Create,
            AuditOperation.Update,
            AuditOperation.SoftDelete);
    }

    public void Dispose()
    {
        _dbContext.Dispose();
        _connection.Dispose();
    }

    private sealed class AuditedItem : BaseSoftDeleteEntity
    {
        public string Name { get; set; } = string.Empty;
    }

    private sealed class AuditTestDbContext(DbContextOptions<AuditTestDbContext> options) : DbContext(options)
    {
        public DbSet<AuditedItem> Items => Set<AuditedItem>();
        public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.UseAuditLog();
        }
    }

    private sealed class StaticAuditContextProvider : IAuditContextProvider
    {
        public string? GetUserId() => "user-1";
        public string? GetUserName() => "Alice";
        public string? GetTenantId() => "tenant-1";
        public string? GetCorrelationId() => "corr-1";
        public string? GetClientIpAddress() => "127.0.0.1";
        public string? GetUserAgent() => "TestAgent";
        public IDictionary<string, object>? GetMetadata() => null;
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
