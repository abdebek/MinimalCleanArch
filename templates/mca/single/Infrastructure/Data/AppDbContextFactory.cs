using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace MCA.Infrastructure.Data;

/// <summary>
/// Design-time factory for <c>dotnet ef migrations</c> commands.
/// </summary>
public sealed class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var basePath = Directory.GetCurrentDirectory();
        var configuration = new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetConnectionString("DefaultConnection")
#if (UsePostgres)
            ?? "Host=localhost;Database=__DB_NAME__;Username=postgres;Password=postgres";
#elif (UseSqlServer)
            ?? "Server=localhost;Database=__DB_NAME__;Trusted_Connection=True;TrustServerCertificate=True";
#else
            ?? "Data Source=__DB_NAME__.db";
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
