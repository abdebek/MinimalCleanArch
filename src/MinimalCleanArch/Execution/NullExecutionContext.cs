namespace MinimalCleanArch.Execution;

/// <summary>
/// Empty execution context used when no request or message scope data is available.
/// </summary>
public sealed class NullExecutionContext : IExecutionContext
{
    /// <summary>
    /// Gets the shared empty execution-context instance.
    /// </summary>
    public static NullExecutionContext Instance { get; } = new();

    private static readonly IReadOnlyDictionary<string, string> EmptyMetadata =
        new Dictionary<string, string>();

    private NullExecutionContext()
    {
    }

    /// <inheritdoc />
    public string? UserId => null;

    /// <inheritdoc />
    public string? UserName => null;

    /// <inheritdoc />
    public string? TenantId => null;

    /// <inheritdoc />
    public string? CorrelationId => null;

    /// <inheritdoc />
    public string? ClientIpAddress => null;

    /// <inheritdoc />
    public string? UserAgent => null;

    /// <inheritdoc />
    public IReadOnlyDictionary<string, string> Metadata => EmptyMetadata;
}
