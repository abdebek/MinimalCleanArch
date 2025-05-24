using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MinimalCleanArch.DataAccess.Repositories;
using MinimalCleanArch.Repositories;

namespace MinimalCleanArch.DataAccess.Extensions;

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
        // Register DbContext
        services.AddDbContext<TContext>(optionsAction);
        
        // Register DbContext as base class for DI
        services.AddScoped<DbContext>(provider => provider.GetRequiredService<TContext>());
        
        // Register Unit of Work
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        
        // Register Repository implementations
        services.AddScoped(typeof(IRepository<,>), typeof(Repository<,>));
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        
        return services;
    }

    /// <summary>
    /// Adds MinimalCleanArch services with a specific DbContext instance
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="dbContextFactory">Factory function to create the DbContext</param>
    /// <typeparam name="TContext">The type of the DbContext</typeparam>
    /// <returns>The service collection</returns>
    public static IServiceCollection AddMinimalCleanArch<TContext>(
        this IServiceCollection services,
        Func<IServiceProvider, TContext> dbContextFactory) 
        where TContext : DbContext
    {
        // Register DbContext with factory
        services.AddScoped<TContext>(dbContextFactory);
        
        // Register DbContext as base class for DI
        services.AddScoped<DbContext>(provider => provider.GetRequiredService<TContext>());
        
        // Register Unit of Work
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        
        // Register Repository implementations
        services.AddScoped(typeof(IRepository<,>), typeof(Repository<,>));
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        
        return services;
    }

    /// <summary>
    /// Adds only the repository services (assumes DbContext is already registered)
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection</returns>
    public static IServiceCollection AddMinimalCleanArchRepositories(this IServiceCollection services)
    {
        // Register Unit of Work
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        
        // Register Repository implementations
        services.AddScoped(typeof(IRepository<,>), typeof(Repository<,>));
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        
        return services;
    }

    /// <summary>
    /// Adds MinimalCleanArch services with custom Unit of Work implementation
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="optionsAction">The DbContext options action</param>
    /// <typeparam name="TContext">The type of the DbContext</typeparam>
    /// <typeparam name="TUnitOfWork">The type of the Unit of Work implementation</typeparam>
    /// <returns>The service collection</returns>
    public static IServiceCollection AddMinimalCleanArch<TContext, TUnitOfWork>(
        this IServiceCollection services,
        Action<DbContextOptionsBuilder> optionsAction) 
        where TContext : DbContext
        where TUnitOfWork : class, IUnitOfWork
    {
        // Register DbContext
        services.AddDbContext<TContext>(optionsAction);
        
        // Register DbContext as base class for DI
        services.AddScoped<DbContext>(provider => provider.GetRequiredService<TContext>());
        
        // Register custom Unit of Work
        services.AddScoped<IUnitOfWork, TUnitOfWork>();
        
        // Register Repository implementations
        services.AddScoped(typeof(IRepository<,>), typeof(Repository<,>));
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        
        return services;
    }
}