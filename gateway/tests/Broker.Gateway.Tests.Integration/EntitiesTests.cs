using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;

namespace Broker.Gateway.Tests.Integration;

public sealed class EntitiesTests(CustomWebApplicationFactory factory) : IntegrationTestBase(factory)
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    [Fact]
    public async Task GetEntities_WithRoleClaim_ReturnsFilteredEntities()
    {
        Authenticate();

        // Set up known entities config first
        var setupConfig = new
        {
            entities = new[]
            {
                new
                {
                    name = "Client",
                    fields = new[]
                    {
                        new { name = "id", roles = new[] { "*" } },
                        new { name = "ssn", roles = new[] { "Manager" } },
                        new { name = "email", roles = new[] { "Manager", "Operator" } }
                    }
                }
            }
        };
        var setupResponse = await _client.PutAsJsonAsync("/api/v1/config/entities", setupConfig);
        setupResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Now test with Manager role
        AuthenticateWithPermissions(
            permissions: new[] { "clients.read" },
            roles: new[] { "Manager" });

        var response = await _client.GetAsync("/api/v1/config/entities");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadAsStringAsync();
        var entities = JsonSerializer.Deserialize<List<EntityDto>>(json, JsonOptions);

        entities.Should().NotBeNull();
        entities!.Count.Should().BeGreaterThan(0);
        entities.Should().Contain(x => x.Name == "Client");
    }

    [Fact]
    public async Task GetEntitiesRaw_WithSettingsManage_ReturnsAll()
    {
        Authenticate();

        // Set up known config first
        var setupConfig = new
        {
            entities = new[]
            {
                new
                {
                    name = "TestRawEntity",
                    fields = new[]
                    {
                        new { name = "id", roles = new[] { "*" } }
                    }
                }
            }
        };
        var setupResponse = await _client.PutAsJsonAsync("/api/v1/config/entities", setupConfig);
        setupResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var response = await _client.GetAsync("/api/v1/config/entities/raw");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadAsStringAsync();
        var entities = JsonSerializer.Deserialize<List<EntityDto>>(json, JsonOptions);

        entities.Should().NotBeNull();
        entities!.Count.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task SaveEntities_ValidConfig_Returns200()
    {
        Authenticate();

        var config = new
        {
            entities = new[]
            {
                new
                {
                    name = "TestEntity",
                    fields = new[]
                    {
                        new { name = "id", roles = new[] { "*" } },
                        new { name = "name", roles = new[] { "Manager" } }
                    }
                }
            }
        };

        var response = await _client.PutAsJsonAsync("/api/v1/config/entities", config);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<MessageResponse>(JsonOptions);
        body.Should().NotBeNull();
        body!.Message.Should().Contain("saved");
    }

    [Fact]
    public async Task SaveEntities_NullEntities_Returns400()
    {
        Authenticate();

        // Send a request where the JSON body causes EntitiesConfig.Entities to be null.
        // Since EntitiesConfig initializes Entities = [], sending {} won't make it null.
        // We need to explicitly send entities: null.
        var json = """{"entities": null}""";
        var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

        var response = await _client.PutAsync("/api/v1/config/entities", content);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetEntity_ByName_ReturnsEntity()
    {
        Authenticate();

        // Set up known entities config with "Client" entity
        var setupConfig = new
        {
            entities = new[]
            {
                new
                {
                    name = "Client",
                    fields = new[]
                    {
                        new { name = "id", roles = new[] { "*" } },
                        new { name = "email", roles = new[] { "Manager" } }
                    }
                }
            }
        };
        var setupResponse = await _client.PutAsJsonAsync("/api/v1/config/entities", setupConfig);
        setupResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Admin token includes Manager role, which sees all fields
        var response = await _client.GetAsync("/api/v1/config/entities/Client");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var entityJson = await response.Content.ReadAsStringAsync();
        var entity = JsonSerializer.Deserialize<EntityDto>(entityJson, JsonOptions);

        entity.Should().NotBeNull();
        entity!.Name.Should().Be("Client");
        entity.Fields.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetEntity_NonExistent_Returns404()
    {
        Authenticate();

        var response = await _client.GetAsync("/api/v1/config/entities/NonexistentEntity");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private sealed class EntityDto
    {
        public string Name { get; set; } = string.Empty;
        public List<FieldDto> Fields { get; set; } = [];
    }

    private sealed class FieldDto
    {
        public string Name { get; set; } = string.Empty;
        public List<string> Roles { get; set; } = [];
    }

    private sealed class MessageResponse
    {
        public string Message { get; set; } = string.Empty;
    }
}
