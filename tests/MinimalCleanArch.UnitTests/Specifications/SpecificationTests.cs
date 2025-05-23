using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MinimalCleanArch.EntityFramework.Specifications;
using MinimalCleanArch.Repositories;
using MinimalCleanArch.Sample.Domain.Entities;
using MinimalCleanArch.Sample.Infrastructure.Data;
using MinimalCleanArch.Sample.Infrastructure.Specifications;
using MinimalCleanArch.Security.Configuration;
using MinimalCleanArch.Security.Encryption;
using FluentAssertions;
using MinimalCleanArch.EntityFramework.Repositories;

namespace MinimalCleanArch.UnitTests.Specifications;

public class SpecificationTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly IServiceScope _scope;
    private readonly ApplicationDbContext _dbContext;
    private readonly IRepository<Todo> _repository;
    private readonly IUnitOfWork _unitOfWork;

    public SpecificationTests()
    {
        // Setup dependency injection for tests
        var services = new ServiceCollection();
        
        // Add logging
        services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Warning));
        
        // Add encryption service
        var encryptionOptions = new EncryptionOptions
        {
            Key = "test-encryption-key-for-specification-tests-32",
            ValidateKeyStrength = false
        };
        services.AddSingleton(encryptionOptions);
        services.AddSingleton<IEncryptionService, AesEncryptionService>();
        
        // Add DbContext with unique in-memory database
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseInMemoryDatabase($"SpecTestDb_{Guid.NewGuid()}"));
        
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
        
        // Seed test data
        SeedTestData();
    }

    private void SeedTestData()
    {
        var todos = new List<Todo>
        {
            new("High Priority Task", "Important work to be done", 5, DateTime.Now.AddDays(1)),
            new("Medium Priority Task", "Regular work", 3, DateTime.Now.AddDays(7)),
            new("Low Priority Task", "Nice to have", 1, DateTime.Now.AddDays(30)),
            new("Completed Task", "Already finished", 2) { },
            new("Overdue Task", "Should have been done", 4, DateTime.Now.AddDays(-5))
        };

        // Mark one as completed
        todos[3].MarkAsCompleted();

        _repository.AddRangeAsync(todos).GetAwaiter().GetResult();
        _unitOfWork.SaveChangesAsync().GetAwaiter().GetResult();
    }

    [Fact]
    public async Task TodoFilterSpecification_FiltersByPriority()
    {
        // Arrange
        var spec = new TodoFilterSpecification(priority: 3);

        // Act
        var result = await _repository.GetAsync(spec);

        // Assert
        result.Should().HaveCount(1);
        result.All(t => t.Priority == 3).Should().BeTrue();
        result.First().Title.Should().Be("Medium Priority Task");
    }

    [Fact]
    public async Task TodoFilterSpecification_FiltersSearchTerm()
    {
        // Arrange
        var spec = new TodoFilterSpecification(searchTerm: "high");

        // Act
        var result = await _repository.GetAsync(spec);

        // Assert
        result.Should().HaveCount(1);
        result.First().Title.Should().Be("High Priority Task");
    }

    [Fact]
    public async Task TodoFilterSpecification_FiltersByCompletion()
    {
        // Arrange
        var completedSpec = new TodoFilterSpecification(isCompleted: true);
        var incompleteSpec = new TodoFilterSpecification(isCompleted: false);

        // Act
        var completedTodos = await _repository.GetAsync(completedSpec);
        var incompleteTodos = await _repository.GetAsync(incompleteSpec);

        // Assert
        completedTodos.Should().HaveCount(1);
        completedTodos.All(t => t.IsCompleted).Should().BeTrue();
        
        incompleteTodos.Should().HaveCount(4);
        incompleteTodos.All(t => !t.IsCompleted).Should().BeTrue();
    }

    [Fact]
    public async Task TodoFilterSpecification_FiltersByDueDate()
    {
        // Arrange
        var tomorrow = DateTime.Now.AddDays(1);
        var dueBeforeSpec = new TodoFilterSpecification(dueBefore: tomorrow);
        var dueAfterSpec = new TodoFilterSpecification(dueAfter: tomorrow);

        // Act
        var dueBefore = await _repository.GetAsync(dueBeforeSpec);
        var dueAfter = await _repository.GetAsync(dueAfterSpec);

        // Assert
        dueBefore.Should().HaveCount(2); // High Priority (due in 1 day) and Overdue (past due)
        dueAfter.Should().HaveCount(2);  // Medium Priority (due in 7 days) and Low Priority (due in 30 days)
    }

    [Fact]
    public async Task TodoFilterSpecification_CombinesMultipleFilters()
    {
        // Arrange
        var spec = new TodoFilterSpecification(
            isCompleted: false,
            priority: 5);

        // Act
        var result = await _repository.GetAsync(spec);

        // Assert
        result.Should().HaveCount(1);
        var todo = result.First();
        todo.IsCompleted.Should().BeFalse();
        todo.Priority.Should().Be(5);
        todo.Title.Should().Be("High Priority Task");
    }

    [Fact]
    public async Task TodoPaginatedSpecification_AppliesPaging()
    {
        // Arrange
        var filterSpec = new TodoFilterSpecification();
        var firstPageSpec = new TodoPaginatedSpecification(2, 1, filterSpec); // Page 1, 2 items
        var secondPageSpec = new TodoPaginatedSpecification(2, 2, filterSpec); // Page 2, 2 items

        // Act
        var firstPage = await _repository.GetAsync(firstPageSpec);
        var secondPage = await _repository.GetAsync(secondPageSpec);

        // Assert
        firstPage.Should().HaveCount(2);
        secondPage.Should().HaveCount(2);
        
        // Ensure no overlap between pages
        var firstPageIds = firstPage.Select(t => t.Id).ToHashSet();
        var secondPageIds = secondPage.Select(t => t.Id).ToHashSet();
        firstPageIds.Should().NotIntersectWith(secondPageIds);
    }

    [Fact]
    public async Task TodoPaginatedSpecification_RespectsOrdering()
    {
        // Arrange
        var filterSpec = new TodoFilterSpecification();
        var paginatedSpec = new TodoPaginatedSpecification(10, 1, filterSpec);

        // Act
        var result = await _repository.GetAsync(paginatedSpec);

        // Assert
        result.Should().HaveCount(5);
        
        // Verify ordering (by priority, then by due date descending)
        for (int i = 1; i < result.Count; i++)
        {
            var current = result[i - 1];
            var next = result[i];
            
            // Either current priority <= next priority
            // Or if same priority, current due date >= next due date (nulls last)
            var isCorrectOrder = current.Priority <= next.Priority ||
                                (current.Priority == next.Priority && 
                                 CompareDueDates(current.DueDate, next.DueDate) >= 0);
            
            isCorrectOrder.Should().BeTrue($"Item {i-1} should come before item {i}");
        }
    }

    [Fact]
    public async Task TodoByIdSpecification_ReturnsCorrectTodo()
    {
        // Arrange
        var allTodos = await _repository.GetAllAsync();
        var targetTodo = allTodos.First();
        var spec = new TodoByIdSpecification(targetTodo.Id);

        // Act
        var result = await _repository.GetAsync(spec);

        // Assert
        result.Should().HaveCount(1);
        result.First().Id.Should().Be(targetTodo.Id);
        result.First().Title.Should().Be(targetTodo.Title);
    }

    [Fact]
    public async Task SpecificationEvaluator_HandlesComplexQuery()
    {
        // Arrange
        var spec = new TodoFilterSpecification(
            searchTerm: "priority",
            isCompleted: false,
            dueAfter: DateTime.Now.AddDays(-10),
            dueBefore: DateTime.Now.AddDays(10));

        // Act
        var result = await _repository.GetAsync(spec);

        // Assert
        result.Should().NotBeEmpty();
        result.All(t => !t.IsCompleted).Should().BeTrue();
        result.All(t => t.Title.ToLower().Contains("priority") || 
                       t.Description.ToLower().Contains("priority")).Should().BeTrue();
    }

    [Fact]
    public async Task Specification_WorksWithCount()
    {
        // Arrange
        var spec = new TodoFilterSpecification(isCompleted: false);

        // Act
        var count = await _repository.CountAsync(spec.Criteria);
        var items = await _repository.GetAsync(spec);

        // Assert
        count.Should().Be(items.Count);
        count.Should().Be(4); // 4 incomplete todos
    }

    private static int CompareDueDates(DateTime? date1, DateTime? date2)
    {
        // Null dates are considered "maximum" (last in descending order)
        if (date1 == null && date2 == null) return 0;
        if (date1 == null) return -1; // null comes after non-null in descending order
        if (date2 == null) return 1;  // non-null comes before null in descending order
        
        return DateTime.Compare(date1.Value, date2.Value);
    }

    public void Dispose()
    {
        _scope?.Dispose();
        _serviceProvider?.Dispose();
    }
}