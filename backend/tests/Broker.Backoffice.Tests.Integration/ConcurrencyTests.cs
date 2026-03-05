using System.Net;
using System.Net.Http.Json;
using Broker.Backoffice.Application.Accounts;
using Broker.Backoffice.Application.Clients;
using Broker.Backoffice.Application.Instruments;
using Broker.Backoffice.Application.Orders.TradeOrders;
using FluentAssertions;

namespace Broker.Backoffice.Tests.Integration;

public class ConcurrencyTests(CustomWebApplicationFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task UpdateAccount_WithStaleRowVersion_ShouldReturn409()
    {
        await AuthenticateAsync();

        var number = $"CONC-{Guid.NewGuid():N}"[..20];
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
        var update2 = await _client.PutAsJsonAsync($"/api/v1/accounts/{created.Id}", new
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
        update2.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task UpdateInstrument_WithStaleRowVersion_ShouldReturn409()
    {
        await AuthenticateAsync();

        var symbol = $"CC-{Guid.NewGuid():N}"[..10];
        var createResp = await _client.PostAsJsonAsync("/api/v1/instruments", new
        {
            Symbol = symbol,
            Name = "Concurrency Test",
            Type = "Stock",
            AssetClass = "Equities",
            Status = "Active",
            LotSize = 1,
            IsMarginEligible = false,
        });
        var created = await createResp.Content.ReadFromJsonAsync<InstrumentDto>();
        var staleRowVersion = created!.RowVersion;

        // First update succeeds
        var update1 = await _client.PutAsJsonAsync($"/api/v1/instruments/{created.Id}", new
        {
            Id = created.Id,
            Symbol = symbol,
            Name = "Updated Name",
            Type = "Stock",
            AssetClass = "Equities",
            Status = "Active",
            LotSize = 100,
            IsMarginEligible = false,
            RowVersion = staleRowVersion,
        });
        update1.StatusCode.Should().Be(HttpStatusCode.OK);

        // Second update with stale RowVersion fails
        var update2 = await _client.PutAsJsonAsync($"/api/v1/instruments/{created.Id}", new
        {
            Id = created.Id,
            Symbol = symbol,
            Name = "Should Fail",
            Type = "Stock",
            AssetClass = "Equities",
            Status = "Active",
            LotSize = 50,
            IsMarginEligible = true,
            RowVersion = staleRowVersion,
        });
        update2.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task UpdateClient_WithStaleRowVersion_ShouldReturn409()
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

        // First update succeeds
        var update1 = await _client.PutAsJsonAsync($"/api/v1/clients/{created.Id}", new
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
        update1.StatusCode.Should().Be(HttpStatusCode.OK);

        // Second update with stale RowVersion fails
        var update2 = await _client.PutAsJsonAsync($"/api/v1/clients/{created.Id}", new
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
        update2.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task UpdateTradeOrder_WithStaleRowVersion_ShouldReturn409()
    {
        await AuthenticateAsync();

        // Create prerequisites
        var accountResp = await _client.PostAsJsonAsync("/api/v1/accounts", new
        {
            Number = $"CONC-{Guid.NewGuid():N}"[..20],
            Status = "Active",
            AccountType = "Individual",
            MarginType = "Cash",
            OptionLevel = "Level0",
            Tariff = "Basic",
        });
        var account = await accountResp.Content.ReadFromJsonAsync<AccountDto>();

        var instrumentResp = await _client.PostAsJsonAsync("/api/v1/instruments", new
        {
            Symbol = $"CO-{Guid.NewGuid():N}"[..10],
            Name = "Conc Inst",
            Type = "Stock",
            AssetClass = "Equities",
            Status = "Active",
            LotSize = 1,
            IsMarginEligible = false,
        });
        var instrument = await instrumentResp.Content.ReadFromJsonAsync<InstrumentDto>();

        var createResp = await _client.PostAsJsonAsync("/api/v1/trade-orders", new
        {
            AccountId = account!.Id,
            InstrumentId = instrument!.Id,
            OrderDate = DateTime.UtcNow.ToString("O"),
            Side = "Buy",
            OrderType = "Market",
            TimeInForce = "Day",
            Quantity = 100,
            Price = 50.00m,
        });
        var created = await createResp.Content.ReadFromJsonAsync<TradeOrderDto>();
        var staleRowVersion = created!.RowVersion;

        // First update succeeds
        var update1 = await _client.PutAsJsonAsync($"/api/v1/trade-orders/{created.Id}", new
        {
            Id = created.Id,
            AccountId = account.Id,
            InstrumentId = instrument.Id,
            OrderDate = created.OrderDate.ToString("O"),
            Status = "InProgress",
            Side = "Buy",
            OrderType = "Market",
            TimeInForce = "Day",
            Quantity = 200,
            Price = 55.00m,
            ExecutedQuantity = 0,
            RowVersion = staleRowVersion,
        });
        update1.StatusCode.Should().Be(HttpStatusCode.OK);

        // Second update with stale RowVersion fails
        var update2 = await _client.PutAsJsonAsync($"/api/v1/trade-orders/{created.Id}", new
        {
            Id = created.Id,
            AccountId = account.Id,
            InstrumentId = instrument.Id,
            OrderDate = created.OrderDate.ToString("O"),
            Status = "InProgress",
            Side = "Buy",
            OrderType = "Market",
            TimeInForce = "Day",
            Quantity = 300,
            Price = 60.00m,
            ExecutedQuantity = 0,
            RowVersion = staleRowVersion,
        });
        update2.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    private record CountryListItem(Guid Id, string Iso2, string Name);
}
