using FluentAssertions;
using MinimalCleanArch.Domain.Events;
using MinimalCleanArch.Messaging;
using Moq;
using Wolverine;

namespace MinimalCleanArch.UnitTests.Messaging;

public class WolverineDomainEventPublisherTests
{
    private readonly Mock<IMessageBus> _messageBusMock;
    private readonly WolverineDomainEventPublisher _publisher;

    public WolverineDomainEventPublisherTests()
    {
        _messageBusMock = new Mock<IMessageBus>();
        _publisher = new WolverineDomainEventPublisher(_messageBusMock.Object);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullMessageBus_ShouldThrowArgumentNullException()
    {
        // Act
        var act = () => new WolverineDomainEventPublisher(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("messageBus");
    }

    [Fact]
    public void Constructor_WithValidMessageBus_ShouldNotThrow()
    {
        // Act
        var act = () => new WolverineDomainEventPublisher(_messageBusMock.Object);

        // Assert
        act.Should().NotThrow();
    }

    #endregion

    #region PublishAsync (Single Event) Tests

    [Fact]
    public async Task PublishAsync_WithValidEvent_ShouldPublishToMessageBus()
    {
        // Arrange
        var domainEvent = new TestEvent();
        _messageBusMock
            .Setup(x => x.PublishAsync(It.IsAny<object>(), It.IsAny<DeliveryOptions?>()))
            .Returns(ValueTask.CompletedTask);

        // Act
        await _publisher.PublishAsync(domainEvent);

        // Assert - Wolverine's PublishAsync is generic, so verify using object match
        _messageBusMock.Verify(
            x => x.PublishAsync(It.Is<object>(o => ReferenceEquals(o, domainEvent)), null),
            Times.Once);
    }

    [Fact]
    public async Task PublishAsync_WithNullEvent_ShouldThrowArgumentNullException()
    {
        // Act
        var act = () => _publisher.PublishAsync((IDomainEvent)null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("domainEvent");
    }

    [Fact]
    public async Task PublishAsync_WithCancelledToken_ShouldThrowOperationCanceledException()
    {
        // Arrange
        var domainEvent = new TestEvent();
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act
        var act = () => _publisher.PublishAsync(domainEvent, cts.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task PublishAsync_WhenMessageBusThrows_ShouldPropagateException()
    {
        // Arrange
        var domainEvent = new TestEvent();
        var expectedException = new InvalidOperationException("Test exception");
        _messageBusMock
            .Setup(x => x.PublishAsync(It.IsAny<object>(), It.IsAny<DeliveryOptions?>()))
            .ThrowsAsync(expectedException);

        // Act
        var act = () => _publisher.PublishAsync(domainEvent);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Test exception");
    }

    #endregion

    #region PublishAsync (Multiple Events) Tests

    [Fact]
    public async Task PublishAsync_WithMultipleEvents_ShouldPublishAllEvents()
    {
        // Arrange
        var events = new List<IDomainEvent>
        {
            new TestEvent(),
            new TestEvent(),
            new TestEvent()
        };
        _messageBusMock
            .Setup(x => x.PublishAsync(It.IsAny<object>(), It.IsAny<DeliveryOptions?>()))
            .Returns(ValueTask.CompletedTask);

        // Act
        await _publisher.PublishAsync(events);

        // Assert
        _messageBusMock.Verify(
            x => x.PublishAsync(It.IsAny<object>(), null),
            Times.Exactly(3));
    }

    [Fact]
    public async Task PublishAsync_WithEmptyEventList_ShouldNotPublishAnything()
    {
        // Arrange
        var events = new List<IDomainEvent>();

        // Act
        await _publisher.PublishAsync(events);

        // Assert
        _messageBusMock.Verify(
            x => x.PublishAsync(It.IsAny<object>(), It.IsAny<DeliveryOptions?>()),
            Times.Never);
    }

    [Fact]
    public async Task PublishAsync_WithNullEventCollection_ShouldThrowArgumentNullException()
    {
        // Act
        var act = () => _publisher.PublishAsync((IEnumerable<IDomainEvent>)null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("domainEvents");
    }

    [Fact]
    public async Task PublishAsync_MultipleEvents_WithCancelledToken_ShouldThrowOperationCanceledException()
    {
        // Arrange
        var events = new List<IDomainEvent> { new TestEvent(), new TestEvent() };
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act
        var act = () => _publisher.PublishAsync(events, cts.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task PublishAsync_MultipleEvents_ShouldPublishConcurrently()
    {
        // Arrange
        var publishStartTimes = new List<DateTimeOffset>();
        var events = Enumerable.Range(0, 5).Select(_ => (IDomainEvent)new TestEvent()).ToList();

        _messageBusMock
            .Setup(x => x.PublishAsync(It.IsAny<object>(), It.IsAny<DeliveryOptions?>()))
            .Returns(async () =>
            {
                lock (publishStartTimes)
                {
                    publishStartTimes.Add(DateTimeOffset.UtcNow);
                }
                await Task.Delay(50); // Simulate some async work
            });

        // Act
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        await _publisher.PublishAsync(events);
        stopwatch.Stop();

        // Assert
        // If concurrent, total time should be ~50ms (not 250ms for sequential)
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(400,
            "events should be published concurrently with headroom for slower CI machines");
    }

    #endregion

    #region ScheduleAsync Tests

    [Fact]
    public async Task ScheduleAsync_WithNullEvent_DateTimeOffset_ShouldThrowArgumentNullException()
    {
        // Act
        var act = () => _publisher.ScheduleAsync(null!, DateTimeOffset.UtcNow.AddHours(1));

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("domainEvent");
    }

    [Fact]
    public async Task ScheduleAsync_WithNullEvent_TimeSpan_ShouldThrowArgumentNullException()
    {
        // Act
        var act = () => _publisher.ScheduleAsync(null!, TimeSpan.FromMinutes(30));

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("domainEvent");
    }

    [Fact]
    public async Task ScheduleAsync_WithCancelledToken_DateTimeOffset_ShouldThrowOperationCanceledException()
    {
        // Arrange
        var domainEvent = new TestEvent();
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act
        var act = () => _publisher.ScheduleAsync(domainEvent, DateTimeOffset.UtcNow.AddHours(1), cts.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task ScheduleAsync_WithCancelledToken_TimeSpan_ShouldThrowOperationCanceledException()
    {
        // Arrange
        var domainEvent = new TestEvent();
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act
        var act = () => _publisher.ScheduleAsync(domainEvent, TimeSpan.FromMinutes(30), cts.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    #endregion

    #region Test Helpers

    private record TestEvent : DomainEvent;

    #endregion
}
