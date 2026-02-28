using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Broker.Backoffice.Application.Auth;
using Broker.Backoffice.Application.Common;
using Broker.Backoffice.Application.Roles;
using FluentAssertions;

namespace Broker.Backoffice.Tests.Integration;

[Collection("Integration")]
public class RolesTests(CustomWebApplicationFactory factory)
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

    [Fact]
    public async Task GetById_ShouldReturn200()
    {
        await AuthenticateAsync();
        var createResp = await _client.PostAsJsonAsync("/api/v1/roles",
            new { Name = $"get_{Guid.NewGuid():N}", Description = "test" });
        var created = await createResp.Content.ReadFromJsonAsync<RoleDto>();

        var response = await _client.GetAsync($"/api/v1/roles/{created!.Id}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var role = await response.Content.ReadFromJsonAsync<RoleDto>();
        role!.Id.Should().Be(created.Id);
        role.Name.Should().Be(created.Name);
    }

    [Fact]
    public async Task UpdateRole_ShouldReturnUpdated()
    {
        await AuthenticateAsync();
        var createResp = await _client.PostAsJsonAsync("/api/v1/roles",
            new { Name = $"upd_{Guid.NewGuid():N}", Description = "original" });
        var created = await createResp.Content.ReadFromJsonAsync<RoleDto>();

        var updateResp = await _client.PutAsJsonAsync($"/api/v1/roles/{created!.Id}", new
        {
            Id = created.Id,
            Name = $"upd2_{Guid.NewGuid():N}",
            Description = "updated",
            RowVersion = created.RowVersion,
        });
        updateResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await updateResp.Content.ReadFromJsonAsync<RoleDto>();
        updated!.Description.Should().Be("updated");
    }

    [Fact]
    public async Task SetPermissions_ShouldWork()
    {
        await AuthenticateAsync();

        // Get available permissions
        var permResp = await _client.GetAsync("/api/v1/permissions");
        var permissions = await permResp.Content.ReadFromJsonAsync<List<PermissionIdDto>>();
        var permId = permissions!.First().Id;

        // Create role
        var createResp = await _client.PostAsJsonAsync("/api/v1/roles",
            new { Name = $"perm_{Guid.NewGuid():N}", Description = "test" });
        var created = await createResp.Content.ReadFromJsonAsync<RoleDto>();
        created!.Permissions.Should().BeEmpty();

        // Set permissions
        var setResp = await _client.PutAsJsonAsync($"/api/v1/roles/{created.Id}/permissions",
            new List<Guid> { permId });
        setResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await setResp.Content.ReadFromJsonAsync<RoleDto>();
        updated!.Permissions.Should().NotBeEmpty();
    }

    // Lightweight DTO for permissions list
    private record PermissionIdDto(Guid Id, string Code, string Name, string? Description, string Group);
}
