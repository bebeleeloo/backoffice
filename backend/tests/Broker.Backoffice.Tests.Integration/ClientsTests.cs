using System.Net;
using System.Net.Http.Json;
using Broker.Backoffice.Application.Clients;
using Broker.Backoffice.Application.Common;
using FluentAssertions;

namespace Broker.Backoffice.Tests.Integration;

public class ClientsTests(CustomWebApplicationFactory factory) : IntegrationTestBase(factory)
{

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

    [Fact]
    public async Task UpdateClient_ShouldReturnUpdated()
    {
        await AuthenticateAsync();
        var countriesResp = await _client.GetAsync("/api/v1/countries");
        var countries = await countriesResp.Content.ReadFromJsonAsync<List<CountryListItem>>();
        var countryId = countries!.First().Id;

        var createResp = await _client.PostAsJsonAsync("/api/v1/clients", new
        {
            ClientType = "Individual",
            Status = "Active",
            Email = $"upd_{Guid.NewGuid():N}@test.com",
            PepStatus = false,
            KycStatus = "NotStarted",
            FirstName = "Original",
            LastName = "Name",
            Addresses = new[]
            {
                new { Type = "Legal", Line1 = "123 Main St", City = "New York", CountryId = countryId }
            }
        });
        var created = await createResp.Content.ReadFromJsonAsync<ClientDto>();

        var updateResp = await _client.PutAsJsonAsync($"/api/v1/clients/{created!.Id}", new
        {
            Id = created.Id,
            ClientType = "Individual",
            Status = "Active",
            Email = created.Email,
            PepStatus = false,
            KycStatus = "NotStarted",
            FirstName = "Updated",
            LastName = "Name",
            Addresses = new[]
            {
                new { Type = "Legal", Line1 = "456 Oak Ave", City = "Boston", CountryId = countryId }
            },
            RowVersion = created.RowVersion,
        });
        updateResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await updateResp.Content.ReadFromJsonAsync<ClientDto>();
        updated!.FirstName.Should().Be("Updated");
    }

