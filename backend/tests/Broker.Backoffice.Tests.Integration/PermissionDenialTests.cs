using System.Net;
using System.Net.Http.Json;
using Broker.Backoffice.Application.Permissions;
using Broker.Backoffice.Application.Roles;
using Broker.Backoffice.Application.Users;
using FluentAssertions;

namespace Broker.Backoffice.Tests.Integration;

public class PermissionDenialTests(CustomWebApplicationFactory factory) : IntegrationTestBase(factory)
{
    private async Task<string> CreateLimitedUserAsync(params string[] permissionCodes)
    {
        await AuthenticateAsync();

        // Get all permissions
        var permResp = await _client.GetAsync("/api/v1/permissions");
        var permissions = await permResp.Content.ReadFromJsonAsync<List<PermissionDto>>();

        // Create role with only the specified permissions
        var roleName = $"limited_{Guid.NewGuid():N}"[..20];
        var createRoleResp = await _client.PostAsJsonAsync("/api/v1/roles", new
        {
            Name = roleName,
            Description = "Limited test role",
        });
        var role = await createRoleResp.Content.ReadFromJsonAsync<RoleDto>();

        var permIds = permissions!
            .Where(p => permissionCodes.Contains(p.Code))
            .Select(p => p.Id)
            .ToList();

        await _client.PutAsJsonAsync($"/api/v1/roles/{role!.Id}/permissions", permIds);

        // Create user with this role
        var username = $"limited_{Guid.NewGuid():N}";
        var password = "Limited123!";
        await _client.PostAsJsonAsync("/api/v1/users", new
        {
            Username = username,
            Email = $"{username}@test.com",
            Password = password,
            IsActive = true,
            RoleIds = new[] { role.Id },
        });

        // Clear admin auth and login as limited user
        _client.DefaultRequestHeaders.Authorization = null;
        await AuthenticateAsAsync(username, password);

        return username;
    }

    [Fact]
    public async Task UserWithClientsRead_CanListClients()
    {
        await CreateLimitedUserAsync("clients.read");
        var response = await _client.GetAsync("/api/v1/clients?page=1&pageSize=10");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task UserWithClientsRead_CannotCreateClient()
    {
        await CreateLimitedUserAsync("clients.read");
        var response = await _client.PostAsJsonAsync("/api/v1/clients", new
        {
            ClientType = "Individual",
            Status = "Active",
            Email = $"denied_{Guid.NewGuid():N}@test.com",
            PepStatus = false,
            KycStatus = "NotStarted",
            Addresses = new[] { new { Type = "Legal", Line1 = "1 St", City = "City", CountryId = Guid.NewGuid() } },
        });
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task UserWithClientsRead_CannotListUsers()
    {
        await CreateLimitedUserAsync("clients.read");
        var response = await _client.GetAsync("/api/v1/users?page=1&pageSize=10");
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task UserWithClientsRead_CanAccessDashboard()
    {
        await CreateLimitedUserAsync("clients.read");
        var response = await _client.GetAsync("/api/v1/dashboard/stats");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task UserWithNoPermissions_CannotListAnything()
    {
        await AuthenticateAsync();

        // Create role with no permissions
        var roleName = $"noperm_{Guid.NewGuid():N}"[..20];
        var createRoleResp = await _client.PostAsJsonAsync("/api/v1/roles", new
        {
            Name = roleName,
            Description = "No permissions",
        });
        var role = await createRoleResp.Content.ReadFromJsonAsync<RoleDto>();

        // Create user
        var username = $"noperm_{Guid.NewGuid():N}";
        await _client.PostAsJsonAsync("/api/v1/users", new
        {
            Username = username,
            Email = $"{username}@test.com",
            Password = "NoPerms123!",
            IsActive = true,
            RoleIds = new[] { role!.Id },
        });

        _client.DefaultRequestHeaders.Authorization = null;
        await AuthenticateAsAsync(username, "NoPerms123!");

        var clientsResp = await _client.GetAsync("/api/v1/clients?page=1&pageSize=10");
        clientsResp.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var accountsResp = await _client.GetAsync("/api/v1/accounts?page=1&pageSize=10");
        accountsResp.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var instrumentsResp = await _client.GetAsync("/api/v1/instruments?page=1&pageSize=10");
        instrumentsResp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    private record PermissionDto(Guid Id, string Code, string Name, string? Description, string Group);
}
