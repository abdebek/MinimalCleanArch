namespace MinimalCleanArch.Features;

public sealed class FeatureOptions
{
    public const string SectionName = "Features";

    /// <summary>Global flags. Keys are feature ids (e.g. <c>todo-export</c>).</summary>
    public Dictionary<string, bool> Flags { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Per-tenant overrides: <c>Tenants[tenantId][feature]</c>.</summary>
    public Dictionary<string, Dictionary<string, bool>> Tenants { get; set; } =
        new(StringComparer.OrdinalIgnoreCase);
}
