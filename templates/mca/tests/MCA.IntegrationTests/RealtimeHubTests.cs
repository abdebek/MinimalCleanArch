#if (UseRealtime)
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using MCA.Application.DTOs;
using MCA.Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace MCA.IntegrationTests;

public class RealtimeHubTests : IClassFixture<RealtimeTestApiFactory>
{
    private readonly RealtimeTestApiFactory _factory;

    public RealtimeHubTests(RealtimeTestApiFactory factory) => _factory = factory;

    [Fact]
    public async Task CreateTodo_PublishesEventOnTodosChannel()
    {
        var received = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);

        await using var connection = new HubConnectionBuilder()
            .WithUrl(new Uri(_factory.Server.BaseAddress!, "/hubs/realtime"), options =>
            {
                options.HttpMessageHandlerFactory = _ => _factory.Server.CreateHandler();
                options.Transports = HttpTransportType.LongPolling;
            })
            .Build();

        connection.On<object>("event", payload =>
        {
            received.TrySetResult(JsonSerializer.Serialize(payload));
        });

        await connection.StartAsync();
        await connection.InvokeAsync("Subscribe", "todos");

        using var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync(
            "/api/todos",
            new CreateTodoRequest("live", "from hub test", 1, null));
        response.EnsureSuccessStatusCode();

        var completed = await Task.WhenAny(received.Task, Task.Delay(TimeSpan.FromSeconds(5)));
        completed.Should().Be(received.Task, "hub client should receive the Todo write event");
        var json = await received.Task;
        json.Should().Contain("created");
        json.Should().Contain("todos");
        json.Should().Contain("live");
    }
}

public class RealtimeTestApiFactory : WebApplicationFactory<Program>
{
    private readonly string _dbName = $"RealtimeDb-{Guid.NewGuid()}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((_, configurationBuilder) =>
        {
            configurationBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Database:EnsureCreated"] = "false",
                ["RateLimiting:EnableGlobalLimiter"] = "false",
                ["RateLimiting:FixedPermitLimit"] = "10000",
                ["RateLimiting:TokenBucketLimit"] = "10000",
                ["RateLimiting:TokensPerPeriod"] = "10000"
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
