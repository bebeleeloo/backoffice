using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Broker.Backoffice.Application.Auth;
using Broker.Backoffice.Application.Clients;
using Broker.Backoffice.Application.Common;
using FluentAssertions;

namespace Broker.Backoffice.Tests.Integration;

[Collection("Integration")]
public class ClientsTests(CustomWebApplicationFactory factory)
{
    private readonly HttpClient _client = factory.CreateClient();

    private async Task AuthenticateAsync()
    {
        var loginResp = await _client.PostAsJsonAsync("/api/v1/auth/login",
            new { Username = "admin", Password = "Admin123!" });
        var auth = await loginResp.Content.ReadFromJsonAsync<AuthResponse>();
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", auth!.AccessToken);
    }

    [Fact]
    public async Task ListClients_Authenticated_ShouldReturnPaged()
    {
        await AuthenticateAsync();
        var response = await _client.GetAsync("/api/v1/clients?page=1&pageSize=10");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PagedResult<ClientListItemDto>>();
        result.Should().NotBeNull();
        result!.Page.Should().Be(1);
    }

    [Fact]
    public async Task ListClients_Unauthenticated_ShouldReturn401()
    {
        var response = await _client.GetAsync("/api/v1/clients");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateClient_Individual_ShouldReturnCreated()
    {
        await AuthenticateAsync();

        // need a country ID for address
        var countriesResp = await _client.GetAsync("/api/v1/countries");
        var countries = await countriesResp.Content.ReadFromJsonAsync<List<CountryListItem>>();
        var countryId = countries!.First().Id;

        var response = await _client.PostAsJsonAsync("/api/v1/clients", new
        {
            ClientType = "Individual",
            Status = "Active",
            Email = $"test_{Guid.NewGuid():N}@test.com",
            PepStatus = false,
            KycStatus = "NotStarted",
            FirstName = "Test",
            LastName = "User",
            Addresses = new[]
            {
                new { Type = "Legal", Line1 = "123 Main St", City = "New York", CountryId = countryId }
            }
        });
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var client = await response.Content.ReadFromJsonAsync<ClientDto>();
        client!.FirstName.Should().Be("Test");
        client.LastName.Should().Be("User");
    }

    [Fact]
    public async Task CreateClient_Corporate_ShouldReturnCreated()
    {
        await AuthenticateAsync();
        var countriesResp = await _client.GetAsync("/api/v1/countries");
        var countries = await countriesResp.Content.ReadFromJsonAsync<List<CountryListItem>>();
        var countryId = countries!.First().Id;

        var response = await _client.PostAsJsonAsync("/api/v1/clients", new
        {
            ClientType = "Corporate",
            Status = "Active",
            Email = $"corp_{Guid.NewGuid():N}@test.com",
            PepStatus = false,
            KycStatus = "NotStarted",
            CompanyName = "Acme Test Corp",
            Addresses = new[]
            {
                new { Type = "Legal", Line1 = "456 Oak Ave", City = "London", CountryId = countryId }
            }
        });
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var client = await response.Content.ReadFromJsonAsync<ClientDto>();
        client!.CompanyName.Should().Be("Acme Test Corp");
    }

    [Fact]
    public async Task GetClient_NotFound_ShouldReturn404()
    {
        await AuthenticateAsync();
        var response = await _client.GetAsync($"/api/v1/clients/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateClient_InvalidEmail_ShouldReturn400()
    {
        await AuthenticateAsync();
        var response = await _client.PostAsJsonAsync("/api/v1/clients", new
        {
            ClientType = "Individual",
            Status = "Active",
            Email = "not-valid",
            PepStatus = false,
            KycStatus = "NotStarted",
            Addresses = new[] { new { Type = "Legal", Line1 = "x", City = "x", CountryId = Guid.NewGuid() } }
        });
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateClient_NoAddresses_ShouldReturn400()
    {
        await AuthenticateAsync();
        var response = await _client.PostAsJsonAsync("/api/v1/clients", new
        {
            ClientType = "Individual",
            Status = "Active",
            Email = $"test_{Guid.NewGuid():N}@test.com",
            PepStatus = false,
            KycStatus = "NotStarted",
            Addresses = Array.Empty<object>()
        });
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateAndDeleteClient_ShouldWork()
    {
        await AuthenticateAsync();
        var countriesResp = await _client.GetAsync("/api/v1/countries");
        var countries = await countriesResp.Content.ReadFromJsonAsync<List<CountryListItem>>();
        var countryId = countries!.First().Id;

        var createResp = await _client.PostAsJsonAsync("/api/v1/clients", new
        {
            ClientType = "Individual",
            Status = "Active",
            Email = $"del_{Guid.NewGuid():N}@test.com",
            PepStatus = false,
            KycStatus = "NotStarted",
            FirstName = "Delete",
            LastName = "Me",
            Addresses = new[]
            {
                new { Type = "Legal", Line1 = "1 St", City = "City", CountryId = countryId }
            }
        });
        var created = await createResp.Content.ReadFromJsonAsync<ClientDto>();

        var delResp = await _client.DeleteAsync($"/api/v1/clients/{created!.Id}");
        delResp.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var getResp = await _client.GetAsync($"/api/v1/clients/{created.Id}");
        getResp.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // Lightweight DTO for countries endpoint
    private record CountryListItem(Guid Id, string Iso2, string Name);
}
