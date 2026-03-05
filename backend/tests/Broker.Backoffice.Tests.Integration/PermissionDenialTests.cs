using System.Net;
using System.Net.Http.Json;
using FluentAssertions;

namespace Broker.Backoffice.Tests.Integration;

public class PermissionDenialTests(CustomWebApplicationFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task UserWithClientsRead_CanListClients()
    {
        await AuthenticateWithPermissions("clients.read");
        var response = await _client.GetAsync("/api/v1/clients?page=1&pageSize=10");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task UserWithClientsRead_CannotCreateClient()
    {
        await AuthenticateWithPermissions("clients.read");
        var response = await _client.PostAsJsonAsync("/api/v1/clients", new
        {
            ClientType = "Individual",
            Status = "Active",
            Email = $"denied_{Guid.NewGuid():N}@test.com",
            PepStatus = false,
            KycStatus = "NotStarted",
            Addresses = new[] { new { Type = "Legal", Line1 = "1 St", City = "City", CountryId = Guid.NewGuid() } },
        });
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task UserWithClientsRead_CanAccessDashboard()
    {
        await AuthenticateWithPermissions("clients.read");
        var response = await _client.GetAsync("/api/v1/dashboard/stats");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task UserWithNoPermissions_CannotListAnything()
    {
        await AuthenticateWithPermissions(); // No permissions

        var clientsResp = await _client.GetAsync("/api/v1/clients?page=1&pageSize=10");
        clientsResp.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var accountsResp = await _client.GetAsync("/api/v1/accounts?page=1&pageSize=10");
        accountsResp.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var instrumentsResp = await _client.GetAsync("/api/v1/instruments?page=1&pageSize=10");
        instrumentsResp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
