using Microsoft.Extensions.DependencyInjection;
using MinimalCleanArch.Jobs;
using Wolverine;

namespace MinimalCleanArch.Messaging.Jobs;

/// <summary>
/// Dispatches jobs through Wolverine: invoke now, <see cref="IMessageBus.ScheduleAsync"/> for delay.
/// Resolves <see cref="IMessageBus"/> per call — it is scoped, while <see cref="IJobExecutor"/> is a singleton.
/// </summary>
public sealed class WolverineJobExecutor : IDelayedJobExecutor
{
    private readonly IServiceScopeFactory _scopeFactory;

    public WolverineJobExecutor(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public async Task ExecuteAsync(object job, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(job);
        cancellationToken.ThrowIfCancellationRequested();
        await using var scope = _scopeFactory.CreateAsyncScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
        await bus.PublishAsync(job);
    }

    public async Task ScheduleAsync(object job, TimeSpan delay, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(job);
        cancellationToken.ThrowIfCancellationRequested();
        await using var scope = _scopeFactory.CreateAsyncScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
        await bus.ScheduleAsync(job, delay);
    }
}
