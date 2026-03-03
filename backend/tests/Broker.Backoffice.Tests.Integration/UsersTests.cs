using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Broker.Backoffice.Application.Common;
using Broker.Backoffice.Application.Users;
using FluentAssertions;

namespace Broker.Backoffice.Tests.Integration;

public class UsersTests(CustomWebApplicationFactory factory) : IntegrationTestBase(factory)
{

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

    [Fact]
    public async Task UploadPhoto_ShouldSetHasPhoto()
    {
        await AuthenticateAsync();
        var createResp = await _client.PostAsJsonAsync("/api/v1/users", new
        {
            Username = $"photo_{Guid.NewGuid():N}",
            Email = $"photo_{Guid.NewGuid():N}@test.com",
            Password = "Test123!",
            IsActive = true,
            RoleIds = Array.Empty<Guid>()
        });
        var created = await createResp.Content.ReadFromJsonAsync<UserDto>();
        created!.HasPhoto.Should().BeFalse();

        // Upload photo
        var content = new MultipartFormDataContent();
        var imageBytes = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46 }; // minimal JPEG header
        var byteContent = new ByteArrayContent(imageBytes);
        byteContent.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
        content.Add(byteContent, "file", "test.jpg");
        var uploadResp = await _client.PutAsync($"/api/v1/users/{created.Id}/photo", content);
        uploadResp.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify hasPhoto
        var getResp = await _client.GetAsync($"/api/v1/users/{created.Id}");
        var user = await getResp.Content.ReadFromJsonAsync<UserDto>();
        user!.HasPhoto.Should().BeTrue();
    }

    [Fact]
    public async Task GetPhoto_NoPhoto_ShouldReturn404()
    {
        await AuthenticateAsync();
        var createResp = await _client.PostAsJsonAsync("/api/v1/users", new
        {
            Username = $"nophoto_{Guid.NewGuid():N}",
            Email = $"nophoto_{Guid.NewGuid():N}@test.com",
            Password = "Test123!",
            IsActive = true,
            RoleIds = Array.Empty<Guid>()
        });
        var created = await createResp.Content.ReadFromJsonAsync<UserDto>();

        var photoResp = await _client.GetAsync($"/api/v1/users/{created!.Id}/photo");
        photoResp.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetPhoto_Anonymous_ShouldWork()
    {
        await AuthenticateAsync();
        var createResp = await _client.PostAsJsonAsync("/api/v1/users", new
        {
            Username = $"anonphoto_{Guid.NewGuid():N}",
            Email = $"anonphoto_{Guid.NewGuid():N}@test.com",
            Password = "Test123!",
            IsActive = true,
            RoleIds = Array.Empty<Guid>()
        });
        var created = await createResp.Content.ReadFromJsonAsync<UserDto>();

        // Upload photo
        var content = new MultipartFormDataContent();
        var byteContent = new ByteArrayContent(new byte[] { 0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46 });
        byteContent.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
        content.Add(byteContent, "file", "test.jpg");
        await _client.PutAsync($"/api/v1/users/{created!.Id}/photo", content);

        // Fetch without auth
        var anonClient = _factory.CreateClient();
        var photoResp = await anonClient.GetAsync($"/api/v1/users/{created.Id}/photo");
        photoResp.StatusCode.Should().Be(HttpStatusCode.OK);
        photoResp.Content.Headers.ContentType!.MediaType.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task DeletePhoto_ShouldRemovePhoto()
    {
        await AuthenticateAsync();
        var createResp = await _client.PostAsJsonAsync("/api/v1/users", new
        {
            Username = $"delphoto_{Guid.NewGuid():N}",
            Email = $"delphoto_{Guid.NewGuid():N}@test.com",
            Password = "Test123!",
            IsActive = true,
            RoleIds = Array.Empty<Guid>()
        });
        var created = await createResp.Content.ReadFromJsonAsync<UserDto>();

        // Upload
        var content = new MultipartFormDataContent();
        var byteContent = new ByteArrayContent(new byte[] { 0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46 });
        byteContent.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
        content.Add(byteContent, "file", "test.jpg");
        await _client.PutAsync($"/api/v1/users/{created!.Id}/photo", content);

        // Delete
        var delResp = await _client.DeleteAsync($"/api/v1/users/{created.Id}/photo");
        delResp.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify gone
        var photoResp = await _client.GetAsync($"/api/v1/users/{created.Id}/photo");
        photoResp.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateUser_DuplicateEmail_ShouldReturn409()
    {
        await AuthenticateAsync();
        var email = $"dupemail_{Guid.NewGuid():N}@test.com";

        await _client.PostAsJsonAsync("/api/v1/users", new
        {
            Username = $"user1_{Guid.NewGuid():N}",
            Email = email,
            Password = "Test123!",
            IsActive = true,
            RoleIds = Array.Empty<Guid>()
        });

        var response = await _client.PostAsJsonAsync("/api/v1/users", new
        {
            Username = $"user2_{Guid.NewGuid():N}",
            Email = email, // duplicate email
            Password = "Test123!",
            IsActive = true,
            RoleIds = Array.Empty<Guid>()
        });
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task UpdateUser_DuplicateEmail_ShouldReturn409()
    {
        await AuthenticateAsync();
        var email1 = $"ue1_{Guid.NewGuid():N}@test.com";
        var email2 = $"ue2_{Guid.NewGuid():N}@test.com";

        await _client.PostAsJsonAsync("/api/v1/users", new
        {
            Username = $"ue1_{Guid.NewGuid():N}",
            Email = email1,
            Password = "Test123!",
            IsActive = true,
            RoleIds = Array.Empty<Guid>()
        });

        var create2Resp = await _client.PostAsJsonAsync("/api/v1/users", new
        {
            Username = $"ue2_{Guid.NewGuid():N}",
            Email = email2,
            Password = "Test123!",
            IsActive = true,
            RoleIds = Array.Empty<Guid>()
        });
        var user2 = await create2Resp.Content.ReadFromJsonAsync<UserDto>();

        // Try to update user2's email to match user1's
        var response = await _client.PutAsJsonAsync($"/api/v1/users/{user2!.Id}", new
        {
            Id = user2.Id,
            Email = email1, // duplicate
            FullName = "Test",
            IsActive = true,
            RoleIds = Array.Empty<Guid>(),
            RowVersion = user2.RowVersion,
        });
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task UpdateUser_RouteBodyIdMismatch_ShouldReturn400()
    {
        await AuthenticateAsync();
        var createResp = await _client.PostAsJsonAsync("/api/v1/users", new
        {
            Username = $"mis_{Guid.NewGuid():N}",
            Email = $"mis_{Guid.NewGuid():N}@test.com",
            Password = "Test123!",
            IsActive = true,
            RoleIds = Array.Empty<Guid>()
        });
        var created = await createResp.Content.ReadFromJsonAsync<UserDto>();

        var response = await _client.PutAsJsonAsync($"/api/v1/users/{created!.Id}", new
        {
            Id = Guid.NewGuid(), // different from route ID
            Email = created.Email,
            FullName = "Mismatch",
            IsActive = true,
            RoleIds = Array.Empty<Guid>(),
            RowVersion = created.RowVersion,
        });
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ListUsers_WithFilters_ShouldReturnFiltered()
    {
        await AuthenticateAsync();
        var response = await _client.GetAsync(
            "/api/v1/users?page=1&pageSize=10&isActive=true&q=admin");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PagedResult<UserDto>>();
        result.Should().NotBeNull();
        result!.Items.Should().NotBeEmpty();
    }
}
