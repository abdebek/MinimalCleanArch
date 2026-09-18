using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MinimalCleanArch.Jobs;

namespace MinimalCleanArch.UnitTests.Jobs;

public class HostedJobSchedulerTests
{
    [Fact]
    public async Task Recurring_InvokesHandler()
    {
        var hits = 0;
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IJobHandler<ProbeJob>>(new ProbeHandler(() => Interlocked.Increment(ref hits)));
        services.AddJobs(options =>
        {
            options.TickInterval = TimeSpan.FromMilliseconds(20);
            options.Recurring("probe", TimeSpan.FromMilliseconds(40), () => new ProbeJob());
        });

        await using var provider = services.BuildServiceProvider();
        var hosted = (IHostedService)provider.GetRequiredService<IJobScheduler>();
        await hosted.StartAsync(CancellationToken.None);
        await Task.Delay(180);
        await hosted.StopAsync(CancellationToken.None);

        hits.Should().BeGreaterThanOrEqualTo(2);
    }

    [Fact]
    public async Task DelayAsync_InvokesHandlerOnce()
    {
        var hits = 0;
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IJobHandler<ProbeJob>>(new ProbeHandler(() => Interlocked.Increment(ref hits)));
        services.AddJobs(options => options.TickInterval = TimeSpan.FromMilliseconds(20));

        await using var provider = services.BuildServiceProvider();
        var scheduler = provider.GetRequiredService<IJobScheduler>();
        var hosted = (IHostedService)scheduler;
        await hosted.StartAsync(CancellationToken.None);
        await scheduler.DelayAsync(new ProbeJob(), TimeSpan.FromMilliseconds(30));
        await Task.Delay(150);
        await hosted.StopAsync(CancellationToken.None);

        hits.Should().Be(1);
    }

    [Fact]
    public void AddJobs_RegistersHostedScheduler()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddJobs();
        using var provider = services.BuildServiceProvider();
        provider.GetRequiredService<IJobScheduler>().Should().BeOfType<HostedJobScheduler>();
        provider.GetRequiredService<IJobExecutor>().Should().BeOfType<HandlerJobExecutor>();
    }
}

public class JobsPortSurfaceTests
{
    [Fact]
    public void PortTypes_AreNotAspNet()
    {
        typeof(IJobScheduler).Assembly.GetReferencedAssemblies()
            .Select(a => a.Name)
            .Should()
            .NotContain(n => n != null && n.StartsWith("Microsoft.AspNetCore", StringComparison.Ordinal));
    }
}

file sealed record ProbeJob;

file sealed class ProbeHandler(Action onHit) : IJobHandler<ProbeJob>
{
    public Task HandleAsync(ProbeJob job, CancellationToken cancellationToken = default)
    {
        onHit();
        return Task.CompletedTask;
    }
}
