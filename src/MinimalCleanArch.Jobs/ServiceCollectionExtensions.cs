using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace MinimalCleanArch.Jobs;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="IJobScheduler"/> with the in-process hosted-service fallback.
    /// </summary>
    public static IServiceCollection AddJobs(
        this IServiceCollection services,
        Action<JobsOptions>? configure = null)
    {
        var options = new JobsOptions();
        configure?.Invoke(options);
        services.AddSingleton(options);
        services.TryAddSingleton<IJobExecutor, HandlerJobExecutor>();
        services.TryAddSingleton<HostedJobScheduler>();
        services.TryAddSingleton<IJobScheduler>(sp => sp.GetRequiredService<HostedJobScheduler>());
        services.AddHostedService(sp => sp.GetRequiredService<HostedJobScheduler>());
        return services;
    }
}
