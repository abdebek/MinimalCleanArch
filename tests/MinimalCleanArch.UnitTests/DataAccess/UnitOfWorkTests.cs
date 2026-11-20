using System.Data;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using MinimalCleanArch.DataAccess.Repositories;
using MinimalCleanArch.Domain.Entities;

namespace MinimalCleanArch.UnitTests.DataAccess;

public class UnitOfWorkTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly TxDbContext _dbContext;

    public UnitOfWorkTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<TxDbContext>()
            .UseSqlite(_connection)
            .Options;

        _dbContext = new TxDbContext(options);
        _dbContext.Database.EnsureCreated();
    }

    [Fact]
    public async Task ExecuteInTransactionAsync_WithCustomIsolationLevel_CommitsOnRelationalDatabase()
    {
        await using var uow = new UnitOfWork(_dbContext);

        await uow.ExecuteInTransactionAsync(async () =>
        {
            _dbContext.Items.Add(new TxItem { Title = "Committed" });
            await uow.SaveChangesAsync();
        }, IsolationLevel.Serializable);

        uow.HasActiveTransaction.Should().BeFalse();
        (await _dbContext.Items.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task ExecuteInTransactionAsync_WhenExceptionThrown_RollsBackTransaction()
    {
        await using var uow = new UnitOfWork(_dbContext);

        var act = () => uow.ExecuteInTransactionAsync(async () =>
        {
            _dbContext.Items.Add(new TxItem { Title = "RolledBack" });
            await uow.SaveChangesAsync();
            throw new InvalidOperationException("abort");
        });

        await act.Should().ThrowAsync<InvalidOperationException>();
        uow.HasActiveTransaction.Should().BeFalse();
        (await _dbContext.Items.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task SyncDispose_DisposesActiveTransactionWithoutBlocking()
    {
        var uow = new UnitOfWork(_dbContext);
        await uow.BeginTransactionAsync();
        uow.HasActiveTransaction.Should().BeTrue();

        uow.Dispose();

        uow.HasActiveTransaction.Should().BeFalse();
    }

    public void Dispose()
    {
        _dbContext.Dispose();
        _connection.Dispose();
    }

    private sealed class TxItem : BaseEntity
    {
        public string Title { get; set; } = string.Empty;
    }

    private sealed class TxDbContext(DbContextOptions<TxDbContext> options) : DbContext(options)
    {
        public DbSet<TxItem> Items => Set<TxItem>();
    }
}
