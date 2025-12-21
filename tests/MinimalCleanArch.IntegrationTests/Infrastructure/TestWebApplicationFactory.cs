using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Collections.Generic;
using MinimalCleanArch.Sample.Domain.Entities;
using MinimalCleanArch.Sample.Infrastructure.Data;
using MinimalCleanArch.Security.Configuration;
using MinimalCleanArch.Security.Encryption;

namespace MinimalCleanArch.IntegrationTests.Infrastructure;

public sealed class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _databasePath;

    public TestWebApplicationFactory()
    {
        _databasePath = Path.Combine(Path.GetTempPath(), $"mca_tests_{Guid.NewGuid():N}.db");
    }

    public async Task EnsureDatabaseCreatedAsync()
    {
        using var scope = Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await dbContext.Database.EnsureCreatedAsync();
    }

    public async Task<User> CreateUserAsync(string email, string password, params string[] roles)
    {
        using var scope = Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        foreach (var role in roles.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }

        var user = new User
        {
            UserName = email,
            Email = email,
            FullName = "Test User"
        };

        var result = await userManager.CreateAsync(user, password);
        if (!result.Succeeded)
        {
            var errors = string.Join("; ", result.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Failed to create user: {errors}");
        }

        foreach (var role in roles.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            var roleResult = await userManager.AddToRoleAsync(user, role);
            if (!roleResult.Succeeded)
            {
                var errors = string.Join("; ", roleResult.Errors.Select(e => e.Description));
                throw new InvalidOperationException($"Failed to assign role '{role}': {errors}");
            }
        }

        return user;
    }

    public HttpClient CreateAuthenticatedClient(string userId, string email, params string[] roles)
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.UserIdHeader, userId);
        client.DefaultRequestHeaders.Add(TestAuthHandler.EmailHeader, email);

        if (roles.Length > 0)
        {
            client.DefaultRequestHeaders.Add(TestAuthHandler.RolesHeader, string.Join(",", roles));
        }

        return client;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((context, config) =>
        {
            var overrides = new Dictionary<string, string?>
            {
                ["Logging:LogLevel:Default"] = "Warning",
                ["Logging:LogLevel:Microsoft"] = "Warning",
                ["Logging:LogLevel:Microsoft.EntityFrameworkCore"] = "Error",
                ["Logging:LogLevel:Microsoft.EntityFrameworkCore.Model.Validation"] = "Error",
                ["Serilog:MinimumLevel:Default"] = "Warning",
                ["Serilog:MinimumLevel:Override:Microsoft"] = "Warning",
                ["Serilog:MinimumLevel:Override:Microsoft.EntityFrameworkCore"] = "Error",
                ["Serilog:MinimumLevel:Override:Microsoft.AspNetCore"] = "Warning"
            };

            config.AddInMemoryCollection(overrides);
        });
        builder.ConfigureServices(services =>
        {
            var dbContextDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
            if (dbContextDescriptor != null)
            {
                services.Remove(dbContextDescriptor);
            }

            var contextDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(ApplicationDbContext));
            if (contextDescriptor != null)
            {
                services.Remove(contextDescriptor);
            }

            services.RemoveAll<EncryptionOptions>();
            services.RemoveAll<IEncryptionService>();

            var encryptionOptions = new EncryptionOptions
            {
                Key = EncryptionOptions.GenerateStrongKey(64),
                ValidateKeyStrength = false,
                EnableOperationLogging = false,
                AllowEnvironmentVariables = false
            };

            services.AddSingleton(encryptionOptions);
            services.AddSingleton<IEncryptionService, AesEncryptionService>();

            services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseSqlite($"Data Source={_databasePath}");
                options.EnableServiceProviderCaching(false);
                options.EnableSensitiveDataLogging();
            });

            services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = TestAuthHandler.SchemeName;
                    options.DefaultChallengeScheme = TestAuthHandler.SchemeName;
                })
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, _ => { });
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (!disposing)
        {
            return;
        }

        try
        {
            if (File.Exists(_databasePath))
            {
                File.Delete(_databasePath);
            }
        }
        catch
        {
            // Ignore cleanup failures in tests.
        }
    }
}
