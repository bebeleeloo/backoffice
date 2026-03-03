using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Broker.Backoffice.Application.Auth;
using FluentAssertions;

namespace Broker.Backoffice.Tests.Integration;

public class AuthTests(CustomWebApplicationFactory factory) : IntegrationTestBase(factory)
{

    [Fact]
    public async Task Login_WithValidCredentials_ShouldReturnTokens()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/auth/login",
            new { Username = "admin", Password = "Admin123!" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var auth = await response.Content.ReadFromJsonAsync<AuthResponse>();
        auth!.AccessToken.Should().NotBeNullOrEmpty();
        auth.RefreshToken.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Login_WithInvalidCredentials_ShouldReturn401()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/auth/login",
            new { Username = "admin", Password = "wrong" });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Me_WithToken_ShouldReturnProfile()
    {
        var loginResp = await _client.PostAsJsonAsync("/api/v1/auth/login",
            new { Username = "admin", Password = "Admin123!" });
        var auth = await loginResp.Content.ReadFromJsonAsync<AuthResponse>();

        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", auth!.AccessToken);

        var meResp = await _client.GetAsync("/api/v1/auth/me");
        meResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var profile = await meResp.Content.ReadFromJsonAsync<UserProfileResponse>();
        profile!.Username.Should().Be("admin");
        profile.Permissions.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Me_WithoutToken_ShouldReturn401()
    {
        var response = await _client.GetAsync("/api/v1/auth/me");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
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
    public async Task ChangePassword_WithValidCurrent_ShouldReturn204()
    {
        await AuthenticateAsync();
        var response = await _client.PostAsJsonAsync("/api/v1/auth/change-password",
            new { CurrentPassword = "Admin123!", NewPassword = "NewPass123!" });
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Revert password back
        _client.DefaultRequestHeaders.Authorization = null;
        var loginResp = await _client.PostAsJsonAsync("/api/v1/auth/login",
            new { Username = "admin", Password = "NewPass123!" });
        loginResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var auth = await loginResp.Content.ReadFromJsonAsync<AuthResponse>();
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", auth!.AccessToken);
        await _client.PostAsJsonAsync("/api/v1/auth/change-password",
            new { CurrentPassword = "NewPass123!", NewPassword = "Admin123!" });
    }

    [Fact]
    public async Task ChangePassword_WithWrongCurrent_ShouldReturn401()
    {
        await AuthenticateAsync();
        var response = await _client.PostAsJsonAsync("/api/v1/auth/change-password",
            new { CurrentPassword = "WrongPass!", NewPassword = "NewPass123!" });
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ChangePassword_Unauthenticated_ShouldReturn401()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/auth/change-password",
            new { CurrentPassword = "Admin123!", NewPassword = "NewPass123!" });
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UpdateProfile_ShouldReturnUpdated()
    {
        await AuthenticateAsync();
        var response = await _client.PutAsJsonAsync("/api/v1/auth/profile",
            new { FullName = "Updated Admin", Email = "admin@test.com" });
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var profile = await response.Content.ReadFromJsonAsync<UserProfileResponse>();
        profile!.FullName.Should().Be("Updated Admin");

        // Revert
        await _client.PutAsJsonAsync("/api/v1/auth/profile",
            new { FullName = "Admin", Email = "admin@admin.com" });
    }

    [Fact]
    public async Task UpdateProfile_Unauthenticated_ShouldReturn401()
    {
        var response = await _client.PutAsJsonAsync("/api/v1/auth/profile",
            new { FullName = "Hacker", Email = "hacker@test.com" });
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UploadPhoto_ShouldReturnNoContent()
    {
        await AuthenticateAsync();
        var content = new MultipartFormDataContent();
        var byteContent = new ByteArrayContent(new byte[] { 0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46 });
        byteContent.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
        content.Add(byteContent, "file", "test.jpg");

        var response = await _client.PutAsync("/api/v1/auth/photo", content);
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task GetPhoto_AfterUpload_ShouldReturnImage()
    {
        await AuthenticateAsync();

        // Upload
        var content = new MultipartFormDataContent();
        var byteContent = new ByteArrayContent(new byte[] { 0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46 });
        byteContent.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
        content.Add(byteContent, "file", "test.jpg");
        await _client.PutAsync("/api/v1/auth/photo", content);

        // Get
        var response = await _client.GetAsync("/api/v1/auth/photo");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be("image/jpeg");
    }

    [Fact]
    public async Task DeletePhoto_ShouldReturnNoContent()
    {
        await AuthenticateAsync();

        // Upload first
        var content = new MultipartFormDataContent();
        var byteContent = new ByteArrayContent(new byte[] { 0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46 });
        byteContent.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
        content.Add(byteContent, "file", "test.jpg");
        await _client.PutAsync("/api/v1/auth/photo", content);

        // Delete
        var response = await _client.DeleteAsync("/api/v1/auth/photo");
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task GetPhoto_Unauthenticated_ShouldReturn401()
    {
        var response = await _client.GetAsync("/api/v1/auth/photo");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UploadPhoto_Unauthenticated_ShouldReturn401()
    {
        var content = new MultipartFormDataContent();
        var byteContent = new ByteArrayContent(new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 });
        byteContent.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
        content.Add(byteContent, "file", "test.jpg");

        var response = await _client.PutAsync("/api/v1/auth/photo", content);
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UploadPhoto_NoFile_ShouldReturn400()
    {
        await AuthenticateAsync();
        // Send empty multipart form without a file
        var content = new MultipartFormDataContent();
        var response = await _client.PutAsync("/api/v1/auth/photo", content);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateProfile_DuplicateEmail_ShouldReturn409()
    {
        await AuthenticateAsync();

        // Create a user with a known email
        var otherEmail = $"other_{Guid.NewGuid():N}@test.com";
        await _client.PostAsJsonAsync("/api/v1/users", new
        {
            Username = $"other_{Guid.NewGuid():N}",
            Email = otherEmail,
            Password = "Test123!",
            IsActive = true,
            RoleIds = Array.Empty<Guid>()
        });

        // Try to update admin profile to use that email
        var response = await _client.PutAsJsonAsync("/api/v1/auth/profile",
            new { FullName = "Admin", Email = otherEmail });
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

}
