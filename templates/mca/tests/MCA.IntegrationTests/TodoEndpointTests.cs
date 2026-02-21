using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using MCA.Application.DTOs;
using MCA.Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace MCA.IntegrationTests;

public class TodoEndpointTests : IClassFixture<TestApiFactory>
{
    private readonly HttpClient _client;

    public TodoEndpointTests(TestApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact
#if (UseDurableMessaging)
        (Skip = "Skipped when durable messaging is enabled (requires external infrastructure).")
#endif
    ]
    public async Task GetTodos_InitiallyEmpty_ReturnsOkAndEmptyList()
    {
        // Use a dedicated factory with a fresh DB to guarantee empty state regardless
        // of which order xUnit executes tests within the class.
        await using var isolatedFactory = new TestApiFactory();
        using var isolatedClient = isolatedFactory.CreateClient();

        var response = await isolatedClient.GetAsync("/api/todos");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<List<TodoResponse>>();
        payload.Should().NotBeNull();
        payload!.Should().BeEmpty();
    }

    [Fact
#if (UseDurableMessaging)
        (Skip = "Skipped when durable messaging is enabled (requires external infrastructure).")
#endif
    ]
    public async Task CreateTodo_ValidRequest_ReturnsCreatedAndPersists()
    {
        var request = new CreateTodoRequest("sample", "desc", 1, DateTime.UtcNow.AddDays(1));

        var createResponse = await _client.PostAsJsonAsync("/api/todos", request);

        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await createResponse.Content.ReadFromJsonAsync<TodoResponse>();
        created.Should().NotBeNull();
        created!.Title.Should().Be("sample");

        var getResponse = await _client.GetAsync($"/api/todos/{created.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var fetched = await getResponse.Content.ReadFromJsonAsync<TodoResponse>();
        fetched.Should().NotBeNull();
        fetched!.Id.Should().Be(created.Id);
    }

#if (UseValidation)
    [Fact
#if (UseDurableMessaging)
        (Skip = "Skipped when durable messaging is enabled (requires external infrastructure).")
#endif
    ]
    public async Task CreateTodo_InvalidRequest_ReturnsBadRequest()
    {
        var request = new CreateTodoRequest(string.Empty, null, 0, null);

        var response = await _client.PostAsJsonAsync("/api/todos", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
#endif
}

public class TestApiFactory : WebApplicationFactory<Program>
{
    // Each factory instance gets its own DB so independently-created factories
    // (e.g. inline factories in a test) never share state with the class-level fixture.
    private readonly string _dbName = $"TestDb-{Guid.NewGuid()}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
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

            // Ensure database is created for each test run
            using var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Database.EnsureCreated();
        });
    }
}
