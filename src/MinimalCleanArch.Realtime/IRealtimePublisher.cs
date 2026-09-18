namespace MinimalCleanArch.Realtime;

/// <summary>
/// Framework-neutral realtime port. Channel + payload + optional tenant/user audience.
/// No SignalR or ASP.NET types.
/// </summary>
public interface IRealtimePublisher
{
    Task PublishAsync(RealtimeMessage message, CancellationToken cancellationToken = default);
}

/// <summary>
/// A realtime event to push to subscribers of <see cref="Channel"/>.
/// </summary>
public sealed class RealtimeMessage
{
    public required string Channel { get; init; }
    public required object Payload { get; init; }
    public string? TenantId { get; init; }
    public string? UserId { get; init; }
}
