using System.Net;
using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MinimalCleanArch.Extensions.Features;
using MinimalCleanArch.Features;

namespace MinimalCleanArch.UnitTests.Features;

public class FeatureGatePortTests
{
    [Fact]
    public void PortTypes_AreNotAspNet()
    {
        var portAssembly = typeof(IFeatureGate).Assembly;
        portAssembly.GetReferencedAssemblies()
            .Select(a => a.Name)
            .Should()
            .NotContain(n => n != null && (
                n.Contains("AspNetCore", StringComparison.OrdinalIgnoreCase)
                || n.Contains("EntityFramework", StringComparison.OrdinalIgnoreCase)));

        typeof(IFeatureGate).GetMethods()
            .SelectMany(m => m.GetParameters())
            .Select(p => p.ParameterType.Namespace ?? string.Empty)
            .Should()
            .OnlyContain(ns => !ns.StartsWith("Microsoft.AspNetCore", StringComparison.Ordinal));
    }

    [Fact]
    public void MissingFlag_IsDisabled()
    {
        var gate = CreateGate();
        gate.IsEnabled("missing").Should().BeFalse();
    }

    [Fact]
    public void ConfigFlag_IsEnabled()
    {
        var gate = CreateGate(("Features:Flags:todo-export", "true"));
        gate.IsEnabled("todo-export").Should().BeTrue();
        gate.IsEnabled("TODO-EXPORT").Should().BeTrue();
    }

    [Fact]
    public void TenantOverride_WinsOverGlobal()
    {
        var gate = CreateGate(
            ("Features:Flags:todo-export", "false"),
            ("Features:Tenants:acme:todo-export", "true"));
        gate.IsEnabled("todo-export").Should().BeFalse();
        gate.IsEnabled("todo-export", "acme").Should().BeTrue();
    }

    [Fact]
    public void StoreOverride_WinsOverConfig()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Features:Flags:todo-export"] = "false"
            })
            .Build();
        var services = new ServiceCollection();
        services.AddFeatures(configuration);
        services.AddSingleton<IFeatureStore>(new StubStore(true));
        using var provider = services.BuildServiceProvider();
        provider.GetRequiredService<IFeatureGate>().IsEnabled("todo-export").Should().BeTrue();
    }

    private static IFeatureGate CreateGate(params (string Key, string Value)[] pairs)
    {
        var values = pairs.ToDictionary(p => p.Key, p => (string?)p.Value);
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(values).Build();
        var services = new ServiceCollection();
        services.AddFeatures(configuration);
        return services.BuildServiceProvider().GetRequiredService<IFeatureGate>();
    }

    private sealed class StubStore(bool enabled) : IFeatureStore
    {
        public bool? GetOverride(string feature, string? tenantId) => enabled;
    }
}

public class FeatureEndpointFilterTests
{
    [Fact]
    public async Task RequireFeature_FlipInConfig_ChangesAuthorization()
    {
        (await HitExportAsync(enabled: false)).Should().Be(HttpStatusCode.Forbidden);
        (await HitExportAsync(enabled: true)).Should().Be(HttpStatusCode.OK);
    }

    private static async Task<HttpStatusCode> HitExportAsync(bool enabled)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Features:Flags:todo-export"] = enabled ? "true" : "false"
            })
            .Build();
        builder.Services.AddFeatures(configuration);
        await using var app = builder.Build();
        app.MapGet("/api/todos/export", () => Results.Ok(Array.Empty<object>()))
            .RequireFeature("todo-export");
        await app.StartAsync();

        using var client = new HttpClient { BaseAddress = new Uri(app.Urls.Single() + "/") };
        using var response = await client.GetAsync("api/todos/export");
        return response.StatusCode;
    }
}
