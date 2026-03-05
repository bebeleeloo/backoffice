using System.Net.Http.Headers;

namespace Broker.Backoffice.Tests.Integration;

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

    protected Task AuthenticateAsync()
    {
        var token = TestJwtTokenHelper.GenerateAdminToken();
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);
        return Task.CompletedTask;
    }

    protected Task AuthenticateWithPermissions(params string[] permissions)
    {
        var token = TestJwtTokenHelper.GenerateToken(permissions: permissions);
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);
        return Task.CompletedTask;
    }
}
