using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MinimalCleanArch.Extensions.Hosting;
using MinimalCleanArch.Sample.Domain.Entities;
using MinimalCleanArch.Sample.Infrastructure.Data;

namespace MinimalCleanArch.Sample.Infrastructure.Seeders;

/// <summary>
/// Seeds the database with initial roles and admin user.
/// </summary>
public class RoleSeeder : IDatabaseSeeder
{
    /// <summary>
    /// Runs before user seeder.
    /// </summary>
    public int Order => 1;

    public async Task SeedAsync(IServiceProvider serviceProvider, CancellationToken cancellationToken = default)
    {
        var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var logger = serviceProvider.GetRequiredService<ILogger<RoleSeeder>>();

        var roles = new[] { "Admin", "User" };

        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                var result = await roleManager.CreateAsync(new IdentityRole(role));
                if (result.Succeeded)
                {
                    logger.LogInformation("Created role: {Role}", role);
                }
                else
                {
                    logger.LogError("Failed to create role {Role}: {Errors}", role,
                        string.Join(", ", result.Errors.Select(e => e.Description)));
                }
            }
        }
    }
}

/// <summary>
/// Seeds the database with initial users.
/// </summary>
public class UserSeeder : IDatabaseSeeder
{
    /// <summary>
    /// Runs after role seeder.
    /// </summary>
    public int Order => 2;

    public async Task SeedAsync(IServiceProvider serviceProvider, CancellationToken cancellationToken = default)
    {
        var userManager = serviceProvider.GetRequiredService<UserManager<User>>();
        var configuration = serviceProvider.GetRequiredService<IConfiguration>();
        var logger = serviceProvider.GetRequiredService<ILogger<UserSeeder>>();

        // Get credentials from configuration with fallback defaults for development
        var adminEmail = configuration["Seeding:AdminEmail"] ?? "admin@example.com";
        var adminPassword = configuration["Seeding:AdminPassword"] ?? "Admin123!";
        var userEmail = configuration["Seeding:UserEmail"] ?? "user@example.com";
        var userPassword = configuration["Seeding:UserPassword"] ?? "User123!";

        // Create admin user
        await CreateUserWithRoleAsync(
            userManager,
            logger,
            new User
            {
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true,
                FullName = "System Administrator",
                PersonalNotes = "Initial admin user created during setup"
            },
            adminPassword,
            "Admin");

        // Create regular user
        await CreateUserWithRoleAsync(
            userManager,
            logger,
            new User
            {
                UserName = userEmail,
                Email = userEmail,
                EmailConfirmed = true,
                FullName = "Test User",
                PersonalNotes = "Regular user for testing purposes"
            },
            userPassword,
            "User");
    }

    private static async Task CreateUserWithRoleAsync(
        UserManager<User> userManager,
        ILogger logger,
        User user,
        string password,
        string role)
    {
        var existingUser = await userManager.FindByEmailAsync(user.Email!);

        if (existingUser == null)
        {
            var result = await userManager.CreateAsync(user, password);
            if (result.Succeeded)
            {
                var roleResult = await userManager.AddToRoleAsync(user, role);
                if (roleResult.Succeeded)
                {
                    logger.LogInformation("Created user {Email} with role {Role}", user.Email, role);
                }
                else
                {
                    logger.LogError("Failed to assign {Role} role to {Email}: {Errors}",
                        role, user.Email, string.Join(", ", roleResult.Errors.Select(e => e.Description)));
                }
            }
            else
            {
                logger.LogError("Failed to create user {Email}: {Errors}",
                    user.Email, string.Join(", ", result.Errors.Select(e => e.Description)));
            }
        }
        else
        {
            // Ensure existing user has the correct role
            if (!await userManager.IsInRoleAsync(existingUser, role))
            {
                var roleResult = await userManager.AddToRoleAsync(existingUser, role);
                if (roleResult.Succeeded)
                {
                    logger.LogInformation("Added {Role} role to existing user {Email}", role, user.Email);
                }
                else
                {
                    logger.LogError("Failed to add {Role} role to existing user {Email}: {Errors}",
                        role, user.Email, string.Join(", ", roleResult.Errors.Select(e => e.Description)));
                }
            }
        }
    }
}

/// <summary>
/// Ensures the database is created and migrated.
/// </summary>
public class DatabaseMigrationSeeder : IDatabaseSeeder
{
    /// <summary>
    /// Runs first to ensure database exists.
    /// </summary>
    public int Order => 0;

    public async Task SeedAsync(IServiceProvider serviceProvider, CancellationToken cancellationToken = default)
    {
        var dbContext = serviceProvider.GetRequiredService<ApplicationDbContext>();
        var logger = serviceProvider.GetRequiredService<ILogger<DatabaseMigrationSeeder>>();
        var environment = serviceProvider.GetRequiredService<Microsoft.Extensions.Hosting.IHostEnvironment>();

        if (environment.IsDevelopment())
        {
            if (!await dbContext.Database.CanConnectAsync(cancellationToken))
            {
                logger.LogInformation("Creating database...");
                await dbContext.Database.EnsureCreatedAsync(cancellationToken);
                logger.LogInformation("Database created successfully");
            }
            else
            {
                logger.LogDebug("Database already exists");
            }
        }
        else
        {
            // In production, apply migrations instead of EnsureCreated
            logger.LogInformation("Applying database migrations...");
            await dbContext.Database.MigrateAsync(cancellationToken);
            logger.LogInformation("Database migrations applied successfully");
        }
    }
}
