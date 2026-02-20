using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Broker.Backoffice.Application.Auth;
using Broker.Backoffice.Application.Common;
using Broker.Backoffice.Application.Roles;
using FluentAssertions;

namespace Broker.Backoffice.Tests.Integration;

public class RolesTests(CustomWebApplicationFactory factory)
    : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    private async Task AuthenticateAsync()
    {
        var loginResp = await _client.PostAsJsonAsync("/api/v1/auth/login",
            new { Username = "admin", Password = "Admin123!" });
        var auth = await loginResp.Content.ReadFromJsonAsync<AuthResponse>();
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", auth!.AccessToken);
    }

    [Fact]
    public async Task ListRoles_ShouldReturnPaged()
    {
        await AuthenticateAsync();
        var response = await _client.GetAsync("/api/v1/roles?page=1&pageSize=10");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PagedResult<RoleDto>>();
        result!.Items.Should().NotBeEmpty();
    }

    [Fact]
    public async Task CreateAndDeleteRole_ShouldWork()
    {
        await AuthenticateAsync();
        var createResp = await _client.PostAsJsonAsync("/api/v1/roles",
            new { Name = $"role_{Guid.NewGuid():N}", Description = "test role" });
        createResp.StatusCode.Should().Be(HttpStatusCode.Created);
        var role = await createResp.Content.ReadFromJsonAsync<RoleDto>();

        var delResp = await _client.DeleteAsync($"/api/v1/roles/{role!.Id}");
        delResp.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task DeleteSystemRole_ShouldReturn409()
    {
        await AuthenticateAsync();
        var listResp = await _client.GetAsync("/api/v1/roles?page=1&pageSize=50");
        var roles = await listResp.Content.ReadFromJsonAsync<PagedResult<RoleDto>>();
        var adminRole = roles!.Items.First(r => r.IsSystem);

        var response = await _client.DeleteAsync($"/api/v1/roles/{adminRole.Id}");
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }
}
