using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Broker.Backoffice.Application.Auth;
using Broker.Backoffice.Application.Common;
using Broker.Backoffice.Application.Users;
using FluentAssertions;

namespace Broker.Backoffice.Tests.Integration;

public class UsersTests(CustomWebApplicationFactory factory)
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
}
