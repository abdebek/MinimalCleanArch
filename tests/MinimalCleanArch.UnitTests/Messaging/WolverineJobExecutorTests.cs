using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using MinimalCleanArch.Jobs;
using MinimalCleanArch.Messaging.Extensions;
using MinimalCleanArch.Messaging.Jobs;
using Moq;
using Wolverine;

namespace MinimalCleanArch.UnitTests.Messaging;

public class WolverineJobExecutorTests
{
    [Fact]
    public void AddWolverineJobs_ValidateOnBuild_DoesNotCaptureScopedMessageBus()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddJobs();
        services.AddScoped(_ => Mock.Of<IMessageBus>());
        services.AddWolverineJobs();

        using var provider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateScopes = true,
            ValidateOnBuild = true
        });

        provider.GetRequiredService<IJobExecutor>().Should().BeOfType<WolverineJobExecutor>();
    }

    [Fact]
    public async Task ExecuteAsync_PublishesThroughScopedMessageBus()
    {
        var bus = new Mock<IMessageBus>();
        bus.Setup(x => x.PublishAsync(It.IsAny<object>(), It.IsAny<DeliveryOptions?>()))
            .Returns(ValueTask.CompletedTask);

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddJobs();
        services.AddScoped<IMessageBus>(_ => bus.Object);
        services.AddWolverineJobs();

        await using var provider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateScopes = true,
            ValidateOnBuild = true
        });

        var executor = provider.GetRequiredService<IJobExecutor>();
        var job = new ProbeJob();
        await executor.ExecuteAsync(job);

        bus.Verify(x => x.PublishAsync(It.Is<object>(o => ReferenceEquals(o, job)), null), Times.Once);
    }

    private sealed record ProbeJob;
}
