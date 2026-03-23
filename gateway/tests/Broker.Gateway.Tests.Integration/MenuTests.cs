using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;

namespace Broker.Gateway.Tests.Integration;

public sealed class MenuTests(CustomWebApplicationFactory factory) : IntegrationTestBase(factory)
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    [Fact]
    public async Task GetMenu_Authorized_ReturnsFilteredMenu()
    {
        Authenticate();

        // Set up known menu state first
        var setupConfig = new
        {
            menu = new object[]
            {
                new { id = "dashboard", label = "Dashboard", icon = "Dashboard", path = "/" },
                new { id = "clients", label = "Clients", icon = "Groups", path = "/clients", permissions = new[] { "clients.read" } },
                new { id = "admin-only", label = "Admin", icon = "Settings", path = "/admin", permissions = new[] { "settings.manage" } }
            }
        };
        var setupResponse = await _client.PutAsJsonAsync("/api/v1/config/menu", setupConfig);
        setupResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Now test with limited permissions
        AuthenticateWithPermissions(
            permissions: new[] { "clients.read" },
            roles: new[] { "Viewer" });

        var response = await _client.GetAsync("/api/v1/config/menu");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadAsStringAsync();
        var items = JsonSerializer.Deserialize<List<MenuItemDto>>(json, JsonOptions);

        items.Should().NotBeNull();
        // Should contain clients (user has clients.read)
        items!.Should().Contain(x => x.Id == "clients");
        // Should contain dashboard (no permissions required)
        items.Should().Contain(x => x.Id == "dashboard");
        // Should NOT contain admin-only (requires settings.manage)
        items.Should().NotContain(x => x.Id == "admin-only");
    }

    [Fact]
    public async Task GetMenuRaw_WithSettingsManage_ReturnsFullMenu()
    {
        Authenticate();

        // Set up known menu state first
        var setupConfig = new
        {
            menu = new object[]
            {
                new { id = "dashboard", label = "Dashboard", icon = "Dashboard", path = "/" },
                new { id = "gateway", label = "Gateway", icon = "Router", path = "/config", permissions = new[] { "settings.manage" } }
            }
        };
        var setupResponse = await _client.PutAsJsonAsync("/api/v1/config/menu", setupConfig);
        setupResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var response = await _client.GetAsync("/api/v1/config/menu/raw");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadAsStringAsync();
        var items = JsonSerializer.Deserialize<List<MenuItemDto>>(json, JsonOptions);

        items.Should().NotBeNull();
        items!.Count.Should().BeGreaterThan(0);
        // Raw menu should include all items including settings.manage-gated ones
        items.Should().Contain(x => x.Id == "gateway");
    }

    [Fact]
    public async Task SaveMenu_ValidConfig_Returns200()
    {
        Authenticate();

        var config = new
        {
            menu = new[]
            {
                new { id = "test-item", label = "Test", icon = "Dashboard", path = "/test" }
            }
        };

        var response = await _client.PutAsJsonAsync("/api/v1/config/menu", config);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<MessageResponse>(JsonOptions);
        body.Should().NotBeNull();
        body!.Message.Should().Contain("saved");
    }

    [Fact]
    public async Task SaveMenu_EmptyMenu_Returns400()
    {
        Authenticate();

        var config = new { menu = Array.Empty<object>() };

        var response = await _client.PutAsJsonAsync("/api/v1/config/menu", config);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetMenu_Unauthorized_Returns401()
    {
        // No auth header set
        var response = await _client.GetAsync("/api/v1/config/menu");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetMenuRaw_WithoutSettingsManage_Returns403()
    {
        AuthenticateWithPermissions(
            permissions: new[] { "clients.read" },
            roles: new[] { "Viewer" });

        var response = await _client.GetAsync("/api/v1/config/menu/raw");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    private sealed class MenuItemDto
    {
        public string Id { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public string? Path { get; set; }
        public List<string>? Permissions { get; set; }
        public List<MenuItemDto>? Children { get; set; }
    }

    private sealed class MessageResponse
    {
        public string Message { get; set; } = string.Empty;
    }
}
