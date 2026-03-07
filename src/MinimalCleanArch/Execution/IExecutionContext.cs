namespace MinimalCleanArch.Execution;

/// <summary>
/// Read-only access to user, tenant, correlation, and request or message metadata
/// for the current execution scope.
/// </summary>
public interface IExecutionContext
{
    /// <summary>
    /// Gets the current user identifier.
    /// </summary>
    string? UserId { get; }

    /// <summary>
    /// Gets the current user display name or principal name.
    /// </summary>
    string? UserName { get; }

    /// <summary>
    /// Gets the current tenant identifier.
    /// </summary>
    string? TenantId { get; }

    /// <summary>
    /// Gets the current correlation identifier.
    /// </summary>
    string? CorrelationId { get; }

    /// <summary>
    /// Gets the originating client IP address when available.
    /// </summary>
    string? ClientIpAddress { get; }

    /// <summary>
    /// Gets the originating user agent when available.
    /// </summary>
    string? UserAgent { get; }

    /// <summary>
    /// Gets additional execution metadata for the current scope.
    /// </summary>
    IReadOnlyDictionary<string, string> Metadata { get; }
}
