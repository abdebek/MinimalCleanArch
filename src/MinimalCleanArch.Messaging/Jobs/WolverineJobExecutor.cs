using MinimalCleanArch.Jobs;
using Wolverine;

namespace MinimalCleanArch.Messaging.Jobs;

/// <summary>
/// Dispatches jobs through Wolverine: invoke now, <see cref="IMessageBus.ScheduleAsync"/> for delay.
/// </summary>
public sealed class WolverineJobExecutor : IDelayedJobExecutor
{
    private readonly IMessageBus _messageBus;

    public WolverineJobExecutor(IMessageBus messageBus)
    {
        _messageBus = messageBus;
    }

    public Task ExecuteAsync(object job, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(job);
        cancellationToken.ThrowIfCancellationRequested();
        return _messageBus.PublishAsync(job).AsTask();
    }

    public async Task ScheduleAsync(object job, TimeSpan delay, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(job);
        cancellationToken.ThrowIfCancellationRequested();
        await _messageBus.ScheduleAsync(job, delay);
    }
}
