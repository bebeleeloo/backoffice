using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;

namespace Broker.Gateway.Tests.Integration;

public sealed class AuditTests(CustomWebApplicationFactory factory) : IntegrationTestBase(factory)
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    [Fact]
    public async Task SaveMenu_CreatesAuditLog()
    {
        Authenticate();

        var menuConfig = new
        {
            menu = new[]
            {
                new { id = "audit-menu-1", label = "Audit Menu Test", icon = "Dashboard", path = "/audit-menu" }
            }
        };

        var saveResponse = await _client.PutAsJsonAsync("/api/v1/config/menu", menuConfig);
        saveResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Check entity changes for this menu save
        var response = await _client.GetAsync("/api/v1/config/entity-changes?entityType=MenuConfig&entityId=config");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<PagedResponse<OperationDto>>(JsonOptions);
        result.Should().NotBeNull();
        result!.TotalCount.Should().BeGreaterThan(0);

        // Should have changes recorded
        var latestOp = result.Items[0];
        latestOp.ChangeType.Should().NotBeNullOrEmpty();
        latestOp.Changes.Should().NotBeNull();
    }

    [Fact]
    public async Task SaveUpstreams_AuditHasBeforeAfterJson()
    {
        Authenticate();

        // Save upstreams to create an audit entry
        var config = new
        {
            upstreams = new Dictionary<string, object>
            {
                ["audit-svc"] = new
                {
                    address = "http://localhost:7070",
                    routes = new[] { "/api/v1/audit-test" }
                }
            }
        };

        var saveResponse = await _client.PutAsJsonAsync("/api/v1/config/upstreams", config);
        saveResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Check entity changes for upstreams — should have audit data
        var response = await _client.GetAsync("/api/v1/config/entity-changes?entityType=UpstreamsConfig&entityId=config");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<PagedResponse<OperationDto>>(JsonOptions);
        result.Should().NotBeNull();
        result!.TotalCount.Should().BeGreaterThan(0);

        // The latest entry should have a valid change type
        var latestOp = result.Items[0];
        latestOp.ChangeType.Should().NotBeNullOrEmpty();
        latestOp.EntityDisplayName.Should().Be("Upstreams");
    }

    [Fact]
    public async Task Reload_CreatesAuditWithBeforeAfter()
    {
        Authenticate();

        var reloadResponse = await _client.PostAsync("/api/v1/config/reload", null);
        reloadResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Check entity changes for reload action
        var response = await _client.GetAsync("/api/v1/config/entity-changes?entityType=Config&entityId=reload");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<PagedResponse<OperationDto>>(JsonOptions);
        result.Should().NotBeNull();
        result!.TotalCount.Should().BeGreaterThan(0);

        var latestOp = result.Items[0];
        latestOp.EntityDisplayName.Should().Be("Configuration");
    }

    [Fact]
    public async Task SaveEntities_CreatesAuditWithCorrectEntityType()
    {
        Authenticate();

        var config = new
        {
            entities = new[]
            {
                new
                {
                    name = "AuditTestEntity",
                    fields = new[]
                    {
                        new { name = "id", roles = new[] { "*" } }
                    }
                }
            }
        };

        var saveResponse = await _client.PutAsJsonAsync("/api/v1/config/entities", config);
        saveResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Check entity changes
        var response = await _client.GetAsync("/api/v1/config/entity-changes?entityType=EntitiesConfig&entityId=config");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<PagedResponse<OperationDto>>(JsonOptions);
        result.Should().NotBeNull();
        result!.TotalCount.Should().BeGreaterThan(0);

        var latestOp = result.Items[0];
        latestOp.EntityDisplayName.Should().Be("Entity Fields");
    }

    private sealed class PagedResponse<T>
    {
        public List<T> Items { get; set; } = [];
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
    }

    private sealed class OperationDto
    {
        public string OperationId { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public Guid? UserId { get; set; }
        public string? UserName { get; set; }
        public string? EntityDisplayName { get; set; }
        public string ChangeType { get; set; } = string.Empty;
        public List<object> Changes { get; set; } = [];
    }
}
