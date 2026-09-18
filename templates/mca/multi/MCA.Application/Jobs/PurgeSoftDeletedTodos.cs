using MCA.Domain.Interfaces;
using MinimalCleanArch.Jobs;
using MinimalCleanArch.Repositories;

namespace MCA.Application.Jobs;

public sealed record PurgeSoftDeletedTodos(TimeSpan Retention);

public sealed class PurgeSoftDeletedTodosHandler : IJobHandler<PurgeSoftDeletedTodos>
{
    private readonly ITodoRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public PurgeSoftDeletedTodosHandler(ITodoRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    Task IJobHandler<PurgeSoftDeletedTodos>.HandleAsync(
        PurgeSoftDeletedTodos job,
        CancellationToken cancellationToken)
        => Handle(job, cancellationToken);

    public async Task Handle(PurgeSoftDeletedTodos job, CancellationToken cancellationToken)
    {
        var cutoff = DateTime.UtcNow - job.Retention;
        await _repository.HardDeleteSoftDeletedOlderThanAsync(cutoff, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
