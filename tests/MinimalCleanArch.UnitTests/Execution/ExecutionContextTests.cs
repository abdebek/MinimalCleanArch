using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MinimalCleanArch.Execution;
using MinimalCleanArch.Extensions.Extensions;
using MinimalCleanArch.Messaging.Extensions;

namespace MinimalCleanArch.UnitTests.Execution;

public class ExecutionContextTests
{
    [Fact]
    public void HttpExecutionContext_PrefersNameOverEmail()
    {
        var provider = BuildServiceProvider();
        var accessor = provider.GetRequiredService<IHttpContextAccessor>();
        accessor.HttpContext = CreateHttpContext(
            new Claim(ClaimTypes.Name, "Bob Smith"),
            new Claim(ClaimTypes.Email, "bob@example.com"));

        var executionContext = provider.GetRequiredService<IExecutionContext>();

        executionContext.UserName.Should().Be("Bob Smith");
    }

    [Fact]
    public void HttpExecutionContext_UsesConfiguredTenantClaimType()
    {
        var provider = BuildServiceProvider(options =>
        {
            options.TenantIdClaimTypes.Clear();
            options.TenantIdClaimTypes.Add("business_id");
        });

        var accessor = provider.GetRequiredService<IHttpContextAccessor>();
        accessor.HttpContext = CreateHttpContext(new Claim("business_id", "tenant-42"));

        var executionContext = provider.GetRequiredService<IExecutionContext>();

        executionContext.TenantId.Should().Be("tenant-42");
    }

    [Fact]
    public void HttpExecutionContext_Metadata_IsCachedPerScope()
    {
        var provider = BuildServiceProvider();
        var accessor = provider.GetRequiredService<IHttpContextAccessor>();
        accessor.HttpContext = CreateHttpContext();

        var executionContext = provider.GetRequiredService<IExecutionContext>();

        executionContext.Metadata.Should().BeSameAs(executionContext.Metadata);
    }

    [Fact]
    public void MessagingRegistration_ReplacesDefaultExecutionContextRegistration()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Services.AddMinimalCleanArchExtensions();

        builder.AddMinimalCleanArchMessaging();

        var executionContextRegistrations = builder.Services
            .Where(x => x.ServiceType == typeof(IExecutionContext))
            .ToList();

        executionContextRegistrations.Should().ContainSingle();
        executionContextRegistrations[0].ImplementationType.Should().NotBeNull();
        executionContextRegistrations[0].ImplementationType!.Name.Should().Be("MessagingExecutionContext");
    }

    [Fact]
    public void MessagingExecutionContext_UsesHttpFallbackWithConfiguredClaims()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Services.Configure<ExecutionContextOptions>(options =>
        {
            options.TenantIdClaimTypes.Clear();
            options.TenantIdClaimTypes.Add("business_id");
        });

        builder.AddMinimalCleanArchMessaging();

        using var host = builder.Build();
        using var scope = host.Services.CreateScope();
        var accessor = scope.ServiceProvider.GetRequiredService<IHttpContextAccessor>();
        accessor.HttpContext = CreateHttpContext(
            new Claim(ClaimTypes.Name, "Bob Smith"),
            new Claim(ClaimTypes.Email, "bob@example.com"),
            new Claim("business_id", "tenant-42"));

        var executionContext = scope.ServiceProvider.GetRequiredService<IExecutionContext>();

        executionContext.UserName.Should().Be("Bob Smith");
        executionContext.TenantId.Should().Be("tenant-42");
    }

    private static ServiceProvider BuildServiceProvider(Action<ExecutionContextOptions>? configure = null)
    {
        var services = new ServiceCollection();
        services.AddMinimalCleanArchExtensions();

        if (configure is not null)
        {
            services.Configure(configure);
        }

        return services.BuildServiceProvider();
    }

    private static HttpContext CreateHttpContext(params Claim[] claims)
    {
        var context = new DefaultHttpContext();
        var identity = new ClaimsIdentity(claims, authenticationType: "Test");
        context.User = new ClaimsPrincipal(identity);
        context.Request.Path = "/todos";
        context.Request.Method = HttpMethods.Get;
        return context;
    }
}
