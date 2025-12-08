using MCA.Domain.Entities;
using MinimalCleanArch.Repositories;

namespace MCA.Domain.Interfaces;

public interface ITodoRepository : IRepository<Todo, int>
{
    Task<IEnumerable<Todo>> GetByPriorityAsync(int priority, CancellationToken cancellationToken = default);
}
