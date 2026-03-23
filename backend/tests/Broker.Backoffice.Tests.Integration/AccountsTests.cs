using System.Net;
using System.Net.Http.Json;
using Broker.Backoffice.Application.Accounts;
using Broker.Backoffice.Application.Common;
using Broker.Backoffice.Domain.Accounts;
using FluentAssertions;

namespace Broker.Backoffice.Tests.Integration;

public class AccountsTests(CustomWebApplicationFactory factory) : IntegrationTestBase(factory)
{

    [Fact]
    public async Task ListAccounts_Authenticated_ShouldReturnPaged()
    {
        await AuthenticateAsync();
        var response = await _client.GetAsync("/api/v1/accounts?page=1&pageSize=10");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PagedResult<AccountListItemDto>>();
        result.Should().NotBeNull();
        result!.Page.Should().Be(1);
    }

    [Fact]
    public async Task ListAccounts_Unauthenticated_ShouldReturn401()
    {
        var response = await _client.GetAsync("/api/v1/accounts");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateAccount_ShouldReturnCreated()
    {
        await AuthenticateAsync();
        var response = await _client.PostAsJsonAsync("/api/v1/accounts", new
        {
            Number = $"ACC-{Guid.NewGuid():N}"[..20],
            Status = "Active",
            AccountType = "Individual",
            MarginType = "Cash",
            OptionLevel = "Level0",
            Tariff = "Basic",
        });
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var account = await response.Content.ReadFromJsonAsync<AccountDto>();
        account!.Number.Should().NotBeNullOrEmpty();
        account.Status.Should().Be(AccountStatus.Active);
    }

    [Fact]
    public async Task CreateAccount_DuplicateNumber_ShouldReturn409()
    {
        await AuthenticateAsync();
        var number = $"DUP-{Guid.NewGuid():N}"[..20];
        await _client.PostAsJsonAsync("/api/v1/accounts", new
        {
            Number = number, Status = "Active", AccountType = "Individual",
            MarginType = "Cash", OptionLevel = "Level0", Tariff = "Basic",
        });
        var response = await _client.PostAsJsonAsync("/api/v1/accounts", new
        {
            Number = number, Status = "Active", AccountType = "Individual",
            MarginType = "Cash", OptionLevel = "Level0", Tariff = "Basic",
        });
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task GetAccount_NotFound_ShouldReturn404()
    {
        await AuthenticateAsync();
        var response = await _client.GetAsync($"/api/v1/accounts/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateAndGetAccount_ShouldReturnSameData()
    {
        await AuthenticateAsync();
        var number = $"GET-{Guid.NewGuid():N}"[..20];
        var createResp = await _client.PostAsJsonAsync("/api/v1/accounts", new
        {
            Number = number, Status = "Active", AccountType = "Corporate",
            MarginType = "MarginX2", OptionLevel = "Level1", Tariff = "Premium",
            Comment = "test comment",
        });
        var created = await createResp.Content.ReadFromJsonAsync<AccountDto>();

        var getResp = await _client.GetAsync($"/api/v1/accounts/{created!.Id}");
        getResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var fetched = await getResp.Content.ReadFromJsonAsync<AccountDto>();
        fetched!.Number.Should().Be(number);
        fetched.AccountType.Should().Be(AccountType.Corporate);
        fetched.Comment.Should().Be("test comment");
    }

    [Fact]
    public async Task CreateAndDeleteAccount_ShouldWork()
    {
        await AuthenticateAsync();
        var createResp = await _client.PostAsJsonAsync("/api/v1/accounts", new
        {
            Number = $"DEL-{Guid.NewGuid():N}"[..20],
            Status = "Active", AccountType = "Individual",
            MarginType = "Cash", OptionLevel = "Level0", Tariff = "Basic",
        });
        var account = await createResp.Content.ReadFromJsonAsync<AccountDto>();

        var delResp = await _client.DeleteAsync($"/api/v1/accounts/{account!.Id}");
        delResp.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var getResp = await _client.GetAsync($"/api/v1/accounts/{account.Id}");
        getResp.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateAccount_InvalidData_ShouldReturn400()
    {
        await AuthenticateAsync();
        var response = await _client.PostAsJsonAsync("/api/v1/accounts", new
        {
            Number = "", // empty — invalid
            Status = "Active", AccountType = "Individual",
            MarginType = "Cash", OptionLevel = "Level0", Tariff = "Basic",
        });
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateAccount_ShouldReturnUpdated()
    {
        await AuthenticateAsync();
        var number = $"UPD-{Guid.NewGuid():N}"[..20];
        var createResp = await _client.PostAsJsonAsync("/api/v1/accounts", new
        {
            Number = number,
            Status = "Active",
            AccountType = "Individual",
            MarginType = "Cash",
            OptionLevel = "Level0",
            Tariff = "Basic",
            Comment = "Original",
        });
        var created = await createResp.Content.ReadFromJsonAsync<AccountDto>();

        var updateResp = await _client.PutAsJsonAsync($"/api/v1/accounts/{created!.Id}", new
        {
            Id = created.Id,
            Number = number,
            Status = "Active",
            AccountType = "Individual",
            MarginType = "MarginX2",
            OptionLevel = "Level1",
            Tariff = "Premium",
            Comment = "Updated",
            RowVersion = created.RowVersion,
        });
        updateResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await updateResp.Content.ReadFromJsonAsync<AccountDto>();
        updated!.MarginType.Should().Be(MarginType.MarginX2);
        updated.OptionLevel.Should().Be(OptionLevel.Level1);
        updated.Comment.Should().Be("Updated");
    }

    [Fact]
    public async Task UpdateAccount_DuplicateNumber_ShouldReturn409()
    {
        await AuthenticateAsync();
        var number1 = $"DN1-{Guid.NewGuid():N}"[..20];
        var number2 = $"DN2-{Guid.NewGuid():N}"[..20];

        // Create two accounts with different numbers
        await _client.PostAsJsonAsync("/api/v1/accounts", new
        {
            Number = number1, Status = "Active", AccountType = "Individual",
            MarginType = "Cash", OptionLevel = "Level0", Tariff = "Basic",
        });
        var create2Resp = await _client.PostAsJsonAsync("/api/v1/accounts", new
        {
            Number = number2, Status = "Active", AccountType = "Individual",
            MarginType = "Cash", OptionLevel = "Level0", Tariff = "Basic",
        });
        var account2 = await create2Resp.Content.ReadFromJsonAsync<AccountDto>();

        // Try to update account2's number to match account1's number
        var response = await _client.PutAsJsonAsync($"/api/v1/accounts/{account2!.Id}", new
        {
            Id = account2.Id,
            Number = number1, // duplicate
            Status = "Active",
            AccountType = "Individual",
            MarginType = "Cash",
            OptionLevel = "Level0",
            Tariff = "Basic",
            RowVersion = account2.RowVersion,
        });
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task UpdateAccount_RouteBodyIdMismatch_ShouldReturn400()
    {
        await AuthenticateAsync();
        var createResp = await _client.PostAsJsonAsync("/api/v1/accounts", new
        {
            Number = $"MIS-{Guid.NewGuid():N}"[..20],
            Status = "Active",
            AccountType = "Individual",
            MarginType = "Cash",
            OptionLevel = "Level0",
            Tariff = "Basic",
        });
        var created = await createResp.Content.ReadFromJsonAsync<AccountDto>();

        var response = await _client.PutAsJsonAsync($"/api/v1/accounts/{created!.Id}", new
        {
            Id = Guid.NewGuid(), // different from route ID
            Number = created.Number,
            Status = "Active",
            AccountType = "Individual",
            MarginType = "Cash",
            OptionLevel = "Level0",
            Tariff = "Basic",
            RowVersion = created.RowVersion,
        });
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ListAccounts_WithFilters_ShouldReturnFiltered()
    {
        await AuthenticateAsync();

        // Create an account to search for
        var number = $"FLT-{Guid.NewGuid():N}"[..20];
        await _client.PostAsJsonAsync("/api/v1/accounts", new
        {
            Number = number,
            Status = "Active",
            AccountType = "Individual",
            MarginType = "Cash",
            OptionLevel = "Level0",
            Tariff = "Basic",
        });

        var response = await _client.GetAsync(
            $"/api/v1/accounts?page=1&pageSize=10&status=Active&accountType=Individual&q={number[..8]}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PagedResult<AccountListItemDto>>();
        result.Should().NotBeNull();
        result!.Items.Should().NotBeEmpty();
    }

    [Fact]
    public async Task ListAccounts_SortByClearerName_ShouldReturn200()
    {
        await AuthenticateAsync();
        var response = await _client.GetAsync(
            "/api/v1/accounts?page=1&pageSize=10&sort=clearerName asc");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PagedResult<AccountListItemDto>>();
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task SetAccountHolders_ShouldReturnUpdated()
    {
        await AuthenticateAsync();

        // Create a client
        var countriesResp = await _client.GetAsync("/api/v1/countries");
        var countries = await countriesResp.Content.ReadFromJsonAsync<List<CountryListItem>>();
        var countryId = countries!.First().Id;

        var clientResp = await _client.PostAsJsonAsync("/api/v1/clients", new
        {
            ClientType = "Individual",
            Status = "Active",
            Email = $"holder_{Guid.NewGuid():N}@test.com",
            PepStatus = false,
            KycStatus = "NotStarted",
            FirstName = "Holder",
            LastName = "Test",
            Addresses = new[]
            {
                new { Type = "Legal", Line1 = "1 St", City = "City", CountryId = countryId }
            }
        });
        var client = await clientResp.Content.ReadFromJsonAsync<ClientItem>();

        // Create an account
        var accountResp = await _client.PostAsJsonAsync("/api/v1/accounts", new
        {
            Number = $"HLD-{Guid.NewGuid():N}"[..20],
            Status = "Active",
            AccountType = "Individual",
            MarginType = "Cash",
            OptionLevel = "Level0",
            Tariff = "Basic",
        });
        var account = await accountResp.Content.ReadFromJsonAsync<AccountDto>();

        // Set holders
        var response = await _client.PutAsJsonAsync($"/api/v1/accounts/{account!.Id}/holders",
            new[] { new { ClientId = client!.Id, Role = "Owner", IsPrimary = true } });
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await response.Content.ReadFromJsonAsync<AccountDto>();
        updated!.Holders.Should().NotBeEmpty();
    }

    [Fact]
    public async Task SetAccountHolders_InvalidClientId_ShouldReturn409()
    {
        await AuthenticateAsync();

        var accountResp = await _client.PostAsJsonAsync("/api/v1/accounts", new
        {
            Number = $"INV-{Guid.NewGuid():N}"[..20],
            Status = "Active",
            AccountType = "Individual",
            MarginType = "Cash",
            OptionLevel = "Level0",
            Tariff = "Basic",
        });
        var account = await accountResp.Content.ReadFromJsonAsync<AccountDto>();

        var response = await _client.PutAsJsonAsync($"/api/v1/accounts/{account!.Id}/holders",
            new[] { new { ClientId = Guid.NewGuid(), Role = "Owner", IsPrimary = true } });
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task UpdateAccount_StaleRowVersion_ShouldReturn409()
    {
        await AuthenticateAsync();
        var number = $"SRV-{Guid.NewGuid():N}"[..20];
        var createResp = await _client.PostAsJsonAsync("/api/v1/accounts", new
        {
            Number = number,
            Status = "Active",
            AccountType = "Individual",
            MarginType = "Cash",
            OptionLevel = "Level0",
            Tariff = "Basic",
        });
        var created = await createResp.Content.ReadFromJsonAsync<AccountDto>();
        var staleRowVersion = created!.RowVersion;

        // First update succeeds — changes RowVersion
        var update1 = await _client.PutAsJsonAsync($"/api/v1/accounts/{created.Id}", new
        {
            Id = created.Id,
            Number = number,
            Status = "Active",
            AccountType = "Individual",
            MarginType = "MarginX2",
            OptionLevel = "Level0",
            Tariff = "Basic",
            RowVersion = staleRowVersion,
        });
        update1.StatusCode.Should().Be(HttpStatusCode.OK);

        // Second update with stale RowVersion — should fail
        var response = await _client.PutAsJsonAsync($"/api/v1/accounts/{created.Id}", new
        {
            Id = created.Id,
            Number = number,
            Status = "Active",
            AccountType = "Individual",
            MarginType = "Cash",
            OptionLevel = "Level1",
            Tariff = "Premium",
            RowVersion = staleRowVersion,
        });
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    // Lightweight DTOs for deserialization
    private record CountryListItem(Guid Id, string Iso2, string Name);
    private record ClientItem(Guid Id, string Email);
}
