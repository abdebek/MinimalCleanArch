#if (UseAuth && UseMultiTenant)
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using MCA.Application.DTOs;
using MCA.Infrastructure.Data;
#if (SingleProject)
using MCA.Infrastructure.Configuration;
#else
using MCA.Api.Configuration;
#endif
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MinimalCleanArch.Email;
#if (UseMessaging)
using MinimalCleanArch.Messaging.Extensions;
#endif
using Xunit;

namespace MCA.IntegrationTests.Auth;

public class TenantIsolationTests : IClassFixture<TenantIsolationApiFactory>
{
    private readonly TenantIsolationApiFactory _factory;

    public TenantIsolationTests(TenantIsolationApiFactory factory) => _factory = factory;

    [Fact]
    public async Task TenantA_CannotRead_TenantB_Todos()
    {
        using var client = _factory.CreateClient();

        var emailA = UniqueEmail();
        var emailB = UniqueEmail();
        (await client.PostAsJsonAsync("/api/auth/register", new { email = emailA, password = "Test@1234" }))
            .StatusCode.Should().Be(HttpStatusCode.OK);
        (await client.PostAsJsonAsync("/api/auth/register", new { email = emailB, password = "Test@1234" }))
            .StatusCode.Should().Be(HttpStatusCode.OK);

        var tokenA = await GetAccessTokenAsync(client, emailA);
        var tokenB = await GetAccessTokenAsync(client, emailB);

        var createdA = await CreateTodoAsync(client, tokenA, "secret-A");
        var createdB = await CreateTodoAsync(client, tokenB, "secret-B");

        var titlesA = await ListTodoTitlesAsync(client, tokenA);
        titlesA.Should().Equal("secret-A");

        var titlesB = await ListTodoTitlesAsync(client, tokenB);
        titlesB.Should().Equal("secret-B");

        using var getCross = new HttpRequestMessage(HttpMethod.Get, $"/api/todos/{createdB.Id}");
        getCross.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", tokenA);
        using var crossResponse = await client.SendAsync(getCross);
        crossResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);

        createdA.Title.Should().Be("secret-A");
    }

    [Fact]
    public async Task Unauthenticated_TodoList_Returns401()
    {
        using var client = _factory.CreateClient();
        using var response = await client.GetAsync("/api/todos");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private static async Task<string> GetAccessTokenAsync(HttpClient client, string email)
    {
        using var tokenResponse = await client.PostAsync("/connect/token", new FormUrlEncodedContent(
            new Dictionary<string, string>
            {
                ["grant_type"] = "password",
                ["username"] = email,
                ["password"] = "Test@1234",
                ["client_id"] = "mca-web-client",
                ["client_secret"] = "mca-default-secret-change-me",
                ["scope"] = "openid profile email"
            }));
        tokenResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        using var tokenJson = JsonDocument.Parse(await tokenResponse.Content.ReadAsStringAsync());
        var accessToken = tokenJson.RootElement.GetProperty("access_token").GetString();
        accessToken.Should().NotBeNullOrWhiteSpace();
        return accessToken!;
    }

    private static async Task<TodoResponse> CreateTodoAsync(HttpClient client, string token, string title)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/todos");
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        request.Content = JsonContent.Create(new { title });
        using var response = await client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<TodoResponse>();
        body.Should().NotBeNull();
        body!.Title.Should().Be(title);
        return body;
    }

    private static async Task<List<string>> ListTodoTitlesAsync(HttpClient client, string token)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/todos");
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        using var response = await client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = document.RootElement;
        var items = root.ValueKind == JsonValueKind.Array
            ? root
            : root.GetProperty("items");
        return items.EnumerateArray()
            .Select(e => e.GetProperty("title").GetString() ?? string.Empty)
            .ToList();
    }

    private static string UniqueEmail() => $"tenant{Guid.NewGuid():N}@example.com";
}

public class TenantIsolationApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((_, configBuilder) =>
        {
            configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["App:BaseUrl"] = "https://localhost:7443",
                ["ASPNETCORE_URLS"] = "http://localhost",
                ["Database:EnsureCreated"] = "false",
                ["Seed:EnableBootstrapAdmin"] = "false",
                ["RateLimiting:EnableGlobalLimiter"] = "false",
                ["RateLimiting:FixedPermitLimit"] = "10000",
                ["RateLimiting:SlidingPermitLimit"] = "10000",
                ["RateLimiting:TokenBucketLimit"] = "10000",
                ["RateLimiting:TokensPerPeriod"] = "10000",
                ["RateLimiting:ConcurrencyPermitLimit"] = "10000"
            });
        });
        builder.ConfigureServices(services =>
        {
            services.RemoveAll(typeof(DbContextOptions<AppDbContext>));
            services.RemoveAll(typeof(IDbContextOptionsConfiguration<AppDbContext>));
            services.RemoveAll<AppDbContext>();

            var dbName = $"TenantIsolationDb-{Guid.NewGuid()}";
            services.AddDbContext<AppDbContext>((sp, options) =>
            {
                options.UseInMemoryDatabase(dbName);
                options.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
                options.UseOpenIddict<Guid>();
#if (UseMessaging)
                options.UseDomainEventPublishing(sp);
#endif
            });

            var bootstrapProvider = services.BuildServiceProvider();
            try
            {
                using var bootstrapScope = bootstrapProvider.CreateScope();
                var db = bootstrapScope.ServiceProvider.GetRequiredService<AppDbContext>();
                db.Database.EnsureCreated();
                var configuration = bootstrapScope.ServiceProvider.GetRequiredService<IConfiguration>();
                bootstrapScope.ServiceProvider
                    .SeedOpenIddictApplicationsAsync(configuration)
                    .GetAwaiter()
                    .GetResult();
            }
            finally
            {
                if (bootstrapProvider is IAsyncDisposable asyncDisposable)
                    asyncDisposable.DisposeAsync().AsTask().GetAwaiter().GetResult();
                else
                    bootstrapProvider.Dispose();
            }

            services.RemoveAll<IEmailSender>();
            services.AddSingleton<IEmailSender, NoOpEmailSender>();
        });
    }

    private sealed class NoOpEmailSender : IEmailSender
    {
        public Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }
}
#endif
