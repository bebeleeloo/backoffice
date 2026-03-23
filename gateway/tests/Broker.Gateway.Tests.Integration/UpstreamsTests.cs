using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;

namespace Broker.Gateway.Tests.Integration;

public sealed class UpstreamsTests(CustomWebApplicationFactory factory) : IntegrationTestBase(factory)
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    [Fact]
    public async Task GetUpstreams_WithSettingsManage_ReturnsUpstreams()
    {
        Authenticate();

        // Set up known upstreams config
        var setupConfig = new
        {
            upstreams = new Dictionary<string, object>
            {
                ["core"] = new
                {
                    address = "http://localhost:8080",
                    routes = new[] { "/api/v1/clients" }
                },
                ["auth"] = new
                {
                    address = "http://localhost:8082",
                    routes = new[] { "/api/v1/auth" }
                }
            }
        };
        var setupResponse = await _client.PutAsJsonAsync("/api/v1/config/upstreams", setupConfig);
        setupResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var response = await _client.GetAsync("/api/v1/config/upstreams");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadAsStringAsync();
        var upstreams = JsonSerializer.Deserialize<Dictionary<string, UpstreamDto>>(json, JsonOptions);

        upstreams.Should().NotBeNull();
        upstreams!.Count.Should().BeGreaterThan(0);
        upstreams.Should().ContainKey("core");
        upstreams.Should().ContainKey("auth");
    }

    [Fact]
    public async Task SaveUpstreams_ValidConfig_Returns200()
    {
        Authenticate();

        var config = new
        {
            upstreams = new Dictionary<string, object>
            {
                ["test-service"] = new
                {
                    address = "http://localhost:9090",
                    routes = new[] { "/api/v1/test" }
                }
            }
        };

        var response = await _client.PutAsJsonAsync("/api/v1/config/upstreams", config);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<MessageResponse>(JsonOptions);
        body.Should().NotBeNull();
        body!.Message.Should().Contain("saved");
    }

    [Fact]
    public async Task SaveUpstreams_InvalidUri_Returns400()
    {
        Authenticate();

        var config = new
        {
            upstreams = new Dictionary<string, object>
            {
                ["bad-service"] = new
                {
                    address = "not-a-valid-uri",
                    routes = new[] { "/api/v1/test" }
                }
            }
        };

        var response = await _client.PutAsJsonAsync("/api/v1/config/upstreams", config);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task SaveUpstreams_EmptyRoutes_Returns400()
    {
        Authenticate();

        var config = new
        {
            upstreams = new Dictionary<string, object>
            {
                ["no-routes"] = new
                {
                    address = "http://localhost:9090",
                    routes = Array.Empty<string>()
                }
            }
        };

        var response = await _client.PutAsJsonAsync("/api/v1/config/upstreams", config);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetUpstreams_WithoutPermission_Returns403()
    {
        AuthenticateWithPermissions(
            permissions: new[] { "clients.read" },
            roles: new[] { "Viewer" });

        var response = await _client.GetAsync("/api/v1/config/upstreams");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Reload_WithSettingsManage_Returns200()
    {
        Authenticate();

        var response = await _client.PostAsync("/api/v1/config/reload", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<MessageResponse>(JsonOptions);
        body.Should().NotBeNull();
        body!.Message.Should().Contain("reloaded");
    }

    private sealed class UpstreamDto
    {
        public string Address { get; set; } = string.Empty;
        public List<string> Routes { get; set; } = [];
    }

    private sealed class MessageResponse
    {
        public string Message { get; set; } = string.Empty;
    }
}
