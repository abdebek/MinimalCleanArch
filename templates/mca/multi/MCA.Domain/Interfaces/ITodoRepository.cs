using MCA.Domain.Entities;
using MinimalCleanArch.Repositories;

namespace MCA.Domain.Interfaces;

/// <summary>
/// Repository interface for Todo entities.
/// </summary>
public interface ITodoRepository : IRepository<Todo>
{
    /// <summary>
    /// Gets all incomplete todos ordered by priority.
    /// </summary>
    Task<IReadOnlyList<Todo>> GetIncompleteByPriorityAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all todos due before the specified date.
    /// </summary>
    Task<IReadOnlyList<Todo>> GetDueBeforeAsync(DateTime date, CancellationToken cancellationToken = default);
}
