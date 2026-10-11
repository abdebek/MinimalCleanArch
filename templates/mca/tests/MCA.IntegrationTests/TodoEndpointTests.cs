using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using MCA.Application.DTOs;
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
    public async Task ScalarUi_LoadsInDevelopment()
    {
        var response = await _client.GetAsync("/scalar/v1");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
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

        // Multi-project returns a bare list; single-project returns a paginated object with items.
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        if (document.RootElement.ValueKind == JsonValueKind.Array)
        {
            document.RootElement.GetArrayLength().Should().Be(0);
        }
        else
        {
            document.RootElement.TryGetProperty("items", out var items)
                .Should().BeTrue("paginated list responses expose an items array");
            items.GetArrayLength().Should().Be(0);
        }
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
    public async Task CreateTodo_InvalidRequest_ReturnsValidationProblemDetails()
    {
        var request = new CreateTodoRequest(string.Empty, null, 0, null);

        var response = await _client.PostAsJsonAsync("/api/todos", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType?.MediaType.Should().BeOneOf(
            "application/problem+json",
            "application/json");

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = document.RootElement;
        root.TryGetProperty("title", out _).Should().BeTrue();
        root.TryGetProperty("status", out var status).Should().BeTrue();
        status.GetInt32().Should().Be(400);
        // ASP.NET ValidationProblem uses "errors"; some shapes use "detail" only.
        (root.TryGetProperty("errors", out _) || root.TryGetProperty("detail", out _))
            .Should().BeTrue("validation failures should include errors or detail");
    }

    [Fact
#if (UseDurableMessaging)
        (Skip = "Skipped when durable messaging is enabled (requires external infrastructure).")
#endif
    ]
    public async Task GetTodoById_Missing_ReturnsNotFoundProblemDetails()
    {
        var response = await _client.GetAsync("/api/todos/999999");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        response.Content.Headers.ContentType?.MediaType.Should().BeOneOf(
            "application/problem+json",
            "application/json");

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = document.RootElement;
        root.TryGetProperty("status", out var status).Should().BeTrue();
        status.GetInt32().Should().Be(404);
        root.TryGetProperty("title", out _).Should().BeTrue();
    }
#endif

#if (!UseAuth)
    [Fact
#if (UseDurableMessaging)
        (Skip = "Skipped when durable messaging is enabled (requires external infrastructure).")
#endif
    ]
    public async Task RestoreTodo_AfterSoftDelete_IsVisibleAgain()
    {
        await using var factory = new TestApiFactory();
        using var client = factory.CreateClient();
        var create = await client.PostAsJsonAsync(
            "/api/todos",
            new CreateTodoRequest("restore-me", null, 0, null));
        create.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await create.Content.ReadFromJsonAsync<TodoResponse>();
        created.Should().NotBeNull();

        (await client.DeleteAsync($"/api/todos/{created!.Id}")).StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await client.GetAsync($"/api/todos/{created.Id}")).StatusCode.Should().Be(HttpStatusCode.NotFound);

        var restore = await client.PostAsync($"/api/todos/{created.Id}/restore", content: null);
        restore.StatusCode.Should().Be(HttpStatusCode.OK);

        var fetched = await client.GetAsync($"/api/todos/{created.Id}");
        fetched.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await fetched.Content.ReadFromJsonAsync<TodoResponse>();
        body.Should().NotBeNull();
        body!.Title.Should().Be("restore-me");
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
        builder.ConfigureAppConfiguration((_, configurationBuilder) =>
        {
            configurationBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Database:EnsureCreated"] = "false",
                // Keep Todo tests independent of rate-limit noise when --all is used.
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

            // Ensure database is created for each test run.
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
    }
}
