using Microsoft.AspNetCore.SignalR;

namespace MinimalCleanArch.Extensions.Realtime;

/// <summary>
/// Demo hub. Clients call <see cref="Subscribe"/> with a channel (e.g. <c>todos</c>).
/// </summary>
public sealed class RealtimeHub : Hub
{
    public const string Path = "/hubs/realtime";
    public const string ClientMethod = "event";

    public Task Subscribe(string channel)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(channel);
        return Groups.AddToGroupAsync(Context.ConnectionId, GroupName(channel, tenantId: null, userId: null));
    }

    internal static string GroupName(string channel, string? tenantId, string? userId)
    {
        if (!string.IsNullOrEmpty(userId))
        {
            return $"user:{userId}";
        }

        if (!string.IsNullOrEmpty(tenantId))
        {
            return $"tenant:{tenantId}:{channel}";
        }

        return $"channel:{channel}";
    }
}
