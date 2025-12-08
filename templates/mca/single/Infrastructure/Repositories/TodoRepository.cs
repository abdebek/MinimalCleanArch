using MCA.Domain.Entities;
using MCA.Domain.Interfaces;
using MCA.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using MinimalCleanArch.Repositories;

namespace MCA.Infrastructure.Repositories;

public class TodoRepository : Repository<Todo, int>, ITodoRepository
{
    public TodoRepository(AppDbContext dbContext) : base(dbContext)
    {
    }

    public async Task<IReadOnlyList<Todo>> GetByPriorityAsync(int priority, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(t => t.Priority == priority)
            .ToListAsync(cancellationToken);
    }
}
