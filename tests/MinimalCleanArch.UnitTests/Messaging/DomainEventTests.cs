using FluentAssertions;
using MinimalCleanArch.Domain.Events;

namespace MinimalCleanArch.UnitTests.Messaging;

public class DomainEventTests
{
    #region IDomainEvent Tests

    [Fact]
    public void DomainEvent_ShouldHaveUniqueEventId()
    {
        // Arrange & Act
        var event1 = new TestDomainEvent();
        var event2 = new TestDomainEvent();

        // Assert
        event1.EventId.Should().NotBe(Guid.Empty);
        event2.EventId.Should().NotBe(Guid.Empty);
        event1.EventId.Should().NotBe(event2.EventId);
    }

    [Fact]
    public void DomainEvent_ShouldHaveOccurredAtTimestamp()
    {
        // Arrange
        var beforeCreate = DateTimeOffset.UtcNow;

        // Act
        var domainEvent = new TestDomainEvent();
        var afterCreate = DateTimeOffset.UtcNow;

        // Assert
        domainEvent.OccurredAt.Should().BeOnOrAfter(beforeCreate);
        domainEvent.OccurredAt.Should().BeOnOrBefore(afterCreate);
    }

    [Fact]
    public void DomainEvent_ShouldBeImmutable_EventId()
    {
        // Arrange
        var domainEvent = new TestDomainEvent();
        var originalEventId = domainEvent.EventId;

        // Act - trying to create a new record with same EventId (records are immutable by design)
        var copiedEvent = domainEvent with { };

        // Assert - with expression creates a new instance but preserves values
        copiedEvent.EventId.Should().Be(originalEventId);
    }

    #endregion

    #region EntityDomainEvent Tests

    [Fact]
    public void EntityDomainEvent_ShouldRequireEntityId()
    {
        // Arrange & Act
        var entityId = 42;
        var domainEvent = new TestEntityDomainEvent { EntityId = entityId };

        // Assert
        domainEvent.EntityId.Should().Be(entityId);
    }

    [Fact]
    public void EntityDomainEvent_ShouldInheritFromDomainEvent()
    {
        // Arrange & Act
        var domainEvent = new TestEntityDomainEvent { EntityId = 1 };

        // Assert
        domainEvent.Should().BeAssignableTo<IDomainEvent>();
        domainEvent.EventId.Should().NotBe(Guid.Empty);
        domainEvent.OccurredAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void EntityDomainEvent_WithGuidKey_ShouldWork()
    {
        // Arrange & Act
        var entityId = Guid.NewGuid();
        var domainEvent = new TestGuidEntityDomainEvent { EntityId = entityId };

        // Assert
        domainEvent.EntityId.Should().Be(entityId);
    }

    [Fact]
    public void EntityDomainEvent_WithStringKey_ShouldWork()
    {
        // Arrange & Act
        var entityId = "test-entity-id";
        var domainEvent = new TestStringEntityDomainEvent { EntityId = entityId };

        // Assert
        domainEvent.EntityId.Should().Be(entityId);
    }

    #endregion

    #region IHasDomainEvents Tests

    [Fact]
    public void EntityWithEvents_ShouldStartWithNoDomainEvents()
    {
        // Arrange & Act
        var entity = new TestEntityWithEvents();

        // Assert
        entity.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void EntityWithEvents_ShouldCollectRaisedEvents()
    {
        // Arrange
        var entity = new TestEntityWithEvents();

        // Act
        entity.DoSomething();
        entity.DoSomething();

        // Assert
        entity.DomainEvents.Should().HaveCount(2);
        entity.DomainEvents.Should().AllBeOfType<TestDomainEvent>();
    }

    [Fact]
    public void EntityWithEvents_ClearDomainEvents_ShouldRemoveAllEvents()
    {
        // Arrange
        var entity = new TestEntityWithEvents();
        entity.DoSomething();
        entity.DoSomething();
        entity.DomainEvents.Should().HaveCount(2);

        // Act
        entity.ClearDomainEvents();

        // Assert
        entity.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void EntityWithEvents_DomainEvents_ShouldBeReadOnly()
    {
        // Arrange
        var entity = new TestEntityWithEvents();
        entity.DoSomething();

        // Assert
        entity.DomainEvents.Should().BeOfType<System.Collections.ObjectModel.ReadOnlyCollection<IDomainEvent>>();
    }

    [Fact]
    public void EntityWithEvents_ShouldPreserveEventOrder()
    {
        // Arrange
        var entity = new TestEntityWithEvents();

        // Act
        entity.DoSomethingWithValue("first");
        entity.DoSomethingWithValue("second");
        entity.DoSomethingWithValue("third");

        // Assert
        var events = entity.DomainEvents.Cast<TestDomainEventWithValue>().ToList();
        events[0].Value.Should().Be("first");
        events[1].Value.Should().Be("second");
        events[2].Value.Should().Be("third");
    }

    [Fact]
    public void EntityWithEvents_CanRaiseDifferentEventTypes()
    {
        // Arrange
        var entity = new TestEntityWithEvents();

        // Act
        entity.DoSomething();
        entity.DoSomethingWithValue("test");

        // Assert
        entity.DomainEvents.Should().HaveCount(2);
        entity.DomainEvents.OfType<TestDomainEvent>().Should().HaveCount(1);
        entity.DomainEvents.OfType<TestDomainEventWithValue>().Should().HaveCount(1);
    }

    #endregion

    #region Test Helpers

    private record TestDomainEvent : DomainEvent;

    private record TestDomainEventWithValue : DomainEvent
    {
        public required string Value { get; init; }
    }

    private record TestEntityDomainEvent : EntityDomainEvent<int>;

    private record TestGuidEntityDomainEvent : EntityDomainEvent<Guid>;

    private record TestStringEntityDomainEvent : EntityDomainEvent<string>;

    private class TestEntityWithEvents : EntityWithEvents
    {
        public void DoSomething()
        {
            RaiseDomainEvent(new TestDomainEvent());
        }

        public void DoSomethingWithValue(string value)
        {
            RaiseDomainEvent(new TestDomainEventWithValue { Value = value });
        }
    }

    #endregion
}
