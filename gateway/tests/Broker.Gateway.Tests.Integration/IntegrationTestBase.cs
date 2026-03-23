using System.Net.Http.Headers;

namespace Broker.Gateway.Tests.Integration;

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

    protected void Authenticate()
    {
        var token = TestJwtTokenHelper.GenerateAdminToken();
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);
    }

    protected void AuthenticateWithPermissions(
        IEnumerable<string> permissions,
        IEnumerable<string>? roles = null)
    {
        var token = TestJwtTokenHelper.GenerateToken(
            permissions: permissions,
            roles: roles);
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);
    }
}
