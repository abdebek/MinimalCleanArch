using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using MCA.Application.DTOs;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
#if (UsePostgres)
using Testcontainers.PostgreSql;
#endif
#if (UseSqlServer)
using Microsoft.Data.SqlClient;
using Testcontainers.MsSql;
#endif
using Xunit;

namespace MCA.IntegrationTests;

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
    public async Task ScalarUi_LoadsInDevelopment()
    {
        Skip.IfNot(_factory.Enabled, "RUN_DOCKER_E2E not set");

        var response = await _client.GetAsync("/scalar/v1");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [SkippableFact]
    public async Task GetTodos_InitiallyEmpty_ReturnsOkAndEmptyList()
    {
        Skip.IfNot(_factory.Enabled, "RUN_DOCKER_E2E not set");

        await using var isolatedFactory = new DurableApiFactory();
        await isolatedFactory.InitializeAsync();
        using var isolatedClient = isolatedFactory.CreateClient();

        var response = await isolatedClient.GetAsync("/api/todos");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<List<TodoResponse>>();
        payload.Should().NotBeNull();
        payload!.Should().BeEmpty();

        await ((IAsyncLifetime)isolatedFactory).DisposeAsync();
    }

    [SkippableFact]
    public async Task CreateTodo_ValidRequest_ReturnsCreatedAndPersists()
    {
        Skip.IfNot(_factory.Enabled, "RUN_DOCKER_E2E not set");

        var request = new CreateTodoRequest("durable-item", "desc", 1, DateTime.UtcNow.AddDays(1));

        var createResponse = await _client.PostAsJsonAsync("/api/todos", request);

        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await createResponse.Content.ReadFromJsonAsync<TodoResponse>();
        created.Should().NotBeNull();
        created!.Title.Should().Be("durable-item");

        var getResponse = await _client.GetAsync($"/api/todos/{created.Id}");

        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var fetched = await getResponse.Content.ReadFromJsonAsync<TodoResponse>();
        fetched.Should().NotBeNull();
        fetched!.Id.Should().Be(created.Id);
    }

#if (UseValidation)
    [SkippableFact]
    public async Task CreateTodo_InvalidRequest_ReturnsBadRequest()
    {
        Skip.IfNot(_factory.Enabled, "RUN_DOCKER_E2E not set");

        var request = new CreateTodoRequest(string.Empty, null, 0, null);

        var response = await _client.PostAsJsonAsync("/api/todos", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
#endif
}

public class DurableApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly bool _enabled;
    private string? _originalConnectionString;
#if (UseSqlServer)
    private MsSqlContainer? _sqlServer;
#endif
#if (UsePostgres)
    private PostgreSqlContainer? _postgres;
#endif

    public bool Enabled => _enabled;

    public DurableApiFactory()
    {
        _enabled = string.Equals(Environment.GetEnvironmentVariable("RUN_DOCKER_E2E"), "1", StringComparison.OrdinalIgnoreCase);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        if (!_enabled)
        {
            return;
        }

        builder.UseEnvironment("Development");
    }

    public async Task InitializeAsync()
    {
        if (!_enabled)
        {
            return;
        }

#if (UsePostgres)
        _postgres = new PostgreSqlBuilder("postgres:latest")
            .WithDatabase("mca")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .Build();
        await _postgres.StartAsync();
#endif
#if (UseSqlServer)
        _sqlServer = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest")
            .WithPassword("YourStrong@Passw0rd")
            .WithPortBinding(0, 1433)
            .Build();
        await _sqlServer.StartAsync();
#endif

        _originalConnectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");
        Environment.SetEnvironmentVariable(
            "ConnectionStrings__DefaultConnection",
#if (UsePostgres)
            _postgres!.GetConnectionString()
#else
            new SqlConnectionStringBuilder(_sqlServer!.GetConnectionString())
            {
                InitialCatalog = "mca_tests"
            }.ConnectionString
#endif
        );
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        Environment.SetEnvironmentVariable("ConnectionStrings__DefaultConnection", _originalConnectionString);

        await base.DisposeAsync();

#if (UsePostgres)
        if (_postgres is not null)
        {
            await _postgres.DisposeAsync();
        }
#endif
#if (UseSqlServer)
        if (_sqlServer is not null)
        {
            await _sqlServer.DisposeAsync();
        }
#endif
    }
}
