#if (UseFeatures)
using System.Net;
using FluentAssertions;
using MCA.Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace MCA.IntegrationTests;

public class FeatureGateTests
{
    [Fact]
    public async Task ExportTodos_DisabledInConfig_ReturnsForbidden()
    {
        await using var factory = new FeatureTestApiFactory(todoExportEnabled: false);
        using var client = factory.CreateClient();
        var response = await client.GetAsync("/api/todos/export");
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ExportTodos_EnabledInConfig_ReturnsOk()
    {
        await using var factory = new FeatureTestApiFactory(todoExportEnabled: true);
        using var client = factory.CreateClient();
        var response = await client.GetAsync("/api/todos/export");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}

public class FeatureTestApiFactory(bool todoExportEnabled) : WebApplicationFactory<Program>
{
    private readonly string _dbName = $"FeatureDb-{Guid.NewGuid()}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((_, configurationBuilder) =>
        {
            configurationBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Database:EnsureCreated"] = "false",
                ["RateLimiting:EnableGlobalLimiter"] = "false",
                ["Features:Flags:todo-export"] = todoExportEnabled ? "true" : "false"
            });
        });
        builder.ConfigureServices(services =>
        {
            services.RemoveAll(typeof(DbContextOptions<AppDbContext>));
            services.RemoveAll(typeof(IDbContextOptionsConfiguration<AppDbContext>));
            services.RemoveAll<AppDbContext>();

            services.AddDbContext<AppDbContext>(options =>
            {
                options.UseInMemoryDatabase(_dbName);
                options.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
#if (UseAuth)
                options.UseOpenIddict<Guid>();
#endif
            });

            var sp = services.BuildServiceProvider();
            try
            {
                using var scope = sp.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                db.Database.EnsureCreated();
            }
            finally
            {
                if (sp is IAsyncDisposable asyncDisposable)
                    asyncDisposable.DisposeAsync().AsTask().GetAwaiter().GetResult();
                else
                    sp.Dispose();
            }
        });
    }
}
#endif
