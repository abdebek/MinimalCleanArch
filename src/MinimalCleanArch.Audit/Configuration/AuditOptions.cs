namespace MinimalCleanArch.Audit.Configuration;

/// <summary>
/// Configuration options for the audit logging feature.
/// </summary>
public class AuditOptions
{
    /// <summary>
    /// Gets or sets whether audit logging is enabled. Default: true.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to capture old values for Update operations. Default: true.
    /// </summary>
    public bool CaptureOldValues { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to capture new values for Create/Update operations. Default: true.
    /// </summary>
    public bool CaptureNewValues { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to track which properties changed. Default: true.
    /// </summary>
    public bool TrackChangedProperties { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to capture client IP address. Default: true.
    /// </summary>
    public bool CaptureClientIp { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to capture user agent. Default: false.
    /// </summary>
    public bool CaptureUserAgent { get; set; } = false;

    /// <summary>
    /// Gets or sets the table name for audit logs. Default: "AuditLogs".
    /// </summary>
    public string TableName { get; set; } = "AuditLogs";

    /// <summary>
    /// Gets or sets the schema for audit logs. Default: null (uses default schema).
    /// </summary>
    public string? Schema { get; set; }

    /// <summary>
    /// Gets or sets entity types to exclude from auditing (by full type name).
    /// </summary>
    public HashSet<string> ExcludedEntityTypes { get; set; } = new();

    /// <summary>
    /// Gets or sets property names to exclude from auditing (applies to all entities).
    /// Common exclusions: passwords, tokens, secrets.
    /// </summary>
    public HashSet<string> ExcludedProperties { get; set; } = new(StringComparer.OrdinalIgnoreCase)
    {
        "Password",
        "PasswordHash",
        "SecurityStamp",
        "ConcurrencyStamp",
        "TwoFactorSecret",
        "RefreshToken",
        "AccessToken"
    };

    /// <summary>
    /// Gets or sets the maximum length for OldValues/NewValues JSON.
    /// Values exceeding this will be truncated. Default: 4000.
    /// </summary>
    public int MaxValueLength { get; set; } = 4000;

    /// <summary>
    /// Gets or sets the retention period for audit logs.
    /// Null means logs are kept indefinitely. Default: null.
    /// </summary>
    public TimeSpan? RetentionPeriod { get; set; }

    /// <summary>
    /// Excludes an entity type from auditing.
    /// </summary>
    /// <typeparam name="T">The entity type to exclude.</typeparam>
    /// <returns>The options instance for chaining.</returns>
    public AuditOptions ExcludeEntity<T>()
    {
        ExcludedEntityTypes.Add(typeof(T).FullName ?? typeof(T).Name);
        return this;
    }

    /// <summary>
    /// Excludes a property from auditing across all entities.
    /// </summary>
    /// <param name="propertyName">The property name to exclude.</param>
    /// <returns>The options instance for chaining.</returns>
    public AuditOptions ExcludeProperty(string propertyName)
    {
        ExcludedProperties.Add(propertyName);
        return this;
    }
}
