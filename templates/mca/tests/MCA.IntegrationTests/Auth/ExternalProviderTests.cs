#if (UseAuth)
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace MCA.IntegrationTests.Auth;

public class UnconfiguredExternalProviderTests : IClassFixture<AuthTestApiFactory>
{
    private readonly AuthTestApiFactory _factory;

    public UnconfiguredExternalProviderTests(AuthTestApiFactory factory) => _factory = factory;

    [Fact]
    public async Task Google_Challenge_ReturnsNotFound_WhenSecretsAreEmpty()
    {
        using var client = _factory.CreateClient();
        using var response = await client.GetAsync("/api/auth/external/Google");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("not configured");
    }

    [Fact]
    public async Task Providers_List_IsEmpty_WhenNoneConfigured()
    {
        using var client = _factory.CreateClient();
        using var response = await client.GetAsync("/api/auth/external/providers");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        doc.RootElement.GetProperty("providers").GetArrayLength().Should().Be(0);
    }
}

public class ConfiguredExternalProviderTests : IClassFixture<ExternalAuthTestApiFactory>
{
    private readonly ExternalAuthTestApiFactory _factory;

    public ConfiguredExternalProviderTests(ExternalAuthTestApiFactory factory) => _factory = factory;

    [Fact]
    public async Task Google_Challenge_Redirects_To_Google_When_Configured()
    {
        using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        using var response = await client.GetAsync("/api/auth/external/Google");

        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location.Should().NotBeNull();
        response.Headers.Location!.Host.Should().ContainEquivalentOf("google");
    }

    [Fact]
    public async Task Providers_List_Contains_Configured_Google_Only()
    {
        using var client = _factory.CreateClient();
        using var response = await client.GetAsync("/api/auth/external/providers");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var names = doc.RootElement.GetProperty("providers").EnumerateArray().Select(e => e.GetString()).ToArray();
        names.Should().Contain("Google");
        names.Should().NotContain("Microsoft");
        names.Should().NotContain("GitHub");
    }

    [Fact]
    public async Task Unconfigured_Named_Provider_Returns_NotFound()
    {
        using var client = _factory.CreateClient();
        using var response = await client.GetAsync("/api/auth/external/Microsoft");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}

public class ExternalAuthTestApiFactory : AuthTestApiFactory
{
    protected override IEnumerable<KeyValuePair<string, string?>> ExtraConfiguration() =>
    [
        new("Authentication:Google:ClientId", "test-google-client-id"),
        new("Authentication:Google:ClientSecret", "test-google-client-secret")
    ];
}
#endif
