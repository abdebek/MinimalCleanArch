using MCA.Domain.Interfaces;
using MinimalCleanArch.Jobs;
using MinimalCleanArch.Repositories;

namespace MCA.Application.Jobs;

public sealed record PurgeSoftDeletedTodos(TimeSpan Retention);

public sealed class PurgeSoftDeletedTodosHandler : IJobHandler<PurgeSoftDeletedTodos>
{
    private readonly ITodoRepository _todoRepository;
    private readonly IUnitOfWork _unitOfWork;

    public PurgeSoftDeletedTodosHandler(ITodoRepository todoRepository, IUnitOfWork unitOfWork)
    {
        _todoRepository = todoRepository;
        _unitOfWork = unitOfWork;
    }

    Task IJobHandler<PurgeSoftDeletedTodos>.HandleAsync(
        PurgeSoftDeletedTodos job,
        CancellationToken cancellationToken)
        => Handle(job, cancellationToken);

    public async Task Handle(PurgeSoftDeletedTodos job, CancellationToken cancellationToken)
    {
        var cutoff = DateTime.UtcNow - job.Retention;
        await _todoRepository.HardDeleteSoftDeletedOlderThanAsync(cutoff, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
