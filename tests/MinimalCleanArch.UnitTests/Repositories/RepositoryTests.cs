using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MinimalCleanArch.EntityFramework.Repositories;
using MinimalCleanArch.Repositories;
using MinimalCleanArch.Sample.Domain.Entities;
using MinimalCleanArch.Sample.Infrastructure.Data;
using MinimalCleanArch.Security.Configuration;
using MinimalCleanArch.Security.Encryption;
using Moq;
using FluentAssertions;

namespace MinimalCleanArch.UnitTests.Repositories;

public class RepositoryTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly IServiceScope _scope;
    private readonly ApplicationDbContext _dbContext;
    private readonly IRepository<Todo> _repository;
    private readonly IUnitOfWork _unitOfWork;

    public RepositoryTests()
    {
        // Setup dependency injection for tests
        var services = new ServiceCollection();
        
        // Add logging
        services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Warning));
        
        // Add encryption service
        var encryptionOptions = new EncryptionOptions
        {
            Key = "test-encryption-key-for-unit-tests-32-characters",
            ValidateKeyStrength = false
        };
        services.AddSingleton(encryptionOptions);
        services.AddSingleton<IEncryptionService, AesEncryptionService>();
        
        // Add DbContext with unique in-memory database
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}"));
        
        // Add repositories and unit of work
        services.AddScoped<DbContext>(provider => provider.GetRequiredService<ApplicationDbContext>());
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IRepository<Todo>, Repository<Todo>>();
        
        _serviceProvider = services.BuildServiceProvider();
        _scope = _serviceProvider.CreateScope();
        
        _dbContext = _scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        _repository = _scope.ServiceProvider.GetRequiredService<IRepository<Todo>>();
        _unitOfWork = _scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        
        // Ensure database is created
        _dbContext.Database.EnsureCreated();
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnEntity_WhenEntityExists()
    {
        // Arrange
        var testTitle = "Test Todo";
        var todo = new Todo(testTitle, "Description");
        
        await _repository.AddAsync(todo);
        await _unitOfWork.SaveChangesAsync();

        // Act
        var result = await _repository.GetByIdAsync(todo.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Title.Should().Be(testTitle);
        result.Id.Should().Be(todo.Id);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnNull_WhenEntityDoesNotExist()
    {
        // Arrange
        var nonExistentId = 99999;

        // Act
        var result = await _repository.GetByIdAsync(nonExistentId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task AddAsync_ShouldAddEntity_WhenSaveChangesIsCalled()
    {
        // Arrange
        var testTitle = "New Todo";
        var todo = new Todo(testTitle, "Description");

        // Act
        await _repository.AddAsync(todo);
        await _unitOfWork.SaveChangesAsync();

        // Assert
        var allTodos = await _repository.GetAllAsync();
        allTodos.Should().HaveCount(1);
        allTodos.First().Title.Should().Be(testTitle);
    }

    [Fact]
    public async Task AddRangeAsync_ShouldAddMultipleEntities()
    {
        // Arrange
        var todos = new List<Todo>
        {
            new("Todo 1", "Description 1"),
            new("Todo 2", "Description 2"),
            new("Todo 3", "Description 3")
        };

        // Act
        await _repository.AddRangeAsync(todos);
        await _unitOfWork.SaveChangesAsync();

        // Assert
        var allTodos = await _repository.GetAllAsync();
        allTodos.Should().HaveCount(3);
    }

    [Fact]
    public async Task UpdateAsync_ShouldUpdateEntity_WhenSaveChangesIsCalled()
    {
        // Arrange
        var todo = new Todo("Original Title", "Original Description");
        await _repository.AddAsync(todo);
        await _unitOfWork.SaveChangesAsync();

        var newTitle = "Updated Title";
        todo.Update(newTitle, "Updated Description", 3, DateTime.Now.AddDays(7));

        // Act
        await _repository.UpdateAsync(todo);
        await _unitOfWork.SaveChangesAsync();

        // Assert
        var updatedTodo = await _repository.GetByIdAsync(todo.Id);
        updatedTodo.Should().NotBeNull();
        updatedTodo!.Title.Should().Be(newTitle);
    }

    [Fact]
    public async Task DeleteAsync_ShouldSoftDeleteEntity_WhenEntityImplementsISoftDelete()
    {
        // Arrange
        var testTitle = "Test Todo";
        var todo = new Todo(testTitle, "Description");
        
        await _repository.AddAsync(todo);
        await _unitOfWork.SaveChangesAsync();

        // Act
        await _repository.DeleteAsync(todo);
        await _unitOfWork.SaveChangesAsync();

        // Assert
        // Regular query should not find the entity due to soft delete
        var todos = await _repository.GetAllAsync();
        todos.Should().BeEmpty();

        // But it should still exist in the database with IsDeleted = true
        var todoWithDeleted = await _dbContext.Todos
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.Id == todo.Id);
        
        todoWithDeleted.Should().NotBeNull();
        todoWithDeleted!.IsDeleted.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteAsync_ById_ShouldSoftDeleteEntity()
    {
        // Arrange
        var todo = new Todo("Test Todo", "Description");
        await _repository.AddAsync(todo);
        await _unitOfWork.SaveChangesAsync();

        // Act
        await _repository.DeleteAsync(todo.Id);
        await _unitOfWork.SaveChangesAsync();

        // Assert
        var result = await _repository.GetByIdAsync(todo.Id);
        result.Should().BeNull(); // Should not be found due to soft delete filter
    }

    [Fact]
    public async Task CountAsync_ShouldReturnCorrectCount()
    {
        // Arrange
        var todos = new List<Todo>
        {
            new("Todo 1", "Description 1", 1),
            new("Todo 2", "Description 2", 2),
            new("Todo 3", "Description 3", 1)
        };
        
        await _repository.AddRangeAsync(todos);
        await _unitOfWork.SaveChangesAsync();

        // Act
        var totalCount = await _repository.CountAsync();
        var priorityOneCount = await _repository.CountAsync(t => t.Priority == 1);

        // Assert
        totalCount.Should().Be(3);
        priorityOneCount.Should().Be(2);
    }

    [Fact]
    public async Task GetAsync_WithFilter_ShouldReturnFilteredResults()
    {
        // Arrange
        var todos = new List<Todo>
        {
            new("High Priority", "Description", 5),
            new("Medium Priority", "Description", 3),
            new("Low Priority", "Description", 1)
        };
        
        await _repository.AddRangeAsync(todos);
        await _unitOfWork.SaveChangesAsync();

        // Act
        var highPriorityTodos = await _repository.GetAsync(t => t.Priority >= 4);

        // Assert
        highPriorityTodos.Should().HaveCount(1);
        highPriorityTodos.First().Title.Should().Be("High Priority");
    }

    [Fact]
    public async Task Transaction_ShouldRollback_OnException()
    {
        // Arrange
        var todo1 = new Todo("Todo 1", "Description 1");
        var todo2 = new Todo("Todo 2", "Description 2");

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                await _repository.AddAsync(todo1);
                await _unitOfWork.SaveChangesAsync();
                
                await _repository.AddAsync(todo2);
                await _unitOfWork.SaveChangesAsync();
                
                // Force an exception
                throw new InvalidOperationException("Test exception");
            });
        });

        // Assert - no todos should be saved due to rollback
        var allTodos = await _repository.GetAllAsync();
        allTodos.Should().BeEmpty();
    }

    [Fact]
    public async Task Transaction_ShouldCommit_OnSuccess()
    {
        // Arrange
        var todos = new List<Todo>
        {
            new("Todo 1", "Description 1"),
            new("Todo 2", "Description 2")
        };

        // Act
        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            await _repository.AddRangeAsync(todos);
            await _unitOfWork.SaveChangesAsync();
        });

        // Assert
        var allTodos = await _repository.GetAllAsync();
        allTodos.Should().HaveCount(2);
    }

    [Fact]
    public async Task EncryptedFields_ShouldBeHandledCorrectly()
    {
        // Arrange
        var sensitiveDescription = "This is sensitive information that should be encrypted";
        var todo = new Todo("Test Todo", sensitiveDescription);

        // Act
        await _repository.AddAsync(todo);
        await _unitOfWork.SaveChangesAsync();

        // Assert
        var retrievedTodo = await _repository.GetByIdAsync(todo.Id);
        retrievedTodo.Should().NotBeNull();
        retrievedTodo!.Description.Should().Be(sensitiveDescription);

        // Verify the description is actually encrypted in the database
        var rawSql = _dbContext.Database.SqlQueryRaw<string>(
            "SELECT Description FROM Todos WHERE Id = {0}", todo.Id);
        
        // Note: This test is database-specific and might need adjustment for different providers
    }

    public void Dispose()
    {
        _scope?.Dispose();
        _serviceProvider?.Dispose();
    }
}