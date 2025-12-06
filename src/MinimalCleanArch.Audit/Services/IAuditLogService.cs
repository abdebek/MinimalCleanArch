using MinimalCleanArch.Audit.Entities;

namespace MinimalCleanArch.Audit.Services;

/// <summary>
/// Service for querying audit logs.
/// </summary>
public interface IAuditLogService
{
    /// <summary>
    /// Gets audit logs for a specific entity.
    /// </summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <param name="entityId">The entity ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of audit log entries for the entity.</returns>
    Task<IReadOnlyList<AuditLog>> GetEntityHistoryAsync<TEntity>(
        string entityId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets audit logs for a specific entity type.
    /// </summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <param name="skip">Number of records to skip.</param>
    /// <param name="take">Number of records to take.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of audit log entries.</returns>
    Task<IReadOnlyList<AuditLog>> GetByEntityTypeAsync<TEntity>(
        int skip = 0,
        int take = 50,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets audit logs for a specific user.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="skip">Number of records to skip.</param>
    /// <param name="take">Number of records to take.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of audit log entries by the user.</returns>
    Task<IReadOnlyList<AuditLog>> GetByUserAsync(
        string userId,
        int skip = 0,
        int take = 50,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets audit logs within a date range.
    /// </summary>
    /// <param name="from">Start date (inclusive).</param>
    /// <param name="to">End date (inclusive).</param>
    /// <param name="skip">Number of records to skip.</param>
    /// <param name="take">Number of records to take.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of audit log entries in the date range.</returns>
    Task<IReadOnlyList<AuditLog>> GetByDateRangeAsync(
        DateTime from,
        DateTime to,
        int skip = 0,
        int take = 50,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets audit logs by operation type.
    /// </summary>
    /// <param name="operation">The operation type.</param>
    /// <param name="skip">Number of records to skip.</param>
    /// <param name="take">Number of records to take.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of audit log entries for the operation.</returns>
    Task<IReadOnlyList<AuditLog>> GetByOperationAsync(
        AuditOperation operation,
        int skip = 0,
        int take = 50,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets audit logs by correlation ID.
    /// </summary>
    /// <param name="correlationId">The correlation ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of audit log entries with the correlation ID.</returns>
    Task<IReadOnlyList<AuditLog>> GetByCorrelationIdAsync(
        string correlationId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches audit logs with flexible criteria.
    /// </summary>
    /// <param name="query">The search query.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of matching audit log entries.</returns>
    Task<IReadOnlyList<AuditLog>> SearchAsync(
        AuditLogQuery query,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the count of audit logs matching the query.
    /// </summary>
    /// <param name="query">The search query.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Count of matching entries.</returns>
    Task<long> CountAsync(
        AuditLogQuery query,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Purges audit logs older than the specified date.
    /// </summary>
    /// <param name="olderThan">Delete logs older than this date.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Number of deleted entries.</returns>
    Task<int> PurgeAsync(
        DateTime olderThan,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Query parameters for searching audit logs.
/// </summary>
public class AuditLogQuery
{
    /// <summary>
    /// Filter by entity type name.
    /// </summary>
    public string? EntityType { get; set; }

    /// <summary>
    /// Filter by entity ID.
    /// </summary>
    public string? EntityId { get; set; }

    /// <summary>
    /// Filter by user ID.
    /// </summary>
    public string? UserId { get; set; }

    /// <summary>
    /// Filter by operation type.
    /// </summary>
    public AuditOperation? Operation { get; set; }

    /// <summary>
    /// Filter by start date (inclusive).
    /// </summary>
    public DateTime? FromDate { get; set; }

    /// <summary>
    /// Filter by end date (inclusive).
    /// </summary>
    public DateTime? ToDate { get; set; }

    /// <summary>
    /// Filter by correlation ID.
    /// </summary>
    public string? CorrelationId { get; set; }

    /// <summary>
    /// Filter by client IP address.
    /// </summary>
    public string? ClientIpAddress { get; set; }

    /// <summary>
    /// Number of records to skip.
    /// </summary>
    public int Skip { get; set; } = 0;

    /// <summary>
    /// Number of records to take.
    /// </summary>
    public int Take { get; set; } = 50;

    /// <summary>
    /// Sort order. True for descending (newest first). Default: true.
    /// </summary>
    public bool OrderByDescending { get; set; } = true;
}
