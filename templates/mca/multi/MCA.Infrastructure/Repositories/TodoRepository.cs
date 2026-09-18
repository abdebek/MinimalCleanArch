using MCA.Domain.Entities;
using MCA.Domain.Interfaces;
using MCA.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using MinimalCleanArch.DataAccess.Repositories;
#if (UseMultiTenant)
using MinimalCleanArch.Execution;
#endif

namespace MCA.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for Todo entities.
/// </summary>
public class TodoRepository : Repository<Todo>, ITodoRepository
{
#if (UseMultiTenant)
    private readonly IExecutionContext _execution;

    public TodoRepository(AppDbContext dbContext, IExecutionContext execution) : base(dbContext)
    {
        _execution = execution;
    }
#else
    public TodoRepository(AppDbContext dbContext) : base(dbContext)
    {
    }
#endif

    public async Task<IReadOnlyList<Todo>> GetIncompleteByPriorityAsync(CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(t => !t.IsCompleted)
            .OrderByDescending(t => t.Priority)
            .ThenBy(t => t.DueDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Todo>> GetDueBeforeAsync(DateTime date, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(t => !t.IsCompleted && t.DueDate.HasValue && t.DueDate.Value < date)
            .OrderBy(t => t.DueDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<Todo?> GetByIdIncludingDeletedAsync(int id, CancellationToken cancellationToken = default)
    {
        var query = DbSet.IgnoreQueryFilters().Where(t => t.Id == id);
#if (UseMultiTenant)
        var tenantId = _execution.TenantId ?? string.Empty;
        query = query.Where(t => t.TenantId == tenantId);
#endif
        return await query.FirstOrDefaultAsync(cancellationToken);
    }

#if (UseJobs)
    public async Task<int> HardDeleteSoftDeletedOlderThanAsync(DateTime cutoffUtc, CancellationToken cancellationToken = default)
    {
        var doomed = await DbSet
            .IgnoreQueryFilters()
            .Where(t => t.IsDeleted && t.DeletedAt != null && t.DeletedAt < cutoffUtc)
            .ToListAsync(cancellationToken);
        DbSet.RemoveRange(doomed);
        return doomed.Count;
    }
#endif
}
