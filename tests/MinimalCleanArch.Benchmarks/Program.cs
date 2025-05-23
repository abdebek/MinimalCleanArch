using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MinimalCleanArch.EntityFramework.Repositories;
using MinimalCleanArch.Repositories;
using MinimalCleanArch.Sample.Domain.Entities;
using MinimalCleanArch.Sample.Infrastructure.Data;
using MinimalCleanArch.Sample.Infrastructure.Specifications;
using MinimalCleanArch.Security.Encryption;
using MinimalCleanArch.Security.Configuration;

namespace MinimalCleanArch.Benchmarks;

public class Program
{
    public static void Main(string[] args)
    {
        var summary = BenchmarkRunner.Run<RepositoryBenchmarks>();
    }
}

[MemoryDiagnoser]
[SimpleJob]
public class RepositoryBenchmarks
{
    private ServiceProvider _serviceProvider = null!;
    private IServiceScope _scope = null!;
    private ApplicationDbContext _dbContext = null!;
    private IRepository<Todo> _repository = null!;
    private IUnitOfWork _unitOfWork = null!;
    private IEncryptionService _encryptionService = null!;

    [GlobalSetup]
    public void Setup()
    {
        // Setup dependency injection
        var services = new ServiceCollection();
        
        // Add logging
        services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Warning));
        
        // Add encryption
        var encryptionOptions = new EncryptionOptions
        {
            Key = "this-is-a-very-strong-test-encryption-key-at-least-32-chars",
            ValidateKeyStrength = false // Skip validation for benchmarks
        };
        services.AddSingleton(encryptionOptions);
        services.AddSingleton<IEncryptionService, AesEncryptionService>();
        
        // Add DbContext with in-memory database
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseInMemoryDatabase($"BenchmarkDb_{Guid.NewGuid()}"));
        
        // Add repositories and unit of work
        services.AddScoped<DbContext>(provider => provider.GetRequiredService<ApplicationDbContext>());
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IRepository<Todo>, Repository<Todo>>();
        
        _serviceProvider = services.BuildServiceProvider();
        _scope = _serviceProvider.CreateScope();
        
        _dbContext = _scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        _repository = _scope.ServiceProvider.GetRequiredService<IRepository<Todo>>();
        _unitOfWork = _scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        _encryptionService = _scope.ServiceProvider.GetRequiredService<IEncryptionService>();
        
        // Ensure database is created
        _dbContext.Database.EnsureCreated();
        
        // Seed data for benchmarks
        SeedData();
    }

    private void SeedData()
    {
        var todos = new List<Todo>();
        for (int i = 0; i < 1000; i++)
        {
            var todo = new Todo(
                $"Todo {i}",
                $"Description {i} with some longer text to test encryption performance",
                i % 6, // Priority 0-5
                i % 2 == 0 ? DateTime.Now.AddDays(i % 30) : null // Some with due dates
            );
            todos.Add(todo);
        }
        
        // Use bulk operations for faster seeding
        _repository.AddRangeAsync(todos).GetAwaiter().GetResult();
        _unitOfWork.SaveChangesAsync().GetAwaiter().GetResult();
    }

    [Benchmark]
    public async Task GetById()
    {
        var id = Random.Shared.Next(1, 1001);
        await _repository.GetByIdAsync(id);
    }

    [Benchmark]
    public async Task GetAllWithSpecification()
    {
        var priority = Random.Shared.Next(0, 6);
        var spec = new TodoFilterSpecification(priority: priority);
        await _repository.GetAsync(spec);
    }

    [Benchmark]
    public async Task GetWithPagination()
    {
        var filterSpec = new TodoFilterSpecification();
        var paginatedSpec = new TodoPaginatedSpecification(10, 2, filterSpec);
        await _repository.GetAsync(paginatedSpec);
    }

    [Benchmark]
    public async Task CreateSingleTodo()
    {
        var todo = new Todo(
            $"Benchmark Todo {DateTime.Now.Ticks}",
            "Benchmark description with encrypted content",
            3);
        
        await _repository.AddAsync(todo);
        await _unitOfWork.SaveChangesAsync();
    }

    [Benchmark]
    public async Task CreateMultipleTodosWithTransaction()
    {
        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var todos = new List<Todo>();
            for (int i = 0; i < 10; i++)
            {
                todos.Add(new Todo(
                    $"Batch Todo {DateTime.Now.Ticks}-{i}",
                    $"Batch description {i}",
                    i % 6));
            }
            
            await _repository.AddRangeAsync(todos);
            await _unitOfWork.SaveChangesAsync();
        });
    }

    [Benchmark]
    public async Task UpdateTodo()
    {
        var id = Random.Shared.Next(1, 501); // Update from first half of data
        var todo = await _repository.GetByIdAsync(id);
        
        if (todo != null)
        {
            todo.Update(
                $"Updated {todo.Title}",
                $"Updated {todo.Description}",
                (todo.Priority + 1) % 6,
                DateTime.Now.AddDays(7));
            
            await _repository.UpdateAsync(todo);
            await _unitOfWork.SaveChangesAsync();
        }
    }

    [Benchmark]
    public async Task SoftDeleteTodo()
    {
        var id = Random.Shared.Next(501, 1001); // Delete from second half
        var todo = await _repository.GetByIdAsync(id);
        
        if (todo != null && !todo.IsDeleted)
        {
            await _repository.DeleteAsync(todo);
            await _unitOfWork.SaveChangesAsync();
        }
    }

    [Benchmark]
    public async Task CountWithFilter()
    {
        var spec = new TodoFilterSpecification(isCompleted: false, priority: 3);
        await _repository.CountAsync(spec.Criteria);
    }

    [Benchmark]
    public async Task SearchTodos()
    {
        var searchTerms = new[] { "Todo", "Description", "Benchmark", "Test" };
        var searchTerm = searchTerms[Random.Shared.Next(searchTerms.Length)];
        
        var spec = new TodoFilterSpecification(searchTerm: searchTerm);
        await _repository.GetAsync(spec);
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _encryptionService?.Dispose();
        _scope?.Dispose();
        _serviceProvider?.Dispose();
    }
}
