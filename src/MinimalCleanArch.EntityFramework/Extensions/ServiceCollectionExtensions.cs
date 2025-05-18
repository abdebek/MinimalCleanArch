using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MinimalCleanArch.EntityFramework.Repositories;
using MinimalCleanArch.Repositories;

namespace MinimalCleanArch.EntityFramework.Extensions;

/// <summary>
/// Extension methods for IServiceCollection
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds MinimalCleanArch services to the service collection
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="optionsAction">The DbContext options action</param>
    /// <typeparam name="TContext">The type of the DbContext</typeparam>
    /// <returns>The service collection</returns>
    public static IServiceCollection AddMinimalCleanArch<TContext>(
        this IServiceCollection services,
        Action<DbContextOptionsBuilder> optionsAction) 
        where TContext : DbContext
    {
        services.AddDbContext<TContext>(optionsAction);
        services.AddScoped<DbContext>(provider => provider.GetService<TContext>()!);
        
        services.AddScoped(typeof(IRepository<,>), typeof(Repository<,>));
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        
        return services;
    }
}
