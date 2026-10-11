namespace MinimalCleanArch.Jobs;

/// <summary>
/// Scheduler tick and jobs registered at host start.
/// </summary>
public sealed class JobsOptions
{
    /// <summary>
    /// How often the in-process scheduler scans delayed and recurring jobs.
    /// Default: 1 second.
    /// </summary>
    public TimeSpan TickInterval { get; set; } = TimeSpan.FromSeconds(1);

    internal List<RecurringRegistration> StartupRecurring { get; } = [];

    public JobsOptions Recurring<TJob>(string name, TimeSpan interval, Func<TJob> factory)
        where TJob : class
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(factory);
        if (interval <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(interval), "Interval must be positive.");
        }

        StartupRecurring.Add(new RecurringRegistration(name, interval, () => factory()));
        return this;
    }
}

internal sealed record RecurringRegistration(string Name, TimeSpan Interval, Func<object> Factory);
