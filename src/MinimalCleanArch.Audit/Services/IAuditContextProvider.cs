namespace MinimalCleanArch.Audit.Services;

/// <summary>
/// Provides contextual information for audit logging.
/// Implement this interface to customize how user and request information is captured.
/// </summary>
public interface IAuditContextProvider
{
    /// <summary>
    /// Gets the current user's ID.
    /// </summary>
    string? GetUserId();

    /// <summary>
    /// Gets the current user's name/email.
    /// </summary>
    string? GetUserName();

    /// <summary>
    /// Gets the current correlation ID for request tracing.
    /// </summary>
    string? GetCorrelationId();

    /// <summary>
    /// Gets the client's IP address.
    /// </summary>
    string? GetClientIpAddress();

    /// <summary>
    /// Gets the client's user agent string.
    /// </summary>
    string? GetUserAgent();

    /// <summary>
    /// Gets additional metadata to include in audit logs.
    /// </summary>
    /// <returns>Dictionary of metadata key-value pairs, or null.</returns>
    IDictionary<string, object>? GetMetadata();
}
