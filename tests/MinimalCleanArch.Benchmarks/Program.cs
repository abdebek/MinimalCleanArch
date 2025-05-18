using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Microsoft.EntityFrameworkCore;
using MinimalCleanArch.EntityFramework.Repositories;
using MinimalCleanArch.Sample.Domain.Entities;
using MinimalCleanArch.Sample.Infrastructure.Data;
using MinimalCleanArch.Sample.Infrastructure.Specifications;
using MinimalCleanArch.Security.Encryption;

namespace MinimalCleanArch.Benchmarks;

public class Program
{
    public static void Main(string[] args)
    {
        var summary = BenchmarkRunner.Run<RepositoryBenchmarks>();
    }
}

[MemoryDiagnoser]
public class RepositoryBenchmarks
{
    private DbContextOptions<ApplicationDbContext> _options = new DbContextOptionsBuilder<ApplicationDbContext>()
        .UseInMemoryDatabase(databaseName: "BenchmarkDb")
        .Options;

    private AesEncryptionService _encryptionService = new AesEncryptionService(
        new Security.Configuration.EncryptionOptions
        {
            Key = "this-is-a-very-strong-test-encryption-key"
        });

    private ApplicationDbContext _dbContext;
    private Repository<Todo> _repository;

    public RepositoryBenchmarks()
    {
        _dbContext = new ApplicationDbContext(_options, _encryptionService);
        _repository = new Repository<Todo>(_dbContext);
    }

    [GlobalSetup]
    public void Setup()
    {
        // Seed data
        for (int i = 0; i < 1000; i++)
        {
            _dbContext.Todos.Add(new Todo($"Todo {i}", $"Description {i}", i % 5));
        }
        _dbContext.SaveChanges();
    }


    [Benchmark]
    public async Task GetById()
    {
        await _repository.GetByIdAsync(1);
    }

    [Benchmark]
    public async Task GetAllWithSpecification()
    {
        var spec = new TodoFilterSpecification(priority: 3);
        await _repository.GetAsync(spec);
    }

    [Benchmark]
    public async Task GetWithPagination()
    {
        var filterSpec = new TodoFilterSpecification();
        var paginatedSpec = new TodoPaginatedSpecification(10, 2, filterSpec);
        await _repository.GetAsync(paginatedSpec);
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _dbContext.Dispose();
    }
}
