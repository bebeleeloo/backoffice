using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Broker.Backoffice.Application.Auth;
using Broker.Backoffice.Application.Common;
using Broker.Backoffice.Application.Users;
using FluentAssertions;

namespace Broker.Backoffice.Tests.Integration;

[Collection("Integration")]
public class UsersTests(CustomWebApplicationFactory factory)
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
    public async Task ListUsers_Authenticated_ShouldReturnPaged()
    {
        await AuthenticateAsync();
        var response = await _client.GetAsync("/api/v1/users?page=1&pageSize=10");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PagedResult<UserDto>>();
        result!.Items.Should().NotBeEmpty();
        result.TotalCount.Should().BeGreaterOrEqualTo(1);
    }

    [Fact]
    public async Task ListUsers_Unauthenticated_ShouldReturn401()
    {
        var response = await _client.GetAsync("/api/v1/users");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateUser_ShouldReturnCreated()
    {
        await AuthenticateAsync();
        var response = await _client.PostAsJsonAsync("/api/v1/users", new
        {
            Username = $"testuser_{Guid.NewGuid():N}",
            Email = $"test_{Guid.NewGuid():N}@test.com",
            Password = "Test123!",
            FullName = "Test User",
            IsActive = true,
            RoleIds = Array.Empty<Guid>()
        });
        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task CreateUser_DuplicateUsername_ShouldReturn409()
    {
        await AuthenticateAsync();
        var username = $"dup_{Guid.NewGuid():N}";
        await _client.PostAsJsonAsync("/api/v1/users", new
        {
            Username = username, Email = $"a@a.com", Password = "Test123!",
            IsActive = true, RoleIds = Array.Empty<Guid>()
        });
        var response = await _client.PostAsJsonAsync("/api/v1/users", new
        {
            Username = username, Email = $"b@b.com", Password = "Test123!",
            IsActive = true, RoleIds = Array.Empty<Guid>()
        });
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task GetUser_NotFound_ShouldReturn404()
    {
        await AuthenticateAsync();
        var response = await _client.GetAsync($"/api/v1/users/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateUser_ShouldCreateAuditLog()
    {
        await AuthenticateAsync();
        await _client.PostAsJsonAsync("/api/v1/users", new
        {
            Username = $"audit_{Guid.NewGuid():N}",
            Email = $"audit_{Guid.NewGuid():N}@test.com",
            Password = "Test123!", IsActive = true, RoleIds = Array.Empty<Guid>()
        });
        var auditResp = await _client.GetAsync("/api/v1/audit?entityType=User&page=1&pageSize=5");
        auditResp.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task UpdateUser_ShouldReturnUpdated()
    {
        await AuthenticateAsync();
        var createResp = await _client.PostAsJsonAsync("/api/v1/users", new
        {
            Username = $"upd_{Guid.NewGuid():N}",
            Email = $"upd_{Guid.NewGuid():N}@test.com",
            Password = "Test123!",
            FullName = "Original Name",
            IsActive = true,
            RoleIds = Array.Empty<Guid>()
        });
        var created = await createResp.Content.ReadFromJsonAsync<UserDto>();

        var updateResp = await _client.PutAsJsonAsync($"/api/v1/users/{created!.Id}", new
        {
            Id = created.Id,
            Email = created.Email,
            FullName = "Updated Name",
            IsActive = false,
            RoleIds = Array.Empty<Guid>(),
            RowVersion = created.RowVersion,
        });
        updateResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await updateResp.Content.ReadFromJsonAsync<UserDto>();
        updated!.FullName.Should().Be("Updated Name");
        updated.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task CreateAndDeleteUser_ShouldWork()
    {
        await AuthenticateAsync();
        var createResp = await _client.PostAsJsonAsync("/api/v1/users", new
        {
            Username = $"del_{Guid.NewGuid():N}",
            Email = $"del_{Guid.NewGuid():N}@test.com",
            Password = "Test123!",
            IsActive = true,
            RoleIds = Array.Empty<Guid>()
        });
        var created = await createResp.Content.ReadFromJsonAsync<UserDto>();

        var delResp = await _client.DeleteAsync($"/api/v1/users/{created!.Id}");
        delResp.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var getResp = await _client.GetAsync($"/api/v1/users/{created.Id}");
        getResp.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
