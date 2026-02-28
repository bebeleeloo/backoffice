using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Broker.Backoffice.Application.Auth;
using FluentAssertions;

namespace Broker.Backoffice.Tests.Integration;

[Collection("Integration")]
public class AuthTests(CustomWebApplicationFactory factory)
{
    private readonly HttpClient _client = factory.CreateClient();

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

    private async Task AuthenticateAsync()
    {
        var loginResp = await _client.PostAsJsonAsync("/api/v1/auth/login",
            new { Username = "admin", Password = "Admin123!" });
        var auth = await loginResp.Content.ReadFromJsonAsync<AuthResponse>();
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", auth!.AccessToken);
    }
}
