using System.Reflection;
using FluentAssertions;
using MinimalCleanArch.Messaging.Extensions;

namespace MinimalCleanArch.UnitTests.Messaging;

public class MessagingOptionsTests
{
    #region Default Values Tests

    [Fact]
    public void MessagingOptions_ShouldHaveNullServiceNameByDefault()
    {
        // Arrange & Act
        var options = new MessagingOptions();

        // Assert
        options.ServiceName.Should().BeNull();
    }

    [Fact]
    public void MessagingOptions_ShouldHaveDefaultSchemaName()
    {
        // Arrange & Act
        var options = new MessagingOptions();

        // Assert
        options.SchemaName.Should().Be("wolverine");
    }

    [Fact]
    public void MessagingOptions_ShouldHaveDefaultLocalQueueName()
    {
        // Arrange & Act
        var options = new MessagingOptions();

        // Assert
        options.LocalQueueName.Should().Be("domain-events");
    }

    [Fact]
    public void MessagingOptions_ShouldHaveDefaultLocalQueueParallelism()
    {
        // Arrange & Act
        var options = new MessagingOptions();

        // Assert
        options.LocalQueueParallelism.Should().Be(Environment.ProcessorCount);
    }

    [Fact]
    public void MessagingOptions_ShouldHaveDefaultDurabilityPollingInterval()
    {
        // Arrange & Act
        var options = new MessagingOptions();

        // Assert
        options.DurabilityPollingInterval.Should().Be(TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void MessagingOptions_ShouldHaveDeadLetterExpirationDisabledByDefault()
    {
        // Arrange & Act
        var options = new MessagingOptions();

        // Assert
        options.DeadLetterQueueExpirationEnabled.Should().BeFalse();
        options.DeadLetterQueueExpiration.Should().BeNull();
    }

    [Fact]
    public void MessagingOptions_ShouldHaveEmptyHandlerAssembliesByDefault()
    {
        // Arrange & Act
        var options = new MessagingOptions();

        // Assert
        options.HandlerAssemblies.Should().BeEmpty();
    }

    #endregion

    #region Property Setting Tests

    [Fact]
    public void MessagingOptions_ServiceName_ShouldBeSettable()
    {
        // Arrange
        var options = new MessagingOptions();

        // Act
        options.ServiceName = "MyService";

        // Assert
        options.ServiceName.Should().Be("MyService");
    }

    [Fact]
    public void MessagingOptions_SchemaName_ShouldBeSettable()
    {
        // Arrange
        var options = new MessagingOptions();

        // Act
        options.SchemaName = "custom_schema";

        // Assert
        options.SchemaName.Should().Be("custom_schema");
    }

    [Fact]
    public void MessagingOptions_LocalQueueName_ShouldBeSettable()
    {
        // Arrange
        var options = new MessagingOptions();

        // Act
        options.LocalQueueName = "orders";

        // Assert
        options.LocalQueueName.Should().Be("orders");
    }

    [Fact]
    public void MessagingOptions_QueuePrefix_ShouldBeSettable()
    {
        // Arrange
        var options = new MessagingOptions();

        // Act
        options.QueuePrefix = "sales-";

        // Assert
        options.QueuePrefix.Should().Be("sales-");
    }

    [Fact]
    public void MessagingOptions_LocalQueueParallelism_ShouldBeSettable()
    {
        // Arrange
        var options = new MessagingOptions();

        // Act
        options.LocalQueueParallelism = 16;

        // Assert
        options.LocalQueueParallelism.Should().Be(16);
    }

    [Fact]
    public void MessagingOptions_DurabilityPollingInterval_ShouldBeSettable()
    {
        // Arrange
        var options = new MessagingOptions();

        // Act
        options.DurabilityPollingInterval = TimeSpan.FromSeconds(30);

        // Assert
        options.DurabilityPollingInterval.Should().Be(TimeSpan.FromSeconds(30));
    }

    [Fact]
    public void MessagingOptions_DeadLetterExpiration_ShouldBeSettable()
    {
        // Arrange
        var options = new MessagingOptions();

        // Act
        options.DeadLetterQueueExpirationEnabled = true;
        options.DeadLetterQueueExpiration = TimeSpan.FromDays(7);

        // Assert
        options.DeadLetterQueueExpirationEnabled.Should().BeTrue();
        options.DeadLetterQueueExpiration.Should().Be(TimeSpan.FromDays(7));
    }

    #endregion

    #region IncludeAssembly Tests

    [Fact]
    public void IncludeAssembly_ShouldAddAssemblyToCollection()
    {
        // Arrange
        var options = new MessagingOptions();
        var assembly = typeof(MessagingOptionsTests).Assembly;

        // Act
        options.IncludeAssembly(assembly);

        // Assert
        options.HandlerAssemblies.Should().Contain(assembly);
    }

    [Fact]
    public void IncludeAssembly_ShouldReturnOptionsForChaining()
    {
        // Arrange
        var options = new MessagingOptions();
        var assembly = typeof(MessagingOptionsTests).Assembly;

        // Act
        var result = options.IncludeAssembly(assembly);

        // Assert
        result.Should().BeSameAs(options);
    }

    [Fact]
    public void IncludeAssembly_ShouldAllowMultipleAssemblies()
    {
        // Arrange
        var options = new MessagingOptions();
        var assembly1 = typeof(MessagingOptionsTests).Assembly;
        var assembly2 = typeof(string).Assembly;

        // Act
        options.IncludeAssembly(assembly1);
        options.IncludeAssembly(assembly2);

        // Assert
        options.HandlerAssemblies.Should().HaveCount(2);
        options.HandlerAssemblies.Should().Contain(assembly1);
        options.HandlerAssemblies.Should().Contain(assembly2);
    }

    [Fact]
    public void IncludeAssembly_ShouldSupportFluentChaining()
    {
        // Arrange
        var options = new MessagingOptions();
        var assembly1 = typeof(MessagingOptionsTests).Assembly;
        var assembly2 = typeof(string).Assembly;

        // Act
        options
            .IncludeAssembly(assembly1)
            .IncludeAssembly(assembly2);

        // Assert
        options.HandlerAssemblies.Should().HaveCount(2);
    }

    #endregion

    #region IncludeAssemblyContaining Tests

    [Fact]
    public void IncludeAssemblyContaining_ShouldAddAssemblyOfType()
    {
        // Arrange
        var options = new MessagingOptions();

        // Act
        options.IncludeAssemblyContaining<MessagingOptionsTests>();

        // Assert
        options.HandlerAssemblies.Should().Contain(typeof(MessagingOptionsTests).Assembly);
    }

    [Fact]
    public void IncludeAssemblyContaining_ShouldReturnOptionsForChaining()
    {
        // Arrange
        var options = new MessagingOptions();

        // Act
        var result = options.IncludeAssemblyContaining<MessagingOptionsTests>();

        // Assert
        result.Should().BeSameAs(options);
    }

    [Fact]
    public void IncludeAssemblyContaining_ShouldSupportFluentChaining()
    {
        // Arrange
        var options = new MessagingOptions();

        // Act
        options
            .IncludeAssemblyContaining<MessagingOptionsTests>()
            .IncludeAssemblyContaining<string>();

        // Assert
        options.HandlerAssemblies.Should().HaveCount(2);
    }

    [Fact]
    public void IncludeAssemblyContaining_MixedWithIncludeAssembly_ShouldWork()
    {
        // Arrange
        var options = new MessagingOptions();
        var additionalAssembly = typeof(List<>).Assembly;

        // Act
        options
            .IncludeAssemblyContaining<MessagingOptionsTests>()
            .IncludeAssembly(additionalAssembly);

        // Assert
        options.HandlerAssemblies.Should().HaveCount(2);
    }

    #endregion

    #region Configuration Scenario Tests

    [Fact]
    public void MessagingOptions_TypicalConfiguration_ShouldWork()
    {
        // Arrange & Act
        var options = new MessagingOptions
        {
            ServiceName = "OrderService",
            SchemaName = "orders",
            LocalQueueName = "orders-events",
            QueuePrefix = "svc-",
            LocalQueueParallelism = 8,
            DurabilityPollingInterval = TimeSpan.FromSeconds(10),
            DeadLetterQueueExpirationEnabled = true,
            DeadLetterQueueExpiration = TimeSpan.FromDays(5)
        };

        options.IncludeAssemblyContaining<MessagingOptionsTests>();

        // Assert
        options.ServiceName.Should().Be("OrderService");
        options.SchemaName.Should().Be("orders");
        options.LocalQueueName.Should().Be("orders-events");
        options.QueuePrefix.Should().Be("svc-");
        options.LocalQueueParallelism.Should().Be(8);
        options.DurabilityPollingInterval.Should().Be(TimeSpan.FromSeconds(10));
        options.DeadLetterQueueExpirationEnabled.Should().BeTrue();
        options.DeadLetterQueueExpiration.Should().Be(TimeSpan.FromDays(5));
        options.HandlerAssemblies.Should().HaveCount(1);
    }

    [Fact]
    public void GetEffectiveLocalQueueName_ShouldApplyPrefix_WhenPresent()
    {
        var options = new MessagingOptions
        {
            QueuePrefix = "svc-",
            LocalQueueName = "domain-events"
        };

        options.GetEffectiveLocalQueueName().Should().Be("svc-domain-events");
    }

    [Fact]
    public void GetEffectiveLocalQueueName_ShouldReturnBaseName_WhenPrefixMissing()
    {
        var options = new MessagingOptions
        {
            LocalQueueName = "domain-events"
        };

        options.GetEffectiveLocalQueueName().Should().Be("domain-events");
    }

    [Fact]
    public void MessagingOptions_ActionConfiguration_ShouldWork()
    {
        // Arrange
        Action<MessagingOptions> configure = opt =>
        {
            opt.ServiceName = "ConfiguredService";
            opt.IncludeAssemblyContaining<MessagingOptionsTests>();
        };

        var options = new MessagingOptions();

        // Act
        configure(options);

        // Assert
        options.ServiceName.Should().Be("ConfiguredService");
        options.HandlerAssemblies.Should().HaveCount(1);
    }

    #endregion
}
