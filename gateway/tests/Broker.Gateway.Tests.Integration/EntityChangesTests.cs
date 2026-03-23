using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;

namespace Broker.Gateway.Tests.Integration;

public sealed class EntityChangesTests(CustomWebApplicationFactory factory) : IntegrationTestBase(factory)
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    [Fact]
    public async Task GetEntityChanges_ReturnsChangesForType()
    {
        Authenticate();

        // Create an audit entry by saving menu
        var menuConfig = new
        {
            menu = new[]
            {
                new { id = "ec-test-1", label = "EC Test", icon = "Dashboard", path = "/ec-test" }
            }
        };
        var saveResponse = await _client.PutAsJsonAsync("/api/v1/config/menu", menuConfig);
        saveResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Query entity changes for MenuConfig
        var response = await _client.GetAsync("/api/v1/config/entity-changes?entityType=MenuConfig&entityId=config");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<PagedResponse<OperationDto>>(JsonOptions);
        result.Should().NotBeNull();
        result!.TotalCount.Should().BeGreaterThan(0);
        result.Items.Should().NotBeEmpty();
        result.Items[0].EntityDisplayName.Should().Be("Menu Configuration");
    }

    [Fact]
    public async Task GetAllEntityChanges_ReturnsPaginatedResults()
    {
        Authenticate();

        // Create audit entries by performing mutations
        var menuConfig = new
        {
            menu = new[]
            {
                new { id = "all-test-1", label = "All Test", icon = "Settings", path = "/all-test" }
            }
        };
        await _client.PutAsJsonAsync("/api/v1/config/menu", menuConfig);

        var response = await _client.GetAsync("/api/v1/config/entity-changes/all?page=1&pageSize=10");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<PagedResponse<GlobalOperationDto>>(JsonOptions);
        result.Should().NotBeNull();
        result!.Page.Should().Be(1);
        result.PageSize.Should().Be(10);
        result.TotalCount.Should().BeGreaterThan(0);
        result.Items.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetAllEntityChanges_FilterByDate_Works()
    {
        Authenticate();

        // Create an audit entry
        var menuConfig = new
        {
            menu = new[]
            {
                new { id = "date-test", label = "Date Test", icon = "Dashboard", path = "/date-test" }
            }
        };
        await _client.PutAsJsonAsync("/api/v1/config/menu", menuConfig);

        var today = DateTime.UtcNow.ToString("yyyy-MM-dd");
        var response = await _client.GetAsync($"/api/v1/config/entity-changes/all?from={today}&to={today}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<PagedResponse<GlobalOperationDto>>(JsonOptions);
        result.Should().NotBeNull();
        result!.TotalCount.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetAllEntityChanges_FilterByEntityType_Works()
    {
        Authenticate();

        // Create a menu audit entry
        var menuConfig = new
        {
            menu = new[]
            {
                new { id = "type-test", label = "Type Test", icon = "Dashboard", path = "/type-test" }
            }
        };
        await _client.PutAsJsonAsync("/api/v1/config/menu", menuConfig);

        var response = await _client.GetAsync("/api/v1/config/entity-changes/all?entityType=MenuConfig");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<PagedResponse<GlobalOperationDto>>(JsonOptions);
        result.Should().NotBeNull();
        result!.TotalCount.Should().BeGreaterThan(0);
        result.Items.Should().OnlyContain(x => x.EntityType == "MenuConfig");
    }

    [Fact]
    public async Task GetAllEntityChanges_FilterByUserName_Works()
    {
        Authenticate();

        // Create an audit entry (authenticated as "admin")
        var menuConfig = new
        {
            menu = new[]
            {
                new { id = "user-test", label = "User Test", icon = "Dashboard", path = "/user-test" }
            }
        };
        await _client.PutAsJsonAsync("/api/v1/config/menu", menuConfig);

        var response = await _client.GetAsync("/api/v1/config/entity-changes/all?userName=admin");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<PagedResponse<GlobalOperationDto>>(JsonOptions);
        result.Should().NotBeNull();
        result!.TotalCount.Should().BeGreaterThan(0);
        result.Items.Should().OnlyContain(x => x.UserName == "admin");
    }

    [Fact]
    public async Task GetAllEntityChanges_FilterBySearch_Works()
    {
        Authenticate();

        // Create an audit entry
        var menuConfig = new
        {
            menu = new[]
            {
                new { id = "search-test", label = "Search Test", icon = "Dashboard", path = "/search-test" }
            }
        };
        await _client.PutAsJsonAsync("/api/v1/config/menu", menuConfig);

        var response = await _client.GetAsync("/api/v1/config/entity-changes/all?q=Menu");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<PagedResponse<GlobalOperationDto>>(JsonOptions);
        result.Should().NotBeNull();
        result!.TotalCount.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetAllEntityChanges_SortByTimestamp_Works()
    {
        Authenticate();

        // Create two audit entries
        var menu1 = new
        {
            menu = new[]
            {
                new { id = "sort-test-1", label = "Sort Test 1", icon = "Dashboard", path = "/sort-1" }
            }
        };
        await _client.PutAsJsonAsync("/api/v1/config/menu", menu1);

        var menu2 = new
        {
            menu = new[]
            {
                new { id = "sort-test-2", label = "Sort Test 2", icon = "Dashboard", path = "/sort-2" }
            }
        };
        await _client.PutAsJsonAsync("/api/v1/config/menu", menu2);

        var response = await _client.GetAsync("/api/v1/config/entity-changes/all?sort=timestamp asc");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<PagedResponse<GlobalOperationDto>>(JsonOptions);
        result.Should().NotBeNull();
        result!.Items.Count.Should().BeGreaterOrEqualTo(2);

        // Verify ascending order
        for (var i = 1; i < result.Items.Count; i++)
        {
            result.Items[i].Timestamp.Should().BeOnOrAfter(result.Items[i - 1].Timestamp);
        }
    }

    [Fact]
    public async Task GetAllEntityChanges_FilterByChangeType_Works()
    {
        Authenticate();

        // Create an audit entry (this will be "Modified" since initial menu already exists)
        var menuConfig = new
        {
            menu = new[]
            {
                new { id = "change-type-test", label = "Change Type Test", icon = "Dashboard", path = "/ct-test" }
            }
        };
        await _client.PutAsJsonAsync("/api/v1/config/menu", menuConfig);

        var response = await _client.GetAsync("/api/v1/config/entity-changes/all?changeType=Modified");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<PagedResponse<GlobalOperationDto>>(JsonOptions);
        result.Should().NotBeNull();
        result!.TotalCount.Should().BeGreaterThan(0);
        result.Items.Should().OnlyContain(x => x.ChangeType == "Modified");
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

    private sealed class GlobalOperationDto
    {
        public string OperationId { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public Guid? UserId { get; set; }
        public string? UserName { get; set; }
        public string EntityType { get; set; } = string.Empty;
        public string EntityId { get; set; } = string.Empty;
        public string? EntityDisplayName { get; set; }
        public string ChangeType { get; set; } = string.Empty;
        public List<object> Changes { get; set; } = [];
    }
}
