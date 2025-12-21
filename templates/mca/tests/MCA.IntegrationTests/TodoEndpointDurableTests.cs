using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using MCA.Application.DTOs;
using MCA.Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Testcontainers.PostgreSql;
using Testcontainers.SqlEdge;
using Xunit;

namespace MCA.IntegrationTests;

// These tests are opt-in: set RUN_DOCKER_E2E=1 to enable.
public class TodoEndpointDurableTests : IClassFixture<DurableApiFactory>, IAsyncLifetime
{
    private readonly HttpClient _client;
    private readonly DurableApiFactory _factory;

    public TodoEndpointDurableTests(DurableApiFactory factory)
    {
        _factory = factory;
        _client = factory.Enabled
            ? factory.CreateClient()
            : new HttpClient(new HttpClientHandler());
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public Task DisposeAsync()
    {
        _client.Dispose();
        return Task.CompletedTask;
    }

    [SkippableFact]
    public async Task CreateTodo_Persists_WithSqlServerOutbox()
    {
        Skip.IfNot(_factory.Enabled, "RUN_DOCKER_E2E not set");
        Skip.IfNot(_factory.DatabaseKind == "sqlserver", "Not running SQL Server variant");

        var request = new CreateTodoRequest("durable-sql", null, 1, null);

        var createResponse = await _client.PostAsJsonAsync("/api/todos", request);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var created = await createResponse.Content.ReadFromJsonAsync<TodoResponse>();
        created.Should().NotBeNull();

        var getResponse = await _client.GetAsync($"/api/todos/{created!.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [SkippableFact]
    public async Task CreateTodo_Persists_WithPostgresOutbox()
    {
        Skip.IfNot(_factory.Enabled, "RUN_DOCKER_E2E not set");
        Skip.IfNot(_factory.DatabaseKind == "postgres", "Not running Postgres variant");

        var request = new CreateTodoRequest("durable-pg", null, 1, null);

        var createResponse = await _client.PostAsJsonAsync("/api/todos", request);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var created = await createResponse.Content.ReadFromJsonAsync<TodoResponse>();
        created.Should().NotBeNull();

        var getResponse = await _client.GetAsync($"/api/todos/{created!.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}

public class DurableApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly bool _enabled;
    private readonly string _dbKind;
    private SqlEdgeContainer? _sqlServer;
    private PostgreSqlContainer? _postgres;

    public bool Enabled => _enabled;
    public string DatabaseKind => _dbKind;

    public DurableApiFactory()
    {
        _enabled = string.Equals(Environment.GetEnvironmentVariable("RUN_DOCKER_E2E"), "1", StringComparison.OrdinalIgnoreCase);
        _dbKind = Environment.GetEnvironmentVariable("RUN_DOCKER_DB")?.ToLowerInvariant() switch
        {
            "postgres" => "postgres",
            _ => "sqlserver"
        };
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        if (!_enabled)
        {
            // Prevent host startup when disabled
            builder.ConfigureServices(services =>
            {
                services.RemoveAll(typeof(DbContextOptions<AppDbContext>));
                services.AddDbContext<AppDbContext>(o => o.UseInMemoryDatabase("SkippedDb"));
            });
            return;
        }

        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((context, config) =>
        {
            var dict = new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = _dbKind switch
                {
                    "postgres" => _postgres?.GetConnectionString(),
                    _ => _sqlServer?.GetConnectionString()
                }
            };
            config.AddInMemoryCollection(dict!);
        });

        builder.ConfigureServices(services =>
        {
            services.RemoveAll(typeof(DbContextOptions<AppDbContext>));
            services.RemoveAll<AppDbContext>();

            if (_dbKind == "postgres")
            {
                services.AddDbContext<AppDbContext>(options =>
                {
                    options.UseNpgsql(_postgres!.GetConnectionString());
                    options.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
                });
            }
            else
            {
                services.AddDbContext<AppDbContext>(options =>
                {
                    options.UseSqlServer(_sqlServer!.GetConnectionString());
                    options.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
                });
            }
        });
    }

    public async Task InitializeAsync()
    {
        if (!_enabled)
        {
            return;
        }

        if (_dbKind == "postgres")
        {
            _postgres = new PostgreSqlBuilder()
                .WithDatabase("mca")
                .WithUsername("postgres")
                .WithPassword("postgres")
                .Build();
            await _postgres.StartAsync();
        }
        else
        {
            _sqlServer = new SqlEdgeBuilder()
                .WithPassword("YourStrong@Passw0rd")
                .WithPortBinding(0, 1433)
                .Build();
            await _sqlServer.StartAsync();
        }
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        if (_postgres is not null)
        {
            await _postgres.DisposeAsync();
        }

        if (_sqlServer is not null)
        {
            await _sqlServer.DisposeAsync();
        }

        await base.DisposeAsync();
    }
}
