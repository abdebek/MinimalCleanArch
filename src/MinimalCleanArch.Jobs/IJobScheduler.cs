namespace MinimalCleanArch.Jobs;

/// <summary>
/// Framework-neutral scheduling port. Delayed and recurring jobs; no ASP.NET types.
/// </summary>
public interface IJobScheduler
{
    /// <summary>
    /// Runs <paramref name="job"/> after <paramref name="delay"/>.
    /// </summary>
    Task DelayAsync<TJob>(TJob job, TimeSpan delay, CancellationToken cancellationToken = default)
        where TJob : class;

    /// <summary>
    /// Runs <paramref name="factory"/> on <paramref name="interval"/> until the host stops.
    /// </summary>
    void Recurring<TJob>(string name, TimeSpan interval, Func<TJob> factory)
        where TJob : class;
}

/// <summary>
/// Application handler for a job payload. Used by the in-process fallback.
/// Wolverine adapters invoke a conventional <c>Handle</c> method instead.
/// </summary>
public interface IJobHandler<in TJob>
    where TJob : class
{
    Task HandleAsync(TJob job, CancellationToken cancellationToken = default);
}

/// <summary>
/// Dispatches a job payload to the configured backend (DI handler or message bus).
/// </summary>
public interface IJobExecutor
{
    Task ExecuteAsync(object job, CancellationToken cancellationToken = default);
}

/// <summary>
/// Optional durable delay. When registered, <see cref="IJobScheduler.DelayAsync{TJob}"/>
/// uses this instead of the in-process queue.
/// </summary>
public interface IDelayedJobExecutor : IJobExecutor
{
    Task ScheduleAsync(object job, TimeSpan delay, CancellationToken cancellationToken = default);
}
