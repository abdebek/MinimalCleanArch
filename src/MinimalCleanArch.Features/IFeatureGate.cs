namespace MinimalCleanArch.Features;

/// <summary>
/// Framework-neutral feature gate. No ASP.NET or EF types.
/// Missing features are disabled (fail closed).
/// </summary>
public interface IFeatureGate
{
    bool IsEnabled(string feature, string? tenantId = null);
}

/// <summary>
/// Optional table or remote override. A non-null result wins over configuration.
/// </summary>
public interface IFeatureStore
{
    bool? GetOverride(string feature, string? tenantId);
}
