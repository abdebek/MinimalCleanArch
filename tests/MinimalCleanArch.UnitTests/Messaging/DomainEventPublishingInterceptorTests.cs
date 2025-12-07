using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using MinimalCleanArch.Domain.Events;
using MinimalCleanArch.Messaging.Middleware;
using Moq;
using Wolverine;

namespace MinimalCleanArch.UnitTests.Messaging;

public class DomainEventPublishingInterceptorTests : IDisposable
{
    private readonly Mock<IMessageBus> _messageBusMock;
    private readonly Mock<ILogger<DomainEventPublishingInterceptor>> _loggerMock;
    private readonly DomainEventPublishingInterceptor _interceptor;
    private readonly TestDbContext _dbContext;

    public DomainEventPublishingInterceptorTests()
    {
        _messageBusMock = new Mock<IMessageBus>();
        _loggerMock = new Mock<ILogger<DomainEventPublishingInterceptor>>();
        _interceptor = new DomainEventPublishingInterceptor(_messageBusMock.Object, _loggerMock.Object);

        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(databaseName: $"TestDb_{Guid.NewGuid()}")
            .AddInterceptors(_interceptor)
            .Options;

        _dbContext = new TestDbContext(options);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullMessageBus_ShouldThrowArgumentNullException()
    {
        // Act
        var act = () => new DomainEventPublishingInterceptor(null!, _loggerMock.Object);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("messageBus");
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Act
        var act = () => new DomainEventPublishingInterceptor(_messageBusMock.Object, null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    #endregion

    #region SaveChangesAsync Tests

    [Fact]
    public async Task SaveChangesAsync_WithEntityWithEvents_ShouldPublishEvents()
    {
        // Arrange
        _messageBusMock
            .Setup(x => x.PublishAsync(It.IsAny<object>(), It.IsAny<DeliveryOptions?>()))
            .Returns(ValueTask.CompletedTask);

        var entity = new TestEntityWithDomainEvents();
        entity.RaiseTestEvent("test-value");
        _dbContext.TestEntities.Add(entity);

        // Act
        await _dbContext.SaveChangesAsync();

        // Assert - verify PublishAsync was called with any object (Wolverine uses generic PublishAsync<T>)
        _messageBusMock.Verify(
            x => x.PublishAsync(It.Is<object>(o => o is TestDomainEvent), null),
            Times.Once);
    }

    [Fact]
    public async Task SaveChangesAsync_WithMultipleEvents_ShouldPublishAllEvents()
    {
        // Arrange
        _messageBusMock
            .Setup(x => x.PublishAsync(It.IsAny<object>(), It.IsAny<DeliveryOptions?>()))
            .Returns(ValueTask.CompletedTask);

        var entity = new TestEntityWithDomainEvents();
        entity.RaiseTestEvent("event-1");
        entity.RaiseTestEvent("event-2");
        entity.RaiseTestEvent("event-3");
        _dbContext.TestEntities.Add(entity);

        // Act
        await _dbContext.SaveChangesAsync();

        // Assert
        _messageBusMock.Verify(
            x => x.PublishAsync(It.Is<object>(o => o is TestDomainEvent), null),
            Times.Exactly(3));
    }

    [Fact]
    public async Task SaveChangesAsync_WithNoEvents_ShouldNotPublishAnything()
    {
        // Arrange
        var entity = new TestEntityWithDomainEvents();
        _dbContext.TestEntities.Add(entity);

        // Act
        await _dbContext.SaveChangesAsync();

        // Assert
        _messageBusMock.Verify(
            x => x.PublishAsync(It.IsAny<object>(), It.IsAny<DeliveryOptions?>()),
            Times.Never);
    }

    [Fact]
    public async Task SaveChangesAsync_WithMultipleEntities_ShouldPublishAllEventsFromAllEntities()
    {
        // Arrange
        _messageBusMock
            .Setup(x => x.PublishAsync(It.IsAny<object>(), It.IsAny<DeliveryOptions?>()))
            .Returns(ValueTask.CompletedTask);

        var entity1 = new TestEntityWithDomainEvents();
        entity1.RaiseTestEvent("entity1-event1");
        entity1.RaiseTestEvent("entity1-event2");

        var entity2 = new TestEntityWithDomainEvents();
        entity2.RaiseTestEvent("entity2-event1");

        _dbContext.TestEntities.AddRange(entity1, entity2);

        // Act
        await _dbContext.SaveChangesAsync();

        // Assert
        _messageBusMock.Verify(
            x => x.PublishAsync(It.Is<object>(o => o is TestDomainEvent), null),
            Times.Exactly(3));
    }

    [Fact]
    public async Task SaveChangesAsync_AfterPublishing_ShouldClearDomainEvents()
    {
        // Arrange
        _messageBusMock
            .Setup(x => x.PublishAsync(It.IsAny<object>(), It.IsAny<DeliveryOptions?>()))
            .Returns(ValueTask.CompletedTask);

        var entity = new TestEntityWithDomainEvents();
        entity.RaiseTestEvent("test-value");
        _dbContext.TestEntities.Add(entity);

        // Act
        await _dbContext.SaveChangesAsync();

        // Assert
        entity.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public async Task SaveChangesAsync_WhenPublishingFails_ShouldNotClearEvents()
    {
        // Arrange
        var expectedException = new InvalidOperationException("Publishing failed");
        _messageBusMock
            .Setup(x => x.PublishAsync(It.IsAny<object>(), It.IsAny<DeliveryOptions?>()))
            .ThrowsAsync(expectedException);

        var entity = new TestEntityWithDomainEvents();
        entity.RaiseTestEvent("test-value");
        _dbContext.TestEntities.Add(entity);

        // Act
        var act = () => _dbContext.SaveChangesAsync();

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();
        entity.DomainEvents.Should().HaveCount(1, "events should NOT be cleared on failure");
    }

    [Fact]
    public async Task SaveChangesAsync_WithMixedEntityTypes_ShouldOnlyPublishFromEntitiesWithEvents()
    {
        // Arrange
        _messageBusMock
            .Setup(x => x.PublishAsync(It.IsAny<object>(), It.IsAny<DeliveryOptions?>()))
            .Returns(ValueTask.CompletedTask);

        var entityWithEvents = new TestEntityWithDomainEvents();
        entityWithEvents.RaiseTestEvent("test");

        var entityWithoutEvents = new TestEntityWithoutEvents { Name = "test" };

        _dbContext.TestEntities.Add(entityWithEvents);
        _dbContext.EntitiesWithoutEvents.Add(entityWithoutEvents);

        // Act
        await _dbContext.SaveChangesAsync();

        // Assert
        _messageBusMock.Verify(
            x => x.PublishAsync(It.Is<object>(o => o is TestDomainEvent), null),
            Times.Once);
    }

    [Fact]
    public async Task SaveChangesAsync_ShouldPublishEventsAfterSaveCompletes()
    {
        // Arrange
        var publishedEvents = new List<object>();

        _messageBusMock
            .Setup(x => x.PublishAsync(It.IsAny<object>(), It.IsAny<DeliveryOptions?>()))
            .Returns((object e, DeliveryOptions? _) =>
            {
                publishedEvents.Add(e);
                return ValueTask.CompletedTask;
            });

        var entity = new TestEntityWithDomainEvents();
        entity.RaiseTestEvent("test");
        _dbContext.TestEntities.Add(entity);

        // Act
        await _dbContext.SaveChangesAsync();

        // Assert - verify event was published
        publishedEvents.Should().HaveCount(1);
        publishedEvents[0].Should().BeOfType<TestDomainEvent>();
    }

    #endregion

    #region Test Helpers

    public void Dispose()
    {
        _dbContext?.Dispose();
    }

    private record TestDomainEvent : DomainEvent
    {
        public required string Value { get; init; }
    }

    private class TestEntityWithDomainEvents : EntityWithEvents
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;

        public void RaiseTestEvent(string value)
        {
            RaiseDomainEvent(new TestDomainEvent { Value = value });
        }
    }

    private class TestEntityWithoutEvents
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    private class TestDbContext : DbContext
    {
        public TestDbContext(DbContextOptions<TestDbContext> options) : base(options)
        {
        }

        public DbSet<TestEntityWithDomainEvents> TestEntities => Set<TestEntityWithDomainEvents>();
        public DbSet<TestEntityWithoutEvents> EntitiesWithoutEvents => Set<TestEntityWithoutEvents>();
    }

    #endregion
}
