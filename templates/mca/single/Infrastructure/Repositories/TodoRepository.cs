using MCA.Domain.Entities;
using MCA.Domain.Interfaces;
using MCA.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using MinimalCleanArch.Repositories;
#if (UseMultiTenant)
using MinimalCleanArch.Execution;
#endif

namespace MCA.Infrastructure.Repositories;

public class TodoRepository : Repository<Todo, int>, ITodoRepository
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

    public async Task<IReadOnlyList<Todo>> GetByPriorityAsync(int priority, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(t => t.Priority == priority)
            .ToListAsync(cancellationToken);
    }

    public async Task<Todo?> GetByIdIncludingDeletedAsync(int id, CancellationToken cancellationToken = default)
    {
#if (UseNet10)
        var query = DbSet.IgnoreQueryFilters(["SoftDelete"]).Where(t => t.Id == id);
#else
        var query = DbSet.IgnoreQueryFilters().Where(t => t.Id == id);
#endif
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
