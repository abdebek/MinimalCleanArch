namespace MinimalCleanArch.Audit.Entities;

/// <summary>
/// Represents an audit log entry tracking entity changes.
/// </summary>
public class AuditLog
{
    /// <summary>
    /// Unique identifier for the audit log entry.
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// The type of operation performed (Create, Update, Delete, SoftDelete).
    /// </summary>
    public AuditOperation Operation { get; set; }

    /// <summary>
    /// The full type name of the entity being audited.
    /// </summary>
    public string EntityType { get; set; } = string.Empty;

    /// <summary>
    /// The primary key value of the entity (serialized as string).
    /// </summary>
    public string EntityId { get; set; } = string.Empty;

    /// <summary>
    /// The user ID who performed the action.
    /// </summary>
    public string? UserId { get; set; }

    /// <summary>
    /// The username who performed the action (denormalized for easier querying).
    /// </summary>
    public string? UserName { get; set; }

    /// <summary>
    /// UTC timestamp when the change occurred.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// JSON representation of the entity state before the change (null for Create).
    /// </summary>
    public string? OldValues { get; set; }

    /// <summary>
    /// JSON representation of the entity state after the change (null for Delete).
    /// </summary>
    public string? NewValues { get; set; }

    /// <summary>
    /// JSON array of property names that were modified (for Update operations).
    /// </summary>
    public string? ChangedProperties { get; set; }

    /// <summary>
    /// Optional correlation ID for request tracing.
    /// </summary>
    public string? CorrelationId { get; set; }

    /// <summary>
    /// Optional IP address of the client.
    /// </summary>
    public string? ClientIpAddress { get; set; }

    /// <summary>
    /// Optional user agent string.
    /// </summary>
    public string? UserAgent { get; set; }

    /// <summary>
    /// Optional additional metadata as JSON.
    /// </summary>
    public string? Metadata { get; set; }
}

/// <summary>
/// Types of audit operations.
/// </summary>
public enum AuditOperation
{
    /// <summary>
    /// Entity was created.
    /// </summary>
    Create = 1,

    /// <summary>
    /// Entity was updated.
    /// </summary>
    Update = 2,

    /// <summary>
    /// Entity was hard deleted.
    /// </summary>
    Delete = 3,

    /// <summary>
    /// Entity was soft deleted (IsDeleted = true).
    /// </summary>
    SoftDelete = 4,

    /// <summary>
    /// Entity was restored from soft delete.
    /// </summary>
    Restore = 5
}
