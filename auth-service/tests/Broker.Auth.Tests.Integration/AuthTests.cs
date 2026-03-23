using System.Text.Json;

namespace Broker.Auth.Tests.Integration;

public class AuthTests(CustomWebApplicationFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task Login_WithValidCredentials_ShouldReturnTokens()
    {
        var resp = await _client.PostAsJsonAsync("/api/v1/auth/login",
            new { Username = "admin", Password = "Admin123!" });
        resp.StatusCode.Should().Be(HttpStatusCode.OK);

        var auth = await resp.Content.ReadFromJsonAsync<AuthResponse>();
        auth!.AccessToken.Should().NotBeNullOrEmpty();
        auth.RefreshToken.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Login_WithInvalidCredentials_ShouldReturn401()
    {
        var resp = await _client.PostAsJsonAsync("/api/v1/auth/login",
            new { Username = "admin", Password = "wrong" });
        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Me_WithToken_ShouldReturnProfile()
    {
        await AuthenticateAsync();
        var resp = await _client.GetAsync("/api/v1/auth/me");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Me_WithoutToken_ShouldReturn401()
    {
        var resp = await _client.GetAsync("/api/v1/auth/me");
        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task RefreshToken_ShouldRotateTokens()
    {
        var loginResp = await _client.PostAsJsonAsync("/api/v1/auth/login",
            new { Username = "admin", Password = "Admin123!" });
        var auth = await loginResp.Content.ReadFromJsonAsync<AuthResponse>();

        var refreshResp = await _client.PostAsJsonAsync("/api/v1/auth/refresh",
            new { RefreshToken = auth!.RefreshToken });
        refreshResp.StatusCode.Should().Be(HttpStatusCode.OK);

        var newAuth = await refreshResp.Content.ReadFromJsonAsync<AuthResponse>();
        newAuth!.AccessToken.Should().NotBe(auth.AccessToken);
        newAuth.RefreshToken.Should().NotBe(auth.RefreshToken);
    }

    [Fact]
    public async Task ChangePassword_WithValidData_Returns204()
    {
        // Create a dedicated user for this test to avoid mutating the shared admin
        await AuthenticateAsync();
        var username = $"chpwd_{Guid.NewGuid():N}"[..20];
        var originalPassword = "OldPass123!";
        var createResp = await _client.PostAsJsonAsync("/api/v1/users", new
        {
            Username = username,
            Email = $"{username}@test.com",
            Password = originalPassword,
            FullName = "Change Password User",
            IsActive = true,
            RoleIds = Array.Empty<Guid>()
        });
        createResp.StatusCode.Should().Be(HttpStatusCode.Created);

        // Authenticate as the new user
        await AuthenticateAsAsync(username, originalPassword);

        var resp = await _client.PostAsJsonAsync("/api/v1/auth/change-password", new
        {
            CurrentPassword = originalPassword,
            NewPassword = "NewPass456!"
        });
        resp.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify new password works
        var loginResp = await _client.PostAsJsonAsync("/api/v1/auth/login",
            new { Username = username, Password = "NewPass456!" });
        loginResp.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ChangePassword_WithWrongCurrentPassword_Returns401()
    {
        await AuthenticateAsync();

        var resp = await _client.PostAsJsonAsync("/api/v1/auth/change-password", new
        {
            CurrentPassword = "TotallyWrongPassword!",
            NewPassword = "NewPass456!"
        });
        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UpdateProfile_WithValidData_ReturnsUpdatedProfile()
    {
        // Create a dedicated user for this test
        await AuthenticateAsync();
        var username = $"prof_{Guid.NewGuid():N}"[..20];
        var createResp = await _client.PostAsJsonAsync("/api/v1/users", new
        {
            Username = username,
            Email = $"{username}@test.com",
            Password = "Pass123!",
            FullName = "Original Name",
            IsActive = true,
            RoleIds = Array.Empty<Guid>()
        });
        createResp.StatusCode.Should().Be(HttpStatusCode.Created);

        // Authenticate as the new user
        await AuthenticateAsAsync(username, "Pass123!");

        var newEmail = $"updated_{Guid.NewGuid():N}@test.com";
        var resp = await _client.PutAsJsonAsync("/api/v1/auth/profile", new
        {
            FullName = "Updated Name",
            Email = newEmail
        });
        resp.StatusCode.Should().Be(HttpStatusCode.OK);

        var profile = await resp.Content.ReadFromJsonAsync<JsonElement>();
        profile.GetProperty("fullName").GetString().Should().Be("Updated Name");
        profile.GetProperty("email").GetString().Should().Be(newEmail);
    }

    [Fact]
    public async Task UpdateProfile_WithDuplicateEmail_Returns409()
    {
        // Create two users
        await AuthenticateAsync();
        var username1 = $"dup1_{Guid.NewGuid():N}"[..20];
        var username2 = $"dup2_{Guid.NewGuid():N}"[..20];
        var existingEmail = $"{username1}@test.com";

        await _client.PostAsJsonAsync("/api/v1/users", new
        {
            Username = username1,
            Email = existingEmail,
            Password = "Pass123!",
            FullName = "User One",
            IsActive = true,
            RoleIds = Array.Empty<Guid>()
        });
        await _client.PostAsJsonAsync("/api/v1/users", new
        {
            Username = username2,
            Email = $"{username2}@test.com",
            Password = "Pass123!",
            FullName = "User Two",
            IsActive = true,
            RoleIds = Array.Empty<Guid>()
        });

        // Authenticate as user2 and try to take user1's email
        await AuthenticateAsAsync(username2, "Pass123!");

        var resp = await _client.PutAsJsonAsync("/api/v1/auth/profile", new
        {
            FullName = "User Two",
            Email = existingEmail
        });
        resp.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task UploadPhoto_ReturnsNoContent()
    {
        await AuthenticateAsync();

        // Create a minimal valid JPEG (smallest valid JPEG is a few hundred bytes, but a simple byte array with correct header works)
        var jpegBytes = CreateMinimalJpeg();
        var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(jpegBytes);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
        content.Add(fileContent, "file", "photo.jpg");

        var resp = await _client.PutAsync("/api/v1/auth/photo", content);
        resp.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task GetPhoto_ReturnsImage()
    {
        await AuthenticateAsync();

        // Upload a photo first
        var jpegBytes = CreateMinimalJpeg();
        var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(jpegBytes);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
        content.Add(fileContent, "file", "photo.jpg");
        var uploadResp = await _client.PutAsync("/api/v1/auth/photo", content);
        uploadResp.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Get the photo
        var resp = await _client.GetAsync("/api/v1/auth/photo");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        resp.Content.Headers.ContentType!.MediaType.Should().Be("image/jpeg");

        var photoData = await resp.Content.ReadAsByteArrayAsync();
        photoData.Length.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task DeletePhoto_ReturnsNoContent()
    {
        await AuthenticateAsync();

        // Upload a photo first
        var jpegBytes = CreateMinimalJpeg();
        var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(jpegBytes);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
        content.Add(fileContent, "file", "photo.jpg");
        await _client.PutAsync("/api/v1/auth/photo", content);

        // Delete the photo
        var resp = await _client.DeleteAsync("/api/v1/auth/photo");
        resp.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Logout_ValidToken_RevokesRefreshToken()
    {
        // Login to get tokens
        var loginResp = await _client.PostAsJsonAsync("/api/v1/auth/login",
            new { Username = "admin", Password = "Admin123!" });
        loginResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var auth = await loginResp.Content.ReadFromJsonAsync<AuthResponse>();

        // Set access token for the logout call (requires [Authorize])
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", auth!.AccessToken);

        // Logout with the refresh token
        var logoutResp = await _client.PostAsJsonAsync("/api/v1/auth/logout",
            new { RefreshToken = auth.RefreshToken });
        logoutResp.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Try to use the revoked refresh token — should fail
        _client.DefaultRequestHeaders.Authorization = null;
        var refreshResp = await _client.PostAsJsonAsync("/api/v1/auth/refresh",
            new { RefreshToken = auth.RefreshToken });
        refreshResp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Logout_InvalidToken_Returns401()
    {
        await AuthenticateAsync();

        var resp = await _client.PostAsJsonAsync("/api/v1/auth/logout",
            new { RefreshToken = "non-existent-refresh-token-value" });
        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Logout_Unauthenticated_Returns401()
    {
        // No auth header set — logout requires [Authorize]
        var resp = await _client.PostAsJsonAsync("/api/v1/auth/logout",
            new { RefreshToken = "some-token" });
        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_ViaBasicAuth_Succeeds()
    {
        // Send Basic Auth header with empty JSON body
        var credentials = Convert.ToBase64String(
            System.Text.Encoding.UTF8.GetBytes("admin:Admin123!"));

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/login");
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", credentials);
        request.Content = new StringContent("{}", System.Text.Encoding.UTF8, "application/json");

        var resp = await _client.SendAsync(request);
        resp.StatusCode.Should().Be(HttpStatusCode.OK);

        var auth = await resp.Content.ReadFromJsonAsync<AuthResponse>();
        auth!.AccessToken.Should().NotBeNullOrEmpty();
        auth.RefreshToken.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Login_ViaBasicAuth_InvalidCredentials_Returns401()
    {
        var credentials = Convert.ToBase64String(
            System.Text.Encoding.UTF8.GetBytes("admin:WrongPassword!"));

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/login");
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", credentials);
        request.Content = new StringContent("{}", System.Text.Encoding.UTF8, "application/json");

        var resp = await _client.SendAsync(request);
        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetPhoto_Unauthenticated_Returns401()
    {
        // GET /auth/photo requires [Authorize], unlike GET /users/{id}/photo
        var resp = await _client.GetAsync("/api/v1/auth/photo");
        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetPhoto_CacheControl_ReturnsPrivateHeader()
    {
        await AuthenticateAsync();

        // Upload a photo first
        var jpegBytes = CreateMinimalJpeg();
        var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(jpegBytes);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
        content.Add(fileContent, "file", "photo.jpg");
        var uploadResp = await _client.PutAsync("/api/v1/auth/photo", content);
        uploadResp.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Get the photo and check cache headers
        var resp = await _client.GetAsync("/api/v1/auth/photo");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        resp.Headers.CacheControl.Should().NotBeNull();
        resp.Headers.CacheControl!.Private.Should().BeTrue();
        resp.Headers.CacheControl.MaxAge.Should().Be(TimeSpan.FromSeconds(3600));
    }

    /// <summary>
    /// Creates a minimal valid JPEG byte array for testing photo upload.
    /// </summary>
    private static byte[] CreateMinimalJpeg()
    {
        // Minimal JPEG: SOI marker + APP0 + minimal frame + EOI
        // This is a valid JPEG structure that image parsers accept
        return
        [
            0xFF, 0xD8, // SOI (Start of Image)
            0xFF, 0xE0, // APP0 marker
            0x00, 0x10, // APP0 length (16)
            0x4A, 0x46, 0x49, 0x46, 0x00, // JFIF identifier
            0x01, 0x01, // Version
            0x00, // Units
            0x00, 0x01, // X density
            0x00, 0x01, // Y density
            0x00, 0x00, // Thumbnail dimensions
            0xFF, 0xD9  // EOI (End of Image)
        ];
    }
}
