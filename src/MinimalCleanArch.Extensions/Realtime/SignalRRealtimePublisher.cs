using Microsoft.AspNetCore.SignalR;
using MinimalCleanArch.Realtime;

namespace MinimalCleanArch.Extensions.Realtime;

/// <summary>
/// SignalR adapter for <see cref="IRealtimePublisher"/>.
/// </summary>
public sealed class SignalRRealtimePublisher : IRealtimePublisher
{
    private readonly IHubContext<RealtimeHub> _hub;

    public SignalRRealtimePublisher(IHubContext<RealtimeHub> hub)
    {
        _hub = hub;
    }

    public Task PublishAsync(RealtimeMessage message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentException.ThrowIfNullOrWhiteSpace(message.Channel);

        var group = RealtimeHub.GroupName(message.Channel, message.TenantId, message.UserId);
        return _hub.Clients.Group(group).SendAsync(
            RealtimeHub.ClientMethod,
            new { channel = message.Channel, payload = message.Payload },
            cancellationToken);
    }
}
