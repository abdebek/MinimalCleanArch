namespace MinimalCleanArch.Realtime;

/// <summary>
/// Drops messages. Used until a host adapter (SignalR) is registered.
/// </summary>
public sealed class NoOpRealtimePublisher : IRealtimePublisher
{
    public static NoOpRealtimePublisher Instance { get; } = new();

    public Task PublishAsync(RealtimeMessage message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);
        return Task.CompletedTask;
    }
}
