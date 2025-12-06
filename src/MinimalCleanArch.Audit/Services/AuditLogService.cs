using Microsoft.EntityFrameworkCore;
using MinimalCleanArch.Audit.Entities;

namespace MinimalCleanArch.Audit.Services;

/// <summary>
/// Default implementation of <see cref="IAuditLogService"/>.
/// </summary>
public class AuditLogService : IAuditLogService
{
    private readonly DbContext _context;

    /// <summary>
    /// Initializes a new instance of the <see cref="AuditLogService"/> class.
    /// </summary>
    /// <param name="context">The database context containing AuditLog DbSet.</param>
    public AuditLogService(DbContext context)
    {
        _context = context;
    }

    private IQueryable<AuditLog> AuditLogs => _context.Set<AuditLog>().AsNoTracking();

    /// <inheritdoc />
    public async Task<IReadOnlyList<AuditLog>> GetEntityHistoryAsync<TEntity>(
        string entityId,
        CancellationToken cancellationToken = default)
    {
        var entityType = typeof(TEntity).Name;

        return await AuditLogs
            .Where(a => a.EntityType == entityType && a.EntityId == entityId)
            .OrderByDescending(a => a.Timestamp)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<AuditLog>> GetByEntityTypeAsync<TEntity>(
        int skip = 0,
        int take = 50,
        CancellationToken cancellationToken = default)
    {
        var entityType = typeof(TEntity).Name;

        return await AuditLogs
            .Where(a => a.EntityType == entityType)
            .OrderByDescending(a => a.Timestamp)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<AuditLog>> GetByUserAsync(
        string userId,
        int skip = 0,
        int take = 50,
        CancellationToken cancellationToken = default)
    {
        return await AuditLogs
            .Where(a => a.UserId == userId)
            .OrderByDescending(a => a.Timestamp)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<AuditLog>> GetByDateRangeAsync(
        DateTime from,
        DateTime to,
        int skip = 0,
        int take = 50,
        CancellationToken cancellationToken = default)
    {
        return await AuditLogs
            .Where(a => a.Timestamp >= from && a.Timestamp <= to)
            .OrderByDescending(a => a.Timestamp)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<AuditLog>> GetByOperationAsync(
        AuditOperation operation,
        int skip = 0,
        int take = 50,
        CancellationToken cancellationToken = default)
    {
        return await AuditLogs
            .Where(a => a.Operation == operation)
            .OrderByDescending(a => a.Timestamp)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<AuditLog>> GetByCorrelationIdAsync(
        string correlationId,
        CancellationToken cancellationToken = default)
    {
        return await AuditLogs
            .Where(a => a.CorrelationId == correlationId)
            .OrderBy(a => a.Timestamp)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<AuditLog>> SearchAsync(
        AuditLogQuery query,
        CancellationToken cancellationToken = default)
    {
        var queryable = BuildQuery(query);

        if (query.OrderByDescending)
            queryable = queryable.OrderByDescending(a => a.Timestamp);
        else
            queryable = queryable.OrderBy(a => a.Timestamp);

        return await queryable
            .Skip(query.Skip)
            .Take(query.Take)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<long> CountAsync(
        AuditLogQuery query,
        CancellationToken cancellationToken = default)
    {
        var queryable = BuildQuery(query);
        return await queryable.LongCountAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<int> PurgeAsync(
        DateTime olderThan,
        CancellationToken cancellationToken = default)
    {
        return await _context.Set<AuditLog>()
            .Where(a => a.Timestamp < olderThan)
            .ExecuteDeleteAsync(cancellationToken);
    }

    private IQueryable<AuditLog> BuildQuery(AuditLogQuery query)
    {
        var queryable = AuditLogs;

        if (!string.IsNullOrEmpty(query.EntityType))
            queryable = queryable.Where(a => a.EntityType == query.EntityType);

        if (!string.IsNullOrEmpty(query.EntityId))
            queryable = queryable.Where(a => a.EntityId == query.EntityId);

        if (!string.IsNullOrEmpty(query.UserId))
            queryable = queryable.Where(a => a.UserId == query.UserId);

        if (query.Operation.HasValue)
            queryable = queryable.Where(a => a.Operation == query.Operation.Value);

        if (query.FromDate.HasValue)
            queryable = queryable.Where(a => a.Timestamp >= query.FromDate.Value);

        if (query.ToDate.HasValue)
            queryable = queryable.Where(a => a.Timestamp <= query.ToDate.Value);

        if (!string.IsNullOrEmpty(query.CorrelationId))
            queryable = queryable.Where(a => a.CorrelationId == query.CorrelationId);

        if (!string.IsNullOrEmpty(query.ClientIpAddress))
            queryable = queryable.Where(a => a.ClientIpAddress == query.ClientIpAddress);

        return queryable;
    }
}
