using System.Text.Json;

namespace Broker.Auth.Tests.Integration;

public class RolesTests(CustomWebApplicationFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task ListRoles_ShouldReturnPagedResult()
    {
        await AuthenticateAsync();
        var resp = await _client.GetAsync("/api/v1/roles");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task CreateRole_ShouldReturn201()
    {
        await AuthenticateAsync();
        var resp = await _client.PostAsJsonAsync("/api/v1/roles", new
        {
            Name = $"TestRole_{Guid.NewGuid():N}"[..20],
            Description = "Test role"
        });
        resp.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task GetRoleById_ReturnsRole()
    {
        await AuthenticateAsync();

        // Create a role
        var roleName = $"GetRole_{Guid.NewGuid():N}"[..20];
        var createResp = await _client.PostAsJsonAsync("/api/v1/roles", new
        {
            Name = roleName,
            Description = "Role for GetById test"
        });
        createResp.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await createResp.Content.ReadFromJsonAsync<JsonElement>();
        var roleId = created.GetProperty("id").GetString();

        // Get role by id
        var resp = await _client.GetAsync($"/api/v1/roles/{roleId}");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);

        var role = await resp.Content.ReadFromJsonAsync<JsonElement>();
        role.GetProperty("id").GetString().Should().Be(roleId);
        role.GetProperty("name").GetString().Should().Be(roleName);
        role.GetProperty("description").GetString().Should().Be("Role for GetById test");
    }

    [Fact]
    public async Task UpdateRole_WithValidData_ReturnsUpdatedRole()
    {
        await AuthenticateAsync();

        // Create a role
        var createResp = await _client.PostAsJsonAsync("/api/v1/roles", new
        {
            Name = $"UpdRole_{Guid.NewGuid():N}"[..20],
            Description = "Original description"
        });
        var created = await createResp.Content.ReadFromJsonAsync<JsonElement>();
        var roleId = created.GetProperty("id").GetString();
        var rowVersion = created.GetProperty("rowVersion").GetString();

        // Update the role
        var newName = $"Renamed_{Guid.NewGuid():N}"[..20];
        var resp = await _client.PutAsJsonAsync($"/api/v1/roles/{roleId}", new
        {
            Id = roleId,
            Name = newName,
            Description = "Updated description",
            RowVersion = rowVersion
        });
        resp.StatusCode.Should().Be(HttpStatusCode.OK);

        var updated = await resp.Content.ReadFromJsonAsync<JsonElement>();
        updated.GetProperty("name").GetString().Should().Be(newName);
        updated.GetProperty("description").GetString().Should().Be("Updated description");
    }

    [Fact]
    public async Task DeleteRole_ReturnsNoContent()
    {
        await AuthenticateAsync();

        // Create a role to delete
        var createResp = await _client.PostAsJsonAsync("/api/v1/roles", new
        {
            Name = $"DelRole_{Guid.NewGuid():N}"[..20],
            Description = "Delete me"
        });
        var created = await createResp.Content.ReadFromJsonAsync<JsonElement>();
        var roleId = created.GetProperty("id").GetString();

        // Delete the role
        var resp = await _client.DeleteAsync($"/api/v1/roles/{roleId}");
        resp.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify role is gone
        var getResp = await _client.GetAsync($"/api/v1/roles/{roleId}");
        getResp.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateRole_WithDuplicateName_Returns409()
    {
        await AuthenticateAsync();

        var roleName = $"DupRole_{Guid.NewGuid():N}"[..20];

        // Create first role
        var resp1 = await _client.PostAsJsonAsync("/api/v1/roles", new
        {
            Name = roleName,
            Description = "First role"
        });
        resp1.StatusCode.Should().Be(HttpStatusCode.Created);

        // Try to create second role with same name
        var resp2 = await _client.PostAsJsonAsync("/api/v1/roles", new
        {
            Name = roleName,
            Description = "Second role"
        });
        resp2.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task SetRolePermissions_UpdatesPermissions()
    {
        await AuthenticateAsync();

        // Get all permissions to find some IDs
        var permResp = await _client.GetAsync("/api/v1/permissions");
        var permissions = await permResp.Content.ReadFromJsonAsync<JsonElement>();
        var permArray = permissions.EnumerateArray().ToList();
        permArray.Count.Should().BeGreaterThan(0);

        // Pick the first two permission IDs
        var permId1 = permArray[0].GetProperty("id").GetString();
        var permId2 = permArray[1].GetProperty("id").GetString();

        // Create a role
        var createResp = await _client.PostAsJsonAsync("/api/v1/roles", new
        {
            Name = $"PermRole_{Guid.NewGuid():N}"[..20],
            Description = "Role for permissions test"
        });
        var created = await createResp.Content.ReadFromJsonAsync<JsonElement>();
        var roleId = created.GetProperty("id").GetString();

        // Set permissions
        var resp = await _client.PutAsJsonAsync($"/api/v1/roles/{roleId}/permissions",
            new[] { permId1, permId2 });
        resp.StatusCode.Should().Be(HttpStatusCode.OK);

        var updated = await resp.Content.ReadFromJsonAsync<JsonElement>();
        var rolePermissions = updated.GetProperty("permissions").EnumerateArray().ToList();
        rolePermissions.Count.Should().Be(2);
    }

    [Fact]
    public async Task DeleteRole_SystemRole_Returns409()
    {
        await AuthenticateAsync();

        // The "Admin" role is seeded as a system role (IsSystem = true)
        // Find the Admin role by listing roles
        var listResp = await _client.GetAsync("/api/v1/roles?pageSize=100");
        var result = await listResp.Content.ReadFromJsonAsync<JsonElement>();
        var roles = result.GetProperty("items").EnumerateArray().ToList();

        var adminRole = roles.FirstOrDefault(r =>
            r.GetProperty("name").GetString() == "Admin" &&
            r.GetProperty("isSystem").GetBoolean());
        adminRole.ValueKind.Should().NotBe(JsonValueKind.Undefined, "Admin system role should exist in seed data");

        var adminRoleId = adminRole.GetProperty("id").GetString();

        // Try to delete the system role — should be rejected
        var resp = await _client.DeleteAsync($"/api/v1/roles/{adminRoleId}");
        resp.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }
}
