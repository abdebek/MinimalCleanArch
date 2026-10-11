using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.RateLimiting;
using FluentAssertions;
using MCA.Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Xunit;

namespace MCA.IntegrationTests;

public class RateLimitingEndpointTests
{
    [Fact]
    public async Task GlobalLimiter_WhenExceeded_Returns429ProblemDetails()
    {
        await using var factory = new RateLimitTestApiFactory();
        using var client = factory.CreateClient();

        var first = await client.GetAsync("/api/todos");
        var second = await client.GetAsync("/api/todos");

        first.StatusCode.Should().Be(HttpStatusCode.OK);
        second.StatusCode.Should().Be((HttpStatusCode)429);
        second.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");

        var problem = await second.Content.ReadFromJsonAsync<JsonElement>();
        problem.GetProperty("status").GetInt32().Should().Be(429);
        problem.GetProperty("title").GetString().Should().NotBeNullOrWhiteSpace();
        problem.GetProperty("detail").GetString().Should().NotBeNullOrWhiteSpace();
    }
}

public class RateLimitTestApiFactory : WebApplicationFactory<Program>
{
    private readonly string _dbName = $"RateLimitTestDb-{Guid.NewGuid()}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((_, configurationBuilder) =>
        {
            configurationBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Database:EnsureCreated"] = "false",
                ["RateLimiting:EnableGlobalLimiter"] = "true",
                ["RateLimiting:GlobalPermitLimit"] = "1",
                ["RateLimiting:GlobalWindow"] = "00:01:00",
                ["RateLimiting:GlobalQueueLimit"] = "0",
                ["RateLimiting:UseForwardedHeaders"] = "false"
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

            // Wolverine registers IAsyncDisposable-only services — must not Dispose() the temp provider.
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

        // Force a global permit of 1 after Program.cs registration (config bind can race with host sources).
        builder.ConfigureTestServices(services =>
        {
            services.AddSingleton<IPostConfigureOptions<RateLimiterOptions>>(_ =>
                new PostConfigureOptions<RateLimiterOptions>(Options.DefaultName, options =>
                {
                    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(_ =>
                        RateLimitPartition.GetFixedWindowLimiter(
                            "rate-limit-test",
                            static _ => new FixedWindowRateLimiterOptions
                            {
                                PermitLimit = 1,
                                Window = TimeSpan.FromMinutes(1),
                                QueueLimit = 0,
                                QueueProcessingOrder = QueueProcessingOrder.OldestFirst
                            }));
                }));
        });
    }
}
