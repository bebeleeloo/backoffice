using System.Net;
using FluentAssertions;

namespace Broker.Backoffice.Tests.Integration;

public class HealthCheckTests(CustomWebApplicationFactory factory) : IntegrationTestBase(factory)
{

    [Fact]
    public async Task LiveEndpoint_ShouldReturnOk()
    {
        var response = await _client.GetAsync("/health/live");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ReadyEndpoint_ShouldReturnOk()
    {
        var response = await _client.GetAsync("/health/ready");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
