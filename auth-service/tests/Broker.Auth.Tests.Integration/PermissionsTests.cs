namespace Broker.Auth.Tests.Integration;

public class PermissionsTests(CustomWebApplicationFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task ListPermissions_ShouldReturnAll()
    {
        await AuthenticateAsync();
        var resp = await _client.GetAsync("/api/v1/permissions");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
