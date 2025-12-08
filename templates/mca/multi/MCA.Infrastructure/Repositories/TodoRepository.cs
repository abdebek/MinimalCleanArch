using MCA.Domain.Entities;
using MCA.Domain.Interfaces;
using MCA.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using MinimalCleanArch.DataAccess.Repositories;

namespace MCA.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for Todo entities.
/// </summary>
public class TodoRepository : Repository<Todo>, ITodoRepository
{
    public TodoRepository(AppDbContext dbContext) : base(dbContext)
    {
    }

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
}
