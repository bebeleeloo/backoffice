using System.Text.Json;

namespace Broker.Auth.Tests.Integration;

public class UsersTests(CustomWebApplicationFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task ListUsers_ShouldReturnPagedResult()
    {
        await AuthenticateAsync();
        var resp = await _client.GetAsync("/api/v1/users");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task CreateUser_ShouldReturn201()
    {
        await AuthenticateAsync();
        var resp = await _client.PostAsJsonAsync("/api/v1/users", new
        {
            Username = $"test_{Guid.NewGuid():N}"[..20],
            Email = $"test_{Guid.NewGuid():N}@test.com",
            Password = "Pass123!",
            FullName = "Test User",
            IsActive = true,
            RoleIds = Array.Empty<Guid>()
        });
        resp.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task GetUserStats_ShouldReturnStats()
    {
        await AuthenticateAsync();
        var resp = await _client.GetAsync("/api/v1/users/stats");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await resp.Content.ReadFromJsonAsync<JsonElement>();
        json.GetProperty("totalUsers").GetInt32().Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetUserById_ReturnsUser()
    {
        await AuthenticateAsync();

        // Create a user first
        var username = $"getby_{Guid.NewGuid():N}"[..20];
        var createResp = await _client.PostAsJsonAsync("/api/v1/users", new
        {
            Username = username,
            Email = $"{username}@test.com",
            Password = "Pass123!",
            FullName = "Get By Id User",
            IsActive = true,
            RoleIds = Array.Empty<Guid>()
        });
        createResp.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await createResp.Content.ReadFromJsonAsync<JsonElement>();
        var userId = created.GetProperty("id").GetString();

        // Get user by id
        var resp = await _client.GetAsync($"/api/v1/users/{userId}");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);

        var user = await resp.Content.ReadFromJsonAsync<JsonElement>();
        user.GetProperty("id").GetString().Should().Be(userId);
        user.GetProperty("username").GetString().Should().Be(username);
        user.GetProperty("fullName").GetString().Should().Be("Get By Id User");
    }

    [Fact]
    public async Task UpdateUser_WithValidData_ReturnsUpdatedUser()
    {
        await AuthenticateAsync();

        // Create a user
        var username = $"upd_{Guid.NewGuid():N}"[..20];
        var createResp = await _client.PostAsJsonAsync("/api/v1/users", new
        {
            Username = username,
            Email = $"{username}@test.com",
            Password = "Pass123!",
            FullName = "Original Name",
            IsActive = true,
            RoleIds = Array.Empty<Guid>()
        });
        var created = await createResp.Content.ReadFromJsonAsync<JsonElement>();
        var userId = created.GetProperty("id").GetString();
        var rowVersion = created.GetProperty("rowVersion").GetUInt32();

        // Update the user
        var newEmail = $"updated_{Guid.NewGuid():N}@test.com";
        var resp = await _client.PutAsJsonAsync($"/api/v1/users/{userId}", new
        {
            Id = userId,
            Email = newEmail,
            FullName = "Updated Name",
            IsActive = true,
            RoleIds = Array.Empty<Guid>(),
            RowVersion = rowVersion
        });
        resp.StatusCode.Should().Be(HttpStatusCode.OK);

        var updated = await resp.Content.ReadFromJsonAsync<JsonElement>();
        updated.GetProperty("fullName").GetString().Should().Be("Updated Name");
        updated.GetProperty("email").GetString().Should().Be(newEmail);
    }

    [Fact]
    public async Task DeleteUser_ReturnsNoContent()
    {
        await AuthenticateAsync();

        // Create a user to delete
        var username = $"del_{Guid.NewGuid():N}"[..20];
        var createResp = await _client.PostAsJsonAsync("/api/v1/users", new
        {
            Username = username,
            Email = $"{username}@test.com",
            Password = "Pass123!",
            FullName = "Delete Me User",
            IsActive = true,
            RoleIds = Array.Empty<Guid>()
        });
        var created = await createResp.Content.ReadFromJsonAsync<JsonElement>();
        var userId = created.GetProperty("id").GetString();

        // Delete user
        var resp = await _client.DeleteAsync($"/api/v1/users/{userId}");
        resp.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify user is gone
        var getResp = await _client.GetAsync($"/api/v1/users/{userId}");
        getResp.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateUser_WithDuplicateUsername_Returns409()
    {
        await AuthenticateAsync();

        var username = $"dupusr_{Guid.NewGuid():N}"[..20];
        var email1 = $"{username}_1@test.com";
        var email2 = $"{username}_2@test.com";

        // Create first user
        var resp1 = await _client.PostAsJsonAsync("/api/v1/users", new
        {
            Username = username,
            Email = email1,
            Password = "Pass123!",
            FullName = "User One",
            IsActive = true,
            RoleIds = Array.Empty<Guid>()
        });
        resp1.StatusCode.Should().Be(HttpStatusCode.Created);

        // Try to create second user with same username
        var resp2 = await _client.PostAsJsonAsync("/api/v1/users", new
        {
            Username = username,
            Email = email2,
            Password = "Pass123!",
            FullName = "User Two",
            IsActive = true,
            RoleIds = Array.Empty<Guid>()
        });
        resp2.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task CreateUser_WithDuplicateEmail_Returns409()
    {
        await AuthenticateAsync();

        var sharedEmail = $"shared_{Guid.NewGuid():N}@test.com";

        // Create first user
        var resp1 = await _client.PostAsJsonAsync("/api/v1/users", new
        {
            Username = $"usr1_{Guid.NewGuid():N}"[..20],
            Email = sharedEmail,
            Password = "Pass123!",
            FullName = "User One",
            IsActive = true,
            RoleIds = Array.Empty<Guid>()
        });
        resp1.StatusCode.Should().Be(HttpStatusCode.Created);

        // Try to create second user with same email
        var resp2 = await _client.PostAsJsonAsync("/api/v1/users", new
        {
            Username = $"usr2_{Guid.NewGuid():N}"[..20],
            Email = sharedEmail,
            Password = "Pass123!",
            FullName = "User Two",
            IsActive = true,
            RoleIds = Array.Empty<Guid>()
        });
        resp2.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task UpdateUser_WithDuplicateEmail_Returns409()
    {
        await AuthenticateAsync();

        var email1 = $"ueml1_{Guid.NewGuid():N}@test.com";
        var email2 = $"ueml2_{Guid.NewGuid():N}@test.com";

        // Create two users
        var resp1 = await _client.PostAsJsonAsync("/api/v1/users", new
        {
            Username = $"eml1_{Guid.NewGuid():N}"[..20],
            Email = email1,
            Password = "Pass123!",
            FullName = "User One",
            IsActive = true,
            RoleIds = Array.Empty<Guid>()
        });
        resp1.StatusCode.Should().Be(HttpStatusCode.Created);

        var resp2 = await _client.PostAsJsonAsync("/api/v1/users", new
        {
            Username = $"eml2_{Guid.NewGuid():N}"[..20],
            Email = email2,
            Password = "Pass123!",
            FullName = "User Two",
            IsActive = true,
            RoleIds = Array.Empty<Guid>()
        });
        resp2.StatusCode.Should().Be(HttpStatusCode.Created);
        var user2 = await resp2.Content.ReadFromJsonAsync<JsonElement>();
        var user2Id = user2.GetProperty("id").GetString();
        var user2RowVersion = user2.GetProperty("rowVersion").GetUInt32();

        // Try to update user2's email to user1's email
        var updateResp = await _client.PutAsJsonAsync($"/api/v1/users/{user2Id}", new
        {
            Id = user2Id,
            Email = email1,
            FullName = "User Two",
            IsActive = true,
            RoleIds = Array.Empty<Guid>(),
            RowVersion = user2RowVersion
        });
        updateResp.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task UpdateUser_RouteBodyIdMismatch_Returns400()
    {
        await AuthenticateAsync();

        var routeId = Guid.NewGuid();
        var bodyId = Guid.NewGuid();

        var resp = await _client.PutAsJsonAsync($"/api/v1/users/{routeId}", new
        {
            Id = bodyId,
            Email = "test@test.com",
            FullName = "Mismatch User",
            IsActive = true,
            RoleIds = Array.Empty<Guid>(),
            RowVersion = 1u
        });
        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UploadUserPhoto_ReturnsNoContent()
    {
        await AuthenticateAsync();

        // Create a user
        var username = $"photo_{Guid.NewGuid():N}"[..20];
        var createResp = await _client.PostAsJsonAsync("/api/v1/users", new
        {
            Username = username,
            Email = $"{username}@test.com",
            Password = "Pass123!",
            FullName = "Photo User",
            IsActive = true,
            RoleIds = Array.Empty<Guid>()
        });
        var created = await createResp.Content.ReadFromJsonAsync<JsonElement>();
        var userId = created.GetProperty("id").GetString();

        // Upload photo
        var jpegBytes = CreateMinimalJpeg();
        var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(jpegBytes);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
        content.Add(fileContent, "file", "photo.jpg");

        var resp = await _client.PutAsync($"/api/v1/users/{userId}/photo", content);
        resp.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task GetUserPhoto_ReturnsImage()
    {
        await AuthenticateAsync();

        // Create a user and upload a photo
        var username = $"gphoto_{Guid.NewGuid():N}"[..20];
        var createResp = await _client.PostAsJsonAsync("/api/v1/users", new
        {
            Username = username,
            Email = $"{username}@test.com",
            Password = "Pass123!",
            FullName = "Photo Get User",
            IsActive = true,
            RoleIds = Array.Empty<Guid>()
        });
        var created = await createResp.Content.ReadFromJsonAsync<JsonElement>();
        var userId = created.GetProperty("id").GetString();

        var jpegBytes = CreateMinimalJpeg();
        var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(jpegBytes);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
        content.Add(fileContent, "file", "photo.jpg");
        await _client.PutAsync($"/api/v1/users/{userId}/photo", content);

        // Get photo (AllowAnonymous endpoint — no auth needed)
        var resp = await _client.GetAsync($"/api/v1/users/{userId}/photo");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        resp.Content.Headers.ContentType!.MediaType.Should().Be("image/jpeg");

        var photoData = await resp.Content.ReadAsByteArrayAsync();
        photoData.Length.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task DeleteUserPhoto_ReturnsNoContent()
    {
        await AuthenticateAsync();

        // Create a user and upload a photo
        var username = $"dphoto_{Guid.NewGuid():N}"[..20];
        var createResp = await _client.PostAsJsonAsync("/api/v1/users", new
        {
            Username = username,
            Email = $"{username}@test.com",
            Password = "Pass123!",
            FullName = "Photo Delete User",
            IsActive = true,
            RoleIds = Array.Empty<Guid>()
        });
        var created = await createResp.Content.ReadFromJsonAsync<JsonElement>();
        var userId = created.GetProperty("id").GetString();

        var jpegBytes = CreateMinimalJpeg();
        var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(jpegBytes);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
        content.Add(fileContent, "file", "photo.jpg");
        await _client.PutAsync($"/api/v1/users/{userId}/photo", content);

        // Delete photo
        var resp = await _client.DeleteAsync($"/api/v1/users/{userId}/photo");
        resp.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task ResetUserPassword_WithValidData_Returns204()
    {
        await AuthenticateAsync();

        // Create a user
        var username = $"rp_{Guid.NewGuid():N}"[..20];
        var password = "OldPass123!";
        var createResp = await _client.PostAsJsonAsync("/api/v1/users", new
        {
            Username = username,
            Email = $"{username}@test.com",
            Password = password,
            FullName = "Reset Password User",
            IsActive = true,
            RoleIds = Array.Empty<Guid>()
        });
        createResp.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await createResp.Content.ReadFromJsonAsync<JsonElement>();
        var userId = created.GetProperty("id").GetString();

        // Reset password
        var newPassword = "NewPass456!";
        var resp = await _client.PostAsJsonAsync($"/api/v1/users/{userId}/reset-password", new
        {
            NewPassword = newPassword
        });
        resp.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify login with new password
        var loginResp = await _client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            Username = username,
            Password = newPassword
        });
        loginResp.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ResetUserPassword_NonExistentUser_Returns404()
    {
        await AuthenticateAsync();

        var resp = await _client.PostAsJsonAsync($"/api/v1/users/{Guid.NewGuid()}/reset-password", new
        {
            NewPassword = "NewPass456!"
        });
        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    /// <summary>
    /// Creates a minimal valid JPEG byte array for testing photo upload.
    /// </summary>
    private static byte[] CreateMinimalJpeg()
    {
        // Minimal JPEG ≥ 100 bytes: SOI + APP0 + comment padding + EOI
        var header = new byte[]
        {
            0xFF, 0xD8, // SOI
            0xFF, 0xE0, // APP0 marker
            0x00, 0x10, // APP0 length (16)
            0x4A, 0x46, 0x49, 0x46, 0x00, // JFIF
            0x01, 0x01, 0x00,
            0x00, 0x01, 0x00, 0x01,
            0x00, 0x00,
            0xFF, 0xFE, // COM marker
            0x00, 0x52, // Comment length (82)
        };
        var padding = new byte[80];
        var footer = new byte[] { 0xFF, 0xD9 };

        var result = new byte[header.Length + padding.Length + footer.Length];
        header.CopyTo(result, 0);
        padding.CopyTo(result, header.Length);
        footer.CopyTo(result, header.Length + padding.Length);
        return result;
    }
}
