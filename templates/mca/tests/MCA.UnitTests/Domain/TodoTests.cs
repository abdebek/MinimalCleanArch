using FluentAssertions;
using MCA.Domain.Entities;
using Xunit;

namespace MCA.UnitTests.Domain;

public class TodoTests
{
    [Fact]
    public void Constructor_WithValidTitle_ShouldCreateTodo()
    {
        // Arrange
        var title = "Test Todo";
        var description = "Test Description";

        // Act
        var todo = new Todo(title, description);

        // Assert
        todo.Title.Should().Be(title);
        todo.Description.Should().Be(description);
        todo.IsCompleted.Should().BeFalse();
        todo.IsDeleted.Should().BeFalse();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithInvalidTitle_ShouldThrowException(string? invalidTitle)
    {
        // Act
        var act = () => new Todo(invalidTitle!);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void MarkAsCompleted_ShouldSetIsCompletedToTrue()
    {
        // Arrange
        var todo = new Todo("Test");

        // Act
        todo.MarkAsCompleted();

        // Assert
        todo.IsCompleted.Should().BeTrue();
    }

    [Fact]
    public void MarkAsCompleted_WhenAlreadyCompleted_ShouldRemainCompleted()
    {
        // Arrange
        var todo = new Todo("Test");
        todo.MarkAsCompleted();

        // Act
        todo.MarkAsCompleted();

        // Assert
        todo.IsCompleted.Should().BeTrue();
    }

    [Fact]
    public void MarkAsIncomplete_ShouldSetIsCompletedToFalse()
    {
        // Arrange
        var todo = new Todo("Test");
        todo.MarkAsCompleted();

        // Act
        todo.MarkAsIncomplete();

        // Assert
        todo.IsCompleted.Should().BeFalse();
    }

    [Fact]
    public void Update_ShouldUpdateAllProperties()
    {
        // Arrange
        var todo = new Todo("Original Title", "Original Description", 1);
        var newTitle = "Updated Title";
        var newDescription = "Updated Description";
        var newPriority = 5;
        var newDueDate = DateTime.UtcNow.AddDays(7);

        // Act
        todo.Update(newTitle, newDescription, newPriority, newDueDate);

        // Assert
        todo.Title.Should().Be(newTitle);
        todo.Description.Should().Be(newDescription);
        todo.Priority.Should().Be(newPriority);
        todo.DueDate.Should().Be(newDueDate);
    }

    [Fact]
    public void Delete_ShouldSetIsDeletedAndDeletedAt()
    {
        // Arrange
        var todo = new Todo("Test");
        var beforeDelete = DateTime.UtcNow;

        // Act
        todo.Delete();

        // Assert
        todo.IsDeleted.Should().BeTrue();
        todo.DeletedAt.Should().NotBeNull();
        todo.DeletedAt.Should().BeOnOrAfter(beforeDelete);
    }

#if (UseMessaging)
    [Fact]
    public void Constructor_ShouldRaiseTodoCreatedEvent()
    {
        // Arrange & Act
        var todo = new Todo("Test");

        // Assert
        todo.DomainEvents.Should().HaveCount(1);
        todo.DomainEvents.First().Should().BeOfType<MCA.Domain.Events.TodoCreatedEvent>();
    }

    [Fact]
    public void MarkAsCompleted_ShouldRaiseTodoCompletedEvent()
    {
        // Arrange
        var todo = new Todo("Test");
        todo.ClearDomainEvents();

        // Act
        todo.MarkAsCompleted();

        // Assert
        todo.DomainEvents.Should().HaveCount(1);
        todo.DomainEvents.First().Should().BeOfType<MCA.Domain.Events.TodoCompletedEvent>();
    }

    [Fact]
    public void ClearDomainEvents_ShouldRemoveAllEvents()
    {
        // Arrange
        var todo = new Todo("Test");
        todo.DomainEvents.Should().NotBeEmpty();

        // Act
        todo.ClearDomainEvents();

        // Assert
        todo.DomainEvents.Should().BeEmpty();
    }
#endif
}
