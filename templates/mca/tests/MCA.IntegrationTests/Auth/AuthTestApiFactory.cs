#if (UseAuth)
using System.Collections.Concurrent;
using System.Diagnostics;
using FluentAssertions;
using MCA.Domain.Constants;
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

namespace MCA.IntegrationTests;

// Lives in Auth/ so it is still emitted when AuthEndpointTests.cs is excluded
// under --ratelimiting / --all / durable messaging.
public class AuthTestApiFactory : WebApplicationFactory<Program>
{
    protected virtual IEnumerable<KeyValuePair<string, string?>> ExtraConfiguration() => [];

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((_, configBuilder) =>
        {
            var values = new Dictionary<string, string?>
            {
                // Intentionally mismatch App:BaseUrl with runtime URLs to verify redirect URI seeding
                // remains resilient in development when launch profile ports differ.
                ["App:BaseUrl"] = "https://localhost:7443",
                ["ASPNETCORE_URLS"] = "http://localhost",
                ["Database:EnsureCreated"] = "false",
                ["Seed:EnableBootstrapAdmin"] = "true",
                ["Seed:AdminEmail"] = "admin@example.com",
                ["Seed:AdminPassword"] = "SeededAdmin!123",
                ["Seed:AdminFirstName"] = "System",
                ["Seed:AdminLastName"] = "Administrator",
                ["Seed:AdminRole"] = Roles.Admin,
                // Auth suite issues many requests; relax limiters when --all/--ratelimiting is on.
                ["RateLimiting:EnableGlobalLimiter"] = "false",
                ["RateLimiting:FixedPermitLimit"] = "10000",
                ["RateLimiting:SlidingPermitLimit"] = "10000",
                ["RateLimiting:TokenBucketLimit"] = "10000",
                ["RateLimiting:TokensPerPeriod"] = "10000",
                ["RateLimiting:ConcurrencyPermitLimit"] = "10000"
            };
            foreach (var pair in ExtraConfiguration())
            {
                values[pair.Key] = pair.Value;
            }

            configBuilder.AddInMemoryCollection(values);
        });
        builder.ConfigureServices(services =>
        {
            services.RemoveAll(typeof(DbContextOptions<AppDbContext>));
            services.RemoveAll(typeof(IDbContextOptionsConfiguration<AppDbContext>));
            services.RemoveAll<AppDbContext>();

            // Use a fresh in-memory DB per factory instance so test classes don't share state
            var dbName = $"AuthTestDb-{Guid.NewGuid()}";
            services.AddDbContext<AppDbContext>((sp, options) =>
            {
                options.UseInMemoryDatabase(dbName);
                options.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
                options.UseOpenIddict<Guid>();
#if (UseMessaging)
                options.UseDomainEventPublishing(sp);
#endif
            });

            // Wolverine registers IAsyncDisposable-only services — must not Dispose() the temp provider.
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

            // Replace SMTP sender with an in-memory capture sender for auth flow assertions
            services.RemoveAll<IEmailSender>();
            services.AddSingleton<RecordingEmailSender>();
            services.AddSingleton<IEmailSender>(sp => sp.GetRequiredService<RecordingEmailSender>());

            // Route internally-created HttpClient calls (e.g. OAuth demo callback token exchange)
            // back into the in-memory TestServer instead of localhost network sockets.
            services.RemoveAll<IHttpClientFactory>();
            services.AddSingleton<IHttpClientFactory>(_ =>
                new InProcessHttpClientFactory(() =>
                    CreateClient(new WebApplicationFactoryClientOptions
                    {
                        AllowAutoRedirect = false,
                        HandleCookies = true
                    })));
        });
    }
}

internal sealed class RecordingEmailSender : IEmailSender
{
    private readonly ConcurrentQueue<EmailMessage> _messages = new();

    public Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        _messages.Enqueue(new EmailMessage
        {
            To = message.To,
            Subject = message.Subject,
            HtmlBody = message.HtmlBody,
            TextBody = message.TextBody
        });

        return Task.CompletedTask;
    }

    public void Clear()
    {
        while (_messages.TryDequeue(out _))
        {
        }
    }

    public EmailMessage GetLatestForRecipient(string email, string? subjectContains = null)
    {
        var timeout = TimeSpan.FromSeconds(5);
        var startedAt = Stopwatch.StartNew();
        EmailMessage? match = null;

        while (startedAt.Elapsed < timeout)
        {
            match = _messages.Reverse().FirstOrDefault(message =>
                string.Equals(message.To, email, StringComparison.OrdinalIgnoreCase)
                && (string.IsNullOrWhiteSpace(subjectContains)
                    || message.Subject.Contains(subjectContains, StringComparison.OrdinalIgnoreCase)));

            if (match is not null)
            {
                break;
            }

            Thread.Sleep(50);
        }

        match.Should().NotBeNull($"Expected an email for {email}.");
        return match!;
    }
}

internal sealed class InProcessHttpClientFactory : IHttpClientFactory
{
    private readonly Func<HttpClient> _clientFactory;

    public InProcessHttpClientFactory(Func<HttpClient> clientFactory)
    {
        _clientFactory = clientFactory;
    }

    public HttpClient CreateClient(string name) => _clientFactory();
}
#endif
