using System.Net;
using System.Net.Http.Json;
using Broker.Backoffice.Application.Accounts;
using Broker.Backoffice.Application.Instruments;
using Broker.Backoffice.Application.Roles;
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
    public async Task UpdateRole_WithStaleRowVersion_ShouldReturn409()
    {
        await AuthenticateAsync();

        var createResp = await _client.PostAsJsonAsync("/api/v1/roles", new
        {
            Name = $"conc_{Guid.NewGuid():N}"[..20],
            Description = "Concurrency test",
        });
        var created = await createResp.Content.ReadFromJsonAsync<RoleDto>();
        var staleRowVersion = created!.RowVersion;

        // First update succeeds
        var update1 = await _client.PutAsJsonAsync($"/api/v1/roles/{created.Id}", new
        {
            Id = created.Id,
            Name = created.Name,
            Description = "Updated once",
            RowVersion = staleRowVersion,
        });
        update1.StatusCode.Should().Be(HttpStatusCode.OK);

        // Second update with stale RowVersion fails
        var update2 = await _client.PutAsJsonAsync($"/api/v1/roles/{created.Id}", new
        {
            Id = created.Id,
            Name = created.Name,
            Description = "Should fail",
            RowVersion = staleRowVersion,
        });
        update2.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }
}
