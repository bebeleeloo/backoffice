using System.Net;
using System.Net.Http.Json;
using Broker.Backoffice.Application.Countries;
using Broker.Backoffice.Application.Permissions;
using FluentAssertions;

namespace Broker.Backoffice.Tests.Integration;

public class ReadOnlyEndpointsTests(CustomWebApplicationFactory factory) : IntegrationTestBase(factory)
{

    [Fact]
    public async Task Permissions_ShouldReturnList()
    {
        await AuthenticateAsync();
        var response = await _client.GetAsync("/api/v1/permissions");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<List<PermissionDto>>();
        result.Should().NotBeNull();
        result!.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Countries_ShouldReturnList()
    {
        await AuthenticateAsync();
        var response = await _client.GetAsync("/api/v1/countries");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<List<CountryDto>>();
        result.Should().NotBeNull();
        result!.Should().NotBeEmpty();
    }
}
