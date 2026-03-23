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

    /// <summary>
    /// Creates a minimal valid JPEG byte array (106 bytes) for testing photo upload.
    /// </summary>
    protected static byte[] CreateMinimalJpeg()
    {
        // Minimal JPEG >= 100 bytes: SOI + APP0 + comment padding + EOI
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
