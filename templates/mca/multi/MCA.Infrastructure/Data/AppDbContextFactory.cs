using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace MCA.Infrastructure.Data;

/// <summary>
/// Design-time factory for <c>dotnet ef migrations</c> commands.
/// Prefer running EF tools with <c>--startup-project</c> pointed at the API project.
/// </summary>
public sealed class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        // When invoked from repo root or Infrastructure folder, prefer API appsettings if present.
        var candidates = new[]
        {
            Path.Combine(Directory.GetCurrentDirectory(), "MCA.Api"),
            Path.Combine(Directory.GetCurrentDirectory(), "..", "MCA.Api"),
            Directory.GetCurrentDirectory()
        };

        var basePath = candidates.FirstOrDefault(path =>
            File.Exists(Path.Combine(path, "appsettings.json")))
            ?? Directory.GetCurrentDirectory();

        var configuration = new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetConnectionString("DefaultConnection")
#if (UsePostgres)
            ?? "Host=localhost;Database=MCA;Username=postgres;Password=postgres";
#elif (UseSqlServer)
            ?? "Server=localhost;Database=MCA;Trusted_Connection=True;TrustServerCertificate=True";
#else
            ?? "Data Source=MCA.db";
#endif

        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
#if (UsePostgres)
        optionsBuilder.UseNpgsql(connectionString);
#elif (UseSqlServer)
        optionsBuilder.UseSqlServer(connectionString);
#else
        optionsBuilder.UseSqlite(connectionString);
#endif

        return new AppDbContext(optionsBuilder.Options);
    }
}
