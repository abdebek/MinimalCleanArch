using Microsoft.Extensions.DependencyInjection;

namespace MinimalCleanArch.Jobs;

/// <summary>
/// Resolves <see cref="IJobHandler{TJob}"/> from DI and invokes it.
/// </summary>
public sealed class HandlerJobExecutor : IJobExecutor
{
    private readonly IServiceScopeFactory _scopeFactory;

    public HandlerJobExecutor(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public async Task ExecuteAsync(object job, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(job);
        var handlerType = typeof(IJobHandler<>).MakeGenericType(job.GetType());
        using var scope = _scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetService(handlerType)
            ?? throw new InvalidOperationException($"No IJobHandler<{job.GetType().Name}> is registered.");
        var method = handlerType.GetMethod(nameof(IJobHandler<object>.HandleAsync))
            ?? throw new InvalidOperationException($"HandleAsync not found on {handlerType}.");
        await (Task)method.Invoke(handler, [job, cancellationToken])!;
    }
}
