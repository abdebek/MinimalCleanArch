using Microsoft.EntityFrameworkCore;
using MinimalCleanArch.EntityFramework.Repositories;
using MinimalCleanArch.Sample.Domain.Entities;
using MinimalCleanArch.Sample.Infrastructure.Data;
using MinimalCleanArch.Security.Encryption;
using Moq;
using FluentAssertions;

namespace MinimalCleanArch.UnitTests.Repositories;

public class RepositoryTests
{
    private readonly DbContextOptions<ApplicationDbContext> _options;
    private readonly Mock<IEncryptionService> _encryptionServiceMock;

    public RepositoryTests()
    {
        _options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: $"TestDb_{Guid.NewGuid()}")
            .Options;

        _encryptionServiceMock = new Mock<IEncryptionService>();
        _encryptionServiceMock.Setup(x => x.Encrypt(It.IsAny<string>())).Returns<string>(s => s);
        _encryptionServiceMock.Setup(x => x.Decrypt(It.IsAny<string>())).Returns<string>(s => s);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnEntity_WhenEntityExists()
    {
        // Arrange
        var testId = 1;
        var testTitle = "Test Todo";

        using (var context = new ApplicationDbContext(_options, _encryptionServiceMock.Object))
        {
            context.Todos.Add(new Todo(testTitle, "Description"));
            await context.SaveChangesAsync();
        }

        // Act
        using (var context = new ApplicationDbContext(_options, _encryptionServiceMock.Object))
        {
            var repository = new Repository<Todo>(context);
            var result = await repository.GetByIdAsync(testId);

            // Assert
            result.Should().NotBeNull();
            result!.Title.Should().Be(testTitle);
        }
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnNull_WhenEntityDoesNotExist()
    {
        // Arrange
        var testId = 99;

        // Act
        using (var context = new ApplicationDbContext(_options, _encryptionServiceMock.Object))
        {
            var repository = new Repository<Todo>(context);
            var result = await repository.GetByIdAsync(testId);

            // Assert
            result.Should().BeNull();
        }
    }

    [Fact]
    public async Task AddAsync_ShouldAddEntity()
    {
        // Arrange
        var testTitle = "New Todo";
        var todo = new Todo(testTitle, "Description");

        // Act
        using (var context = new ApplicationDbContext(_options, _encryptionServiceMock.Object))
        {
            var repository = new Repository<Todo>(context);
            await repository.AddAsync(todo);
        }

        // Assert
        using (var context = new ApplicationDbContext(_options, _encryptionServiceMock.Object))
        {
            context.Todos.Count().Should().Be(1);
            context.Todos.First().Title.Should().Be(testTitle);
        }
    }

    [Fact]
    public async Task DeleteAsync_ShouldSoftDeleteEntity_WhenEntityImplementsISoftDelete()
    {
        // Arrange
        var testId = 1;
        var testTitle = "Test Todo";

        using (var context = new ApplicationDbContext(_options, _encryptionServiceMock.Object))
        {
            context.Todos.Add(new Todo(testTitle, "Description"));
            await context.SaveChangesAsync();
        }

        // Act
        using (var context = new ApplicationDbContext(_options, _encryptionServiceMock.Object))
        {
            var repository = new Repository<Todo>(context);
            var entity = await repository.GetByIdAsync(testId);
            await repository.DeleteAsync(entity!);
        }

        // Assert
        using (var context = new ApplicationDbContext(_options, _encryptionServiceMock.Object))
        {
            // Regular query should not find the entity due to soft delete
            var todos = await context.Todos.ToListAsync();
            todos.Should().BeEmpty();

            // But it should still exist in the database with IsDeleted = true
            var todoWithDeleted = await context.Todos.IgnoreQueryFilters().FirstOrDefaultAsync(t => t.Id == testId);
            todoWithDeleted.Should().NotBeNull();
            todoWithDeleted!.IsDeleted.Should().BeTrue();
        }
    }
}
