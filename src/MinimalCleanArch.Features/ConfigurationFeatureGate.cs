using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace MinimalCleanArch.Features;

/// <summary>
/// Reads <see cref="FeatureOptions"/> and an optional <see cref="IFeatureStore"/>.
/// Store override, then tenant config, then global flag, else disabled.
/// </summary>
public sealed class ConfigurationFeatureGate : IFeatureGate
{
    private readonly IOptionsMonitor<FeatureOptions> _options;
    private readonly IFeatureStore? _store;

    public ConfigurationFeatureGate(IOptionsMonitor<FeatureOptions> options, IServiceProvider services)
    {
        _options = options;
        _store = services.GetService<IFeatureStore>();
    }

    public bool IsEnabled(string feature, string? tenantId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(feature);

        var stored = _store?.GetOverride(feature, tenantId);
        if (stored is not null)
        {
            return stored.Value;
        }

        var options = _options.CurrentValue;
        if (!string.IsNullOrEmpty(tenantId)
            && options.Tenants.TryGetValue(tenantId, out var tenantFlags)
            && TryGetFlag(tenantFlags, feature, out var tenantEnabled))
        {
            return tenantEnabled;
        }

        return TryGetFlag(options.Flags, feature, out var enabled) && enabled;
    }

    private static bool TryGetFlag(Dictionary<string, bool> flags, string feature, out bool enabled)
    {
        if (flags.TryGetValue(feature, out enabled))
        {
            return true;
        }

        foreach (var pair in flags)
        {
            if (string.Equals(pair.Key, feature, StringComparison.OrdinalIgnoreCase))
            {
                enabled = pair.Value;
                return true;
            }
        }

        enabled = false;
        return false;
    }
}
