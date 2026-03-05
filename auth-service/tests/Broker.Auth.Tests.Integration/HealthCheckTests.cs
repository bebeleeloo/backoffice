namespace Broker.Auth.Tests.Integration;

public class HealthCheckTests(CustomWebApplicationFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task LiveEndpoint_ShouldReturn200()
    {
        var resp = await _client.GetAsync("/health/live");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ReadyEndpoint_ShouldReturn200()
    {
        var resp = await _client.GetAsync("/health/ready");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
