using System.Net;
using FluentAssertions;

namespace Broker.Backoffice.Tests.Integration;

public class SwaggerTests(CustomWebApplicationFactory factory) : IntegrationTestBase(factory)
{

    [Fact]
    public async Task SwaggerEndpoint_ShouldReturnOk()
    {
        var response = await _client.GetAsync("/swagger/v1/swagger.json");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Broker Backoffice API");
    }
}
