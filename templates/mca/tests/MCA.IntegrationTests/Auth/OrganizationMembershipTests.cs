#if (UseAuth && UseMultiTenant)
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using MCA.Application.DTOs;
using Xunit;

namespace MCA.IntegrationTests.Auth;

public class OrganizationMembershipTests : IClassFixture<TenantIsolationApiFactory>
{
    private readonly TenantIsolationApiFactory _factory;

    public OrganizationMembershipTests(TenantIsolationApiFactory factory) => _factory = factory;

    [Fact]
    public async Task CreateOrg_Invite_AndJoin_SharesTodosWithInvitee()
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

        await CreateTodoAsync(client, tokenA, "org-secret");
        (await ListTodoTitlesAsync(client, tokenB)).Should().NotContain("org-secret");

        using var createOrg = Authenticated(HttpMethod.Post, "/api/organizations", tokenA);
        createOrg.Content = JsonContent.Create(new { name = "Acme" });
        using var createOrgResponse = await client.SendAsync(createOrg);
        createOrgResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var org = await createOrgResponse.Content.ReadFromJsonAsync<OrganizationResponse>();
        org.Should().NotBeNull();
        org!.Name.Should().Be("Acme");
        org.Role.Should().Be("Owner");

        using var rolesRequest = Authenticated(HttpMethod.Get, "/api/organizations/current/roles", tokenA);
        using var rolesResponse = await client.SendAsync(rolesRequest);
        rolesResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var roles = await rolesResponse.Content.ReadFromJsonAsync<List<OrganizationRoleResponse>>();
        roles.Should().NotBeNull();
        roles!.Select(r => r.Name).Should().BeEquivalentTo("Owner", "Admin", "Member");

        using var invite = Authenticated(HttpMethod.Post, "/api/organizations/current/invitations", tokenA);
        invite.Content = JsonContent.Create(new { role = "Member" });
        using var inviteResponse = await client.SendAsync(invite);
        inviteResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var invitation = await inviteResponse.Content.ReadFromJsonAsync<OrganizationInvitationResponse>();
        invitation.Should().NotBeNull();
        invitation!.Code.Should().NotBeNullOrWhiteSpace();

        using var join = Authenticated(HttpMethod.Post, "/api/organizations/join", tokenB);
        join.Content = JsonContent.Create(new { code = invitation.Code });
        using var joinResponse = await client.SendAsync(join);
        joinResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var tokenBJoined = await GetAccessTokenAsync(client, emailB);
        (await ListTodoTitlesAsync(client, tokenBJoined)).Should().Contain("org-secret");

        using var membersRequest = Authenticated(HttpMethod.Get, "/api/organizations/current/members", tokenA);
        using var membersResponse = await client.SendAsync(membersRequest);
        membersResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var members = await membersResponse.Content.ReadFromJsonAsync<List<OrganizationMemberResponse>>();
        members.Should().NotBeNull();
        members!.Should().HaveCount(2);
        members.Select(m => m.Role).Should().BeEquivalentTo("Owner", "Member");
    }

    private static HttpRequestMessage Authenticated(HttpMethod method, string url, string token)
    {
        var request = new HttpRequestMessage(method, url);
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        return request;
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

    private static async Task CreateTodoAsync(HttpClient client, string token, string title)
    {
        using var request = Authenticated(HttpMethod.Post, "/api/todos", token);
        request.Content = JsonContent.Create(new { title });
        using var response = await client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    private static async Task<List<string>> ListTodoTitlesAsync(HttpClient client, string token)
    {
        using var request = Authenticated(HttpMethod.Get, "/api/todos", token);
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

    private static string UniqueEmail() => $"org{Guid.NewGuid():N}@example.com";
}
#endif
