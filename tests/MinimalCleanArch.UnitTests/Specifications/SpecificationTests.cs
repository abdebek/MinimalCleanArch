using Microsoft.EntityFrameworkCore;
using MinimalCleanArch.EntityFramework.Specifications;
using MinimalCleanArch.Sample.Domain.Entities;
using MinimalCleanArch.Sample.Infrastructure.Data;
using MinimalCleanArch.Sample.Infrastructure.Specifications;
using MinimalCleanArch.Security.Encryption;
using Moq;
using FluentAssertions;

namespace MinimalCleanArch.UnitTests.Specifications;

public class SpecificationTests
{
    private readonly DbContextOptions<ApplicationDbContext> _options;
    private readonly Mock<IEncryptionService> _encryptionServiceMock;

    public SpecificationTests()
    {
        _options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: $"TestDb_{Guid.NewGuid()}")
            .Options;

        _encryptionServiceMock = new Mock<IEncryptionService>();
        _encryptionServiceMock.Setup(x => x.Encrypt(It.IsAny<string>())).Returns<string>(s => s);
        _encryptionServiceMock.Setup(x => x.Decrypt(It.IsAny<string>())).Returns<string>(s => s);

        // Seed data
        using var context = new ApplicationDbContext(_options, _encryptionServiceMock.Object);
        context.Todos.AddRange(
            new Todo("High Priority", "Description", 5),
            new Todo("Medium Priority", "Description", 3),
            new Todo("Low Priority", "Description", 1)
        );
        context.SaveChanges();
    }

    [Fact]
    public void TodoFilterSpecification_FiltersByPriority()
    {
        // Arrange
        using var context = new ApplicationDbContext(_options, _encryptionServiceMock.Object);
        
        // Act
        var spec = new TodoFilterSpecification(priority: 3);
        var query = SpecificationEvaluator<Todo>.GetQuery(context.Todos.AsQueryable(), spec);
        var result = query.ToList();
        
        // Assert
        result.Should().HaveCount(1);
        result.All(t => t.Priority >= 3).Should().BeTrue();
    }

    [Fact]
    public void TodoFilterSpecification_FiltersSearchTerm()
    {
        // Arrange
        using var context = new ApplicationDbContext(_options, _encryptionServiceMock.Object);
        
        // Act
        var spec = new TodoFilterSpecification(searchTerm: "high");
        var query = SpecificationEvaluator<Todo>.GetQuery(context.Todos.AsQueryable(), spec);
        var result = query.ToList();
        
        // Assert
        result.Should().HaveCount(1);
        result.First().Title.Should().Be("High Priority");
    }

    [Fact]
    public void TodoPaginatedSpecification_AppliesPaging()
    {
        // Arrange
        using var context = new ApplicationDbContext(_options, _encryptionServiceMock.Object);
        var filterSpec = new TodoFilterSpecification();
        
        // Act
        var spec = new TodoPaginatedSpecification(1, 2, filterSpec);
        var query = SpecificationEvaluator<Todo>.GetQuery(context.Todos.AsQueryable(), spec);
        var result = query.ToList();
        
        // Assert
        result.Should().HaveCount(1); // Page size = 1, Page index = 2 (so second page, which has 1 item)
    }
}
