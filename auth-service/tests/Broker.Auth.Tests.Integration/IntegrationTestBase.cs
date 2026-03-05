namespace Broker.Auth.Tests.Integration;

[Collection("Integration")]
public abstract class IntegrationTestBase
{
    protected readonly HttpClient _client;
    protected readonly CustomWebApplicationFactory _factory;

    protected IntegrationTestBase(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    protected async Task AuthenticateAsync()
    {
        var loginResp = await _client.PostAsJsonAsync("/api/v1/auth/login",
            new { Username = "admin", Password = "Admin123!" });
        var auth = await loginResp.Content.ReadFromJsonAsync<AuthResponse>();
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", auth!.AccessToken);
    }

    protected async Task AuthenticateAsAsync(string username, string password)
    {
        var loginResp = await _client.PostAsJsonAsync("/api/v1/auth/login",
            new { Username = username, Password = password });
        var auth = await loginResp.Content.ReadFromJsonAsync<AuthResponse>();
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", auth!.AccessToken);
    }

    protected record AuthResponse(string AccessToken, string RefreshToken, DateTime ExpiresAt);
}
