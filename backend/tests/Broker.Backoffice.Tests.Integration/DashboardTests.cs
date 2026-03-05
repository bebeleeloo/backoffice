using System.Net;
using System.Net.Http.Json;
using Broker.Backoffice.Application.Dashboard;
using FluentAssertions;

namespace Broker.Backoffice.Tests.Integration;

public class DashboardTests(CustomWebApplicationFactory factory) : IntegrationTestBase(factory)
{

    [Fact]
    public async Task Stats_Authenticated_ShouldReturn200()
    {
        await AuthenticateAsync();
        var response = await _client.GetAsync("/api/v1/dashboard/stats");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var stats = await response.Content.ReadFromJsonAsync<DashboardStatsDto>();
        stats.Should().NotBeNull();
        stats!.TotalUsers.Should().Be(5);
    }

    [Fact]
    public async Task Stats_Unauthenticated_ShouldReturn401()
    {
        var response = await _client.GetAsync("/api/v1/dashboard/stats");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
