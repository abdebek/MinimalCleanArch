using MCA.Domain.Entities;
using MinimalCleanArch.Repositories;

namespace MCA.Domain.Interfaces;

public interface ITodoRepository : IRepository<Todo, int>
{
    Task<IReadOnlyList<Todo>> GetIncompleteByPriorityAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Todo>> GetDueBeforeAsync(DateTime date, CancellationToken cancellationToken = default);
    Task<Todo?> GetByIdIncludingDeletedAsync(int id, CancellationToken cancellationToken = default);
#if (UseJobs)
    Task<int> HardDeleteSoftDeletedOlderThanAsync(DateTime cutoffUtc, CancellationToken cancellationToken = default);
#endif
}
