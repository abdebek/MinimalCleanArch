using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MinimalCleanArch.Extensions.Hosting;

/// <summary>
/// Interface for implementing database seeders that run at application startup.
/// </summary>
public interface IDatabaseSeeder
{
    /// <summary>
    /// Gets the order in which this seeder should run. Lower numbers run first.
    /// </summary>
    int Order => 0;

    /// <summary>
    /// Seeds the database with initial data.
    /// </summary>
    /// <param name="serviceProvider">The service provider for resolving dependencies.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SeedAsync(IServiceProvider serviceProvider, CancellationToken cancellationToken = default);
}

/// <summary>
/// Hosted service that runs database seeders at application startup.
/// Seeders are executed in order based on their Order property.
/// </summary>
public class DatabaseSeederHostedService : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DatabaseSeederHostedService> _logger;
    private readonly IHostEnvironment _environment;

    /// <summary>
    /// Initializes a new instance of the <see cref="DatabaseSeederHostedService"/> class.
    /// </summary>
    public DatabaseSeederHostedService(
        IServiceProvider serviceProvider,
        ILogger<DatabaseSeederHostedService> logger,
        IHostEnvironment environment)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _environment = environment;
    }

    /// <summary>
    /// Runs all registered database seeders in order.
    /// </summary>
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting database seeding...");

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var seeders = scope.ServiceProvider
                .GetServices<IDatabaseSeeder>()
                .OrderBy(s => s.Order)
                .ToList();

            if (seeders.Count == 0)
            {
                _logger.LogDebug("No database seeders registered");
                return;
            }

            _logger.LogInformation("Found {Count} database seeder(s) to execute", seeders.Count);

            foreach (var seeder in seeders)
            {
                var seederName = seeder.GetType().Name;
                _logger.LogInformation("Executing seeder: {SeederName} (Order: {Order})", seederName, seeder.Order);

                try
                {
                    await seeder.SeedAsync(scope.ServiceProvider, cancellationToken);
                    _logger.LogInformation("Seeder {SeederName} completed successfully", seederName);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Seeder {SeederName} failed", seederName);

                    // In production, we might want to continue with other seeders
                    // In development, fail fast
                    if (_environment.IsDevelopment())
                    {
                        throw;
                    }
                }
            }

            _logger.LogInformation("Database seeding completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database seeding failed");
            throw;
        }
    }

    /// <summary>
    /// No cleanup needed on stop.
    /// </summary>
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}

/// <summary>
/// Extension methods for registering database seeders.
/// </summary>
public static class DatabaseSeederExtensions
{
    /// <summary>
    /// Adds the database seeder hosted service and registers seeders.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddDatabaseSeeding(this IServiceCollection services)
    {
        services.AddHostedService<DatabaseSeederHostedService>();
        return services;
    }

    /// <summary>
    /// Registers a database seeder to be executed at startup.
    /// </summary>
    /// <typeparam name="TSeeder">The seeder type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddSeeder<TSeeder>(this IServiceCollection services)
        where TSeeder : class, IDatabaseSeeder
    {
        services.AddScoped<IDatabaseSeeder, TSeeder>();
        return services;
    }

    /// <summary>
    /// Adds the database seeder hosted service and registers the specified seeders.
    /// </summary>
    /// <typeparam name="TSeeder">The first seeder type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddDatabaseSeeding<TSeeder>(this IServiceCollection services)
        where TSeeder : class, IDatabaseSeeder
    {
        return services
            .AddDatabaseSeeding()
            .AddSeeder<TSeeder>();
    }

    /// <summary>
    /// Adds the database seeder hosted service and registers the specified seeders.
    /// </summary>
    /// <typeparam name="TSeeder1">The first seeder type.</typeparam>
    /// <typeparam name="TSeeder2">The second seeder type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddDatabaseSeeding<TSeeder1, TSeeder2>(this IServiceCollection services)
        where TSeeder1 : class, IDatabaseSeeder
        where TSeeder2 : class, IDatabaseSeeder
    {
        return services
            .AddDatabaseSeeding()
            .AddSeeder<TSeeder1>()
            .AddSeeder<TSeeder2>();
    }
}