    [Fact]
    public async Task GetClientAccounts_ShouldReturnList()
    {
        await AuthenticateAsync();
        var countriesResp = await _client.GetAsync("/api/v1/countries");
        var countries = await countriesResp.Content.ReadFromJsonAsync<List<CountryListItem>>();
        var countryId = countries!.First().Id;

        var createResp = await _client.PostAsJsonAsync("/api/v1/clients", new
        {
            ClientType = "Individual",
            Status = "Active",
            Email = $"acc_{Guid.NewGuid():N}@test.com",
            PepStatus = false,
            KycStatus = "NotStarted",
            FirstName = "Acc",
            LastName = "Test",
            Addresses = new[]
            {
                new { Type = "Legal", Line1 = "1 St", City = "City", CountryId = countryId }
            }
        });
        var created = await createResp.Content.ReadFromJsonAsync<ClientDto>();

        var response = await _client.GetAsync($"/api/v1/clients/{created!.Id}/accounts");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task CreateClient_DuplicateEmail_ShouldReturn409()
    {
        await AuthenticateAsync();
        var countriesResp = await _client.GetAsync("/api/v1/countries");
        var countries = await countriesResp.Content.ReadFromJsonAsync<List<CountryListItem>>();
        var countryId = countries!.First().Id;
        var email = $"dup_{Guid.NewGuid():N}@test.com";

        await _client.PostAsJsonAsync("/api/v1/clients", new
        {
            ClientType = "Individual",
            Status = "Active",
            Email = email,
            PepStatus = false,
            KycStatus = "NotStarted",
            FirstName = "First",
            LastName = "Client",
            Addresses = new[]
            {
                new { Type = "Legal", Line1 = "1 St", City = "City", CountryId = countryId }
            }
        });

        var response = await _client.PostAsJsonAsync("/api/v1/clients", new
        {
            ClientType = "Individual",
            Status = "Active",
            Email = email, // duplicate
            PepStatus = false,
            KycStatus = "NotStarted",
            FirstName = "Second",
            LastName = "Client",
            Addresses = new[]
            {
                new { Type = "Legal", Line1 = "2 St", City = "City", CountryId = countryId }
            }
        });
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task DeleteClient_LinkedToAccount_ShouldReturn409()
    {
        await AuthenticateAsync();
        var countriesResp = await _client.GetAsync("/api/v1/countries");
        var countries = await countriesResp.Content.ReadFromJsonAsync<List<CountryListItem>>();
        var countryId = countries!.First().Id;

        // Create client
        var clientResp = await _client.PostAsJsonAsync("/api/v1/clients", new
        {
            ClientType = "Individual",
            Status = "Active",
            Email = $"linked_{Guid.NewGuid():N}@test.com",
            PepStatus = false,
            KycStatus = "NotStarted",
            FirstName = "Linked",
            LastName = "Client",
            Addresses = new[]
            {
                new { Type = "Legal", Line1 = "1 St", City = "City", CountryId = countryId }
            }
        });
        var client = await clientResp.Content.ReadFromJsonAsync<ClientDto>();

        // Create account
        var accountResp = await _client.PostAsJsonAsync("/api/v1/accounts", new
        {
            Number = $"LNK-{Guid.NewGuid():N}"[..20],
            Status = "Active",
            AccountType = "Individual",
            MarginType = "Cash",
            OptionLevel = "Level0",
            Tariff = "Basic",
        });
        var account = await accountResp.Content.ReadFromJsonAsync<AccountListItem>();

        // Link client to account via SetHolders
        var holdersResp = await _client.PutAsJsonAsync($"/api/v1/accounts/{account!.Id}/holders",
            new[] { new { ClientId = client!.Id, Role = "Owner", IsPrimary = true } });
        holdersResp.EnsureSuccessStatusCode();

        // Try to delete linked client — should fail
        var response = await _client.DeleteAsync($"/api/v1/clients/{client.Id}");
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task UpdateClient_RouteBodyIdMismatch_ShouldReturn400()
    {
        await AuthenticateAsync();
        var countriesResp = await _client.GetAsync("/api/v1/countries");
        var countries = await countriesResp.Content.ReadFromJsonAsync<List<CountryListItem>>();
        var countryId = countries!.First().Id;

        var createResp = await _client.PostAsJsonAsync("/api/v1/clients", new
        {
            ClientType = "Individual",
            Status = "Active",
            Email = $"mismatch_{Guid.NewGuid():N}@test.com",
            PepStatus = false,
            KycStatus = "NotStarted",
            FirstName = "Test",
            LastName = "Mismatch",
            Addresses = new[]
            {
                new { Type = "Legal", Line1 = "1 St", City = "City", CountryId = countryId }
            }
        });
        var created = await createResp.Content.ReadFromJsonAsync<ClientDto>();

        var response = await _client.PutAsJsonAsync($"/api/v1/clients/{created!.Id}", new
        {
            Id = Guid.NewGuid(), // different from route ID
            ClientType = "Individual",
            Status = "Active",
            Email = created.Email,
            PepStatus = false,
            KycStatus = "NotStarted",
            FirstName = "Updated",
            LastName = "Mismatch",
            Addresses = new[]
            {
                new { Type = "Legal", Line1 = "1 St", City = "City", CountryId = countryId }
            },
            RowVersion = created.RowVersion,
        });
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateClient_StaleRowVersion_ShouldReturn409()
    {
        await AuthenticateAsync();
        var countriesResp = await _client.GetAsync("/api/v1/countries");
        var countries = await countriesResp.Content.ReadFromJsonAsync<List<CountryListItem>>();
        var countryId = countries!.First().Id;

        var createResp = await _client.PostAsJsonAsync("/api/v1/clients", new
        {
            ClientType = "Individual",
            Status = "Active",
            Email = $"conc_{Guid.NewGuid():N}@test.com",
            PepStatus = false,
            KycStatus = "NotStarted",
            FirstName = "Conc",
            LastName = "Test",
            Addresses = new[]
            {
                new { Type = "Legal", Line1 = "1 St", City = "City", CountryId = countryId }
            }
        });
        var created = await createResp.Content.ReadFromJsonAsync<ClientDto>();
        var staleRowVersion = created!.RowVersion;

        // First update succeeds — changes RowVersion
        await _client.PutAsJsonAsync($"/api/v1/clients/{created.Id}", new
        {
            Id = created.Id,
            ClientType = "Individual",
            Status = "Active",
            Email = created.Email,
            PepStatus = false,
            KycStatus = "NotStarted",
            FirstName = "Updated",
            LastName = "Test",
            Addresses = new[]
            {
                new { Type = "Legal", Line1 = "1 St", City = "City", CountryId = countryId }
            },
            RowVersion = staleRowVersion,
        });

        // Second update with stale RowVersion — should fail
        var response = await _client.PutAsJsonAsync($"/api/v1/clients/{created.Id}", new
        {
            Id = created.Id,
            ClientType = "Individual",
            Status = "Active",
            Email = created.Email,
            PepStatus = false,
            KycStatus = "NotStarted",
            FirstName = "Should Fail",
            LastName = "Test",
            Addresses = new[]
            {
                new { Type = "Legal", Line1 = "1 St", City = "City", CountryId = countryId }
            },
            RowVersion = staleRowVersion,
        });
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task ListClients_WithFilters_ShouldReturnFiltered()
    {
        await AuthenticateAsync();
        var countriesResp = await _client.GetAsync("/api/v1/countries");
        var countries = await countriesResp.Content.ReadFromJsonAsync<List<CountryListItem>>();
        var countryId = countries!.First().Id;

        // Create a client to ensure at least one result
        await _client.PostAsJsonAsync("/api/v1/clients", new
        {
            ClientType = "Individual",
            Status = "Active",
            Email = $"flt_{Guid.NewGuid():N}@test.com",
            PepStatus = false,
            KycStatus = "NotStarted",
            FirstName = "FilterTest",
            LastName = "Client",
            Addresses = new[]
            {
                new { Type = "Legal", Line1 = "1 St", City = "City", CountryId = countryId }
            }
        });

        var response = await _client.GetAsync(
            "/api/v1/clients?page=1&pageSize=10&status=Active&clientType=Individual&pepStatus=false&q=FilterTest");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PagedResult<ClientListItemDto>>();
        result.Should().NotBeNull();
        result!.Items.Should().NotBeEmpty();
    }

    [Fact]
    public async Task ListClients_WithDateFilter_ShouldReturn200()
    {
        await AuthenticateAsync();
        var today = DateTime.UtcNow.ToString("yyyy-MM-dd");
        var response = await _client.GetAsync(
            $"/api/v1/clients?page=1&pageSize=10&createdFrom={today}&createdTo={today}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ListClients_SortByDisplayName_ShouldReturn200()
    {
        await AuthenticateAsync();
        var response = await _client.GetAsync(
            "/api/v1/clients?page=1&pageSize=10&sort=displayName asc");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PagedResult<ClientListItemDto>>();
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task SetClientAccounts_ShouldReturnLinked()
    {
        await AuthenticateAsync();
        var countriesResp = await _client.GetAsync("/api/v1/countries");
        var countries = await countriesResp.Content.ReadFromJsonAsync<List<CountryListItem>>();
        var countryId = countries!.First().Id;

        // Create client
        var clientResp = await _client.PostAsJsonAsync("/api/v1/clients", new
        {
            ClientType = "Individual",
            Status = "Active",
            Email = $"setacc_{Guid.NewGuid():N}@test.com",
            PepStatus = false,
            KycStatus = "NotStarted",
            FirstName = "SetAcc",
            LastName = "Test",
            Addresses = new[]
            {
                new { Type = "Legal", Line1 = "1 St", City = "City", CountryId = countryId }
            }
        });
        var client = await clientResp.Content.ReadFromJsonAsync<ClientDto>();

        // Create two accounts
        var acc1Resp = await _client.PostAsJsonAsync("/api/v1/accounts", new
        {
            Number = $"SA1-{Guid.NewGuid():N}"[..20],
            Status = "Active",
            AccountType = "Individual",
            MarginType = "Cash",
            OptionLevel = "Level0",
            Tariff = "Basic",
        });
        var acc1 = await acc1Resp.Content.ReadFromJsonAsync<AccountListItem>();
        var acc2Resp = await _client.PostAsJsonAsync("/api/v1/accounts", new
        {
            Number = $"SA2-{Guid.NewGuid():N}"[..20],
            Status = "Active",
            AccountType = "Individual",
            MarginType = "Cash",
            OptionLevel = "Level0",
            Tariff = "Basic",
        });
        var acc2 = await acc2Resp.Content.ReadFromJsonAsync<AccountListItem>();

        // Set accounts
        var response = await _client.PutAsJsonAsync($"/api/v1/clients/{client!.Id}/accounts",
            new[]
            {
                new { AccountId = acc1!.Id, Role = "Owner", IsPrimary = true },
                new { AccountId = acc2!.Id, Role = "Beneficiary", IsPrimary = false },
            });
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task SetClientAccounts_InvalidAccountId_ShouldReturn409()
    {
        await AuthenticateAsync();
        var countriesResp = await _client.GetAsync("/api/v1/countries");
        var countries = await countriesResp.Content.ReadFromJsonAsync<List<CountryListItem>>();
        var countryId = countries!.First().Id;

        var clientResp = await _client.PostAsJsonAsync("/api/v1/clients", new
        {
            ClientType = "Individual",
            Status = "Active",
            Email = $"invsa_{Guid.NewGuid():N}@test.com",
            PepStatus = false,
            KycStatus = "NotStarted",
            FirstName = "InvSA",
            LastName = "Test",
            Addresses = new[]
            {
                new { Type = "Legal", Line1 = "1 St", City = "City", CountryId = countryId }
            }
        });
        var client = await clientResp.Content.ReadFromJsonAsync<ClientDto>();

        var response = await _client.PutAsJsonAsync($"/api/v1/clients/{client!.Id}/accounts",
            new[] { new { AccountId = Guid.NewGuid(), Role = "Owner", IsPrimary = true } });
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    // Lightweight DTOs for deserialization
    private record CountryListItem(Guid Id, string Iso2, string Name);
    private record AccountListItem(Guid Id, string Number);
}
