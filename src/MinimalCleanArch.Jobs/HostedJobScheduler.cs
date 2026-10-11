using System.Collections.Concurrent;
using System.Threading;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MinimalCleanArch.Jobs;

/// <summary>
/// In-process <see cref="IJobScheduler"/> using a timer loop. Delayed jobs use
/// <see cref="IDelayedJobExecutor"/> when present (Wolverine), otherwise an in-memory queue.
/// </summary>
public sealed class HostedJobScheduler : BackgroundService, IJobScheduler
{
    private readonly IJobExecutor _executor;
    private readonly JobsOptions _options;
    private readonly ILogger<HostedJobScheduler> _logger;
    private readonly ConcurrentDictionary<string, RecurringState> _recurring = new(StringComparer.Ordinal);
    private readonly List<DelayedItem> _delayed = [];
    private readonly object _delayedGate = new();

    public HostedJobScheduler(
        IJobExecutor executor,
        JobsOptions options,
        ILogger<HostedJobScheduler> logger)
    {
        _executor = executor;
        _options = options;
        _logger = logger;
    }

    public async Task DelayAsync<TJob>(TJob job, TimeSpan delay, CancellationToken cancellationToken = default)
        where TJob : class
    {
        ArgumentNullException.ThrowIfNull(job);
        if (delay < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(delay));
        }

        if (_executor is IDelayedJobExecutor delayed)
        {
            await delayed.ScheduleAsync(job, delay, cancellationToken);
            return;
        }

        lock (_delayedGate)
        {
            _delayed.Add(new DelayedItem(job, DateTimeOffset.UtcNow + delay));
        }
    }

    public void Recurring<TJob>(string name, TimeSpan interval, Func<TJob> factory)
        where TJob : class
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(factory);
        if (interval <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(interval));
        }

        _recurring[name] = new RecurringState(name, interval, () => factory(), DateTimeOffset.UtcNow);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        foreach (var registration in _options.StartupRecurring)
        {
            _recurring[registration.Name] = new RecurringState(
                registration.Name,
                registration.Interval,
                registration.Factory,
                DateTimeOffset.UtcNow);
        }

        var interval = _options.TickInterval <= TimeSpan.Zero
            ? TimeSpan.FromSeconds(1)
            : _options.TickInterval;

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await TickAsync(stoppingToken);
                await Task.Delay(interval, stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // host shutdown
        }
    }

    private async Task TickAsync(CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        List<DelayedItem> due;
        lock (_delayedGate)
        {
            due = _delayed.Where(i => i.Due <= now).ToList();
            _delayed.RemoveAll(i => i.Due <= now);
        }

        foreach (var item in due)
        {
            await SafeExecuteAsync(item.Job, cancellationToken);
        }

        foreach (var state in _recurring.Values)
        {
            if (state.NextRun > now)
            {
                continue;
            }

            object job;
            try
            {
                job = state.Factory();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Recurring job {Name} factory failed", state.Name);
                state.NextRun = now + state.Interval;
                continue;
            }

            state.NextRun = now + state.Interval;
            await SafeExecuteAsync(job, cancellationToken);
        }
    }

    private async Task SafeExecuteAsync(object job, CancellationToken cancellationToken)
    {
        try
        {
            await _executor.ExecuteAsync(job, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Job {JobType} failed", job.GetType().Name);
        }
    }

    private sealed class RecurringState
    {
        public RecurringState(string name, TimeSpan interval, Func<object> factory, DateTimeOffset nextRun)
        {
            Name = name;
            Interval = interval;
            Factory = factory;
            NextRun = nextRun;
        }

        public string Name { get; }
        public TimeSpan Interval { get; }
        public Func<object> Factory { get; }
        public DateTimeOffset NextRun { get; set; }
    }

    private sealed record DelayedItem(object Job, DateTimeOffset Due);
}
