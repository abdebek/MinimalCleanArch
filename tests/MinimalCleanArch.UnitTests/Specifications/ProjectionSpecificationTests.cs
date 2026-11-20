using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using MinimalCleanArch.DataAccess.Repositories;
using MinimalCleanArch.Domain.Entities;
using MinimalCleanArch.Repositories;
using MinimalCleanArch.Specifications;

namespace MinimalCleanArch.UnitTests.Specifications;

public class ProjectionSpecificationTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly ProjectionDbContext _dbContext;
    private readonly Repository<Product> _repository;

    public ProjectionSpecificationTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<ProjectionDbContext>()
            .UseSqlite(_connection)
            .Options;

        _dbContext = new ProjectionDbContext(options);
        _dbContext.Database.EnsureCreated();
        _repository = new Repository<Product>(_dbContext);
    }

    [Fact]
    public async Task Repository_GetAsync_WithProjectionSpec_ProjectsInSql()
    {
        _dbContext.Products.AddRange(
            new Product { Name = "Alpha", Price = 10m, IsActive = true },
            new Product { Name = "Beta", Price = 30m, IsActive = true },
            new Product { Name = "Gamma", Price = 20m, IsActive = false });
        await _dbContext.SaveChangesAsync();

        var summaries = await _repository.GetAsync(new ActiveProductSummarySpec());

        summaries.Should().HaveCount(2);
        summaries.Select(s => s.Name).Should().ContainInOrder("Beta", "Alpha");
        summaries[0].Price.Should().Be(30m);
    }

    [Fact]
    public async Task Repository_GetFirstAsync_WithProjectionSpec_ReturnsFirstProjectedItemOrDefault()
    {
        _dbContext.Products.AddRange(
            new Product { Name = "Cheap", Price = 5m, IsActive = true },
            new Product { Name = "Expensive", Price = 50m, IsActive = true });
        await _dbContext.SaveChangesAsync();

        var top = await _repository.GetFirstAsync(new ActiveProductSummarySpec());
        top.Should().NotBeNull();
        top!.Name.Should().Be("Expensive");

        var missing = await _repository.GetFirstAsync(new ProductByNameSummarySpec("DoesNotExist"));
        missing.Should().BeNull();
    }

    [Fact]
    public void InMemorySpecificationEvaluator_Evaluate_ProjectsMatchingItems()
    {
        var items = new[]
        {
            new Product { Id = 1, Name = "Low", Price = 1m, IsActive = true },
            new Product { Id = 2, Name = "High", Price = 99m, IsActive = true },
            new Product { Id = 3, Name = "Inactive", Price = 500m, IsActive = false }
        };

        var projected = InMemorySpecificationEvaluator.Evaluate(items, new ActiveProductSummarySpec());

        projected.Should().HaveCount(2);
        projected[0].Name.Should().Be("High");
        projected[1].Name.Should().Be("Low");
    }

    [Fact]
    public void BaseSpecification_WithoutSelector_ThrowsInvalidOperationException()
    {
        var spec = new MissingSelectorSpec();

        var act = () => spec.Selector;

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*ApplySelector*");
    }

    [Fact]
    public async Task DefaultInterfaceFallback_ProjectsViaGetAsync()
    {
        IRepository<Product, int> repo = new InMemoryProductRepository(
        [
            new Product { Id = 1, Name = "One", Price = 10m, IsActive = true },
            new Product { Id = 2, Name = "Two", Price = 20m, IsActive = true }
        ]);

        var list = await repo.GetAsync(new ActiveProductSummarySpec());
        var first = await repo.GetFirstAsync(new ActiveProductSummarySpec());

        list.Should().HaveCount(2);
        list[0].Name.Should().Be("Two");
        first!.Name.Should().Be("Two");
    }

    public void Dispose()
    {
        _dbContext.Dispose();
        _connection.Dispose();
    }

    private sealed class Product : BaseEntity
    {
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public bool IsActive { get; set; }
    }

    private sealed record ProductSummary(int Id, string Name, decimal Price);

    private sealed class ActiveProductSummarySpec : BaseSpecification<Product, ProductSummary>
    {
        public ActiveProductSummarySpec()
            : base(p => p.IsActive)
        {
            ApplyOrderByDescending(p => p.Price);
            ApplySelector(p => new ProductSummary(p.Id, p.Name, p.Price));
        }
    }

    private sealed class ProductByNameSummarySpec : BaseSpecification<Product, ProductSummary>
    {
        public ProductByNameSummarySpec(string name)
            : base(p => p.Name == name)
        {
            ApplySelector(p => new ProductSummary(p.Id, p.Name, p.Price));
        }
    }

    private sealed class MissingSelectorSpec : BaseSpecification<Product, ProductSummary>;

    private sealed class ProjectionDbContext(DbContextOptions<ProjectionDbContext> options) : DbContext(options)
    {
        public DbSet<Product> Products => Set<Product>();
    }

    private sealed class InMemoryProductRepository(IReadOnlyList<Product> items) : IRepository<Product, int>
    {
        public Task<IReadOnlyList<Product>> GetAllAsync(CancellationToken cancellationToken = default) => Task.FromResult(items);
        public Task<IReadOnlyList<Product>> GetAsync(System.Linq.Expressions.Expression<Func<Product, bool>> filter, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Product>>(items.Where(filter.Compile()).ToList());
        public Task<IReadOnlyList<Product>> GetAsync(ISpecification<Product> specification, CancellationToken cancellationToken = default) =>
            Task.FromResult(InMemorySpecificationEvaluator.Evaluate(items, specification));
        public Task<Product?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
            Task.FromResult(items.FirstOrDefault(p => p.Id == id));
        public Task<Product?> GetFirstAsync(System.Linq.Expressions.Expression<Func<Product, bool>> filter, CancellationToken cancellationToken = default) =>
            Task.FromResult(items.FirstOrDefault(filter.Compile()));
        public Task<Product?> GetFirstAsync(ISpecification<Product> specification, CancellationToken cancellationToken = default) =>
            Task.FromResult(InMemorySpecificationEvaluator.Evaluate(items, specification).FirstOrDefault());
        public Task<int> CountAsync(System.Linq.Expressions.Expression<Func<Product, bool>>? filter = null, CancellationToken cancellationToken = default) =>
            Task.FromResult(filter is null ? items.Count : items.Count(filter.Compile()));
        public Task<Product> AddAsync(Product entity, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<IEnumerable<Product>> AddRangeAsync(IEnumerable<Product> entities, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<Product> UpdateAsync(Product entity, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<IEnumerable<Product>> UpdateRangeAsync(IEnumerable<Product> entities, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task DeleteAsync(Product entity, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task DeleteAsync(int id, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task DeleteRangeAsync(IEnumerable<Product> entities, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    }
}
