using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MCA.Infrastructure.Data;

/// <summary>
/// Applies database schema at startup based on configuration and provider.
/// </summary>
public static class DatabaseInitializer
{
    /// <summary>
    /// Initializes the database using either EF migrations or EnsureCreated.
    /// </summary>
    public static async Task InitializeAsync(
        IServiceProvider services,
        IConfiguration configuration,
        IHostEnvironment environment,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        var ensureCreated = configuration.GetValue("Database:EnsureCreated", false);
        var applyMigrations = configuration.GetValue("Database:ApplyMigrations", false);

        if (!ensureCreated && !applyMigrations)
        {
            logger.LogDebug("Database auto-initialization is disabled (Database:EnsureCreated and Database:ApplyMigrations are false).");
            return;
        }

        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        if (applyMigrations && db.Database.IsRelational())
        {
            var migrations = db.Database.GetMigrations().ToList();
            if (migrations.Count > 0)
            {
                await db.Database.MigrateAsync(cancellationToken);
                logger.LogInformation("Applied EF Core migrations ({Count} defined).", migrations.Count);
            }
            else if (environment.IsDevelopment() || ensureCreated)
            {
                await db.Database.EnsureCreatedAsync(cancellationToken);
                logger.LogWarning(
                    "No EF Core migrations found; used EnsureCreated for local development. " +
                    "Create migrations with: dotnet ef migrations add InitialCreate " +
                    "(see templates README / Database section).");
            }
            else
            {
                throw new InvalidOperationException(
                    "Database:ApplyMigrations is true but no EF Core migrations were found, " +
                    "and EnsureCreated is not allowed outside Development. " +
                    "Add migrations before deploying.");
            }
        }
        else if (ensureCreated)
        {
            await db.Database.EnsureCreatedAsync(cancellationToken);
            logger.LogWarning(
                "Database ensured via EnsureCreated. Prefer Database:ApplyMigrations with EF migrations for SQL Server/PostgreSQL.");
        }
        else if (applyMigrations)
        {
            // applyMigrations was requested but the provider is non-relational (e.g. EF Core
            // InMemory used in tests). Nothing to do; the host/test is responsible for schema.
            logger.LogDebug(
                "Database:ApplyMigrations is true but the context is not relational; skipping schema initialization.");
        }
    }
}
