using System.Net;
using System.Net.Http.Json;
using Broker.Backoffice.Application.Common;
using Broker.Backoffice.Application.Instruments;
using FluentAssertions;

namespace Broker.Backoffice.Tests.Integration;

public class InstrumentsTests(CustomWebApplicationFactory factory) : IntegrationTestBase(factory)
{

    [Fact]
    public async Task ListInstruments_Authenticated_ShouldReturnPaged()
    {
        await AuthenticateAsync();
        var response = await _client.GetAsync("/api/v1/instruments?page=1&pageSize=10");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PagedResult<InstrumentListItemDto>>();
        result.Should().NotBeNull();
        result!.Page.Should().Be(1);
    }

    [Fact]
    public async Task ListInstruments_Unauthenticated_ShouldReturn401()
    {
        var response = await _client.GetAsync("/api/v1/instruments");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateInstrument_ShouldReturnCreated()
    {
        await AuthenticateAsync();
        var symbol = $"SYM-{Guid.NewGuid():N}"[..10];
        var response = await _client.PostAsJsonAsync("/api/v1/instruments", new
        {
            Symbol = symbol,
            Name = "Test Instrument",
            Type = "Stock",
            AssetClass = "Equities",
            Status = "Active",
            LotSize = 1,
            IsMarginEligible = false,
        });
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var instrument = await response.Content.ReadFromJsonAsync<InstrumentDto>();
        instrument!.Symbol.Should().Be(symbol);
        instrument.Name.Should().Be("Test Instrument");
    }

    [Fact]
    public async Task CreateInstrument_DuplicateSymbol_ShouldReturn409()
    {
        await AuthenticateAsync();
        var symbol = $"DUP-{Guid.NewGuid():N}"[..10];
        await _client.PostAsJsonAsync("/api/v1/instruments", new
        {
            Symbol = symbol, Name = "First", Type = "Stock",
            AssetClass = "Equities", Status = "Active", LotSize = 1, IsMarginEligible = false,
        });
        var response = await _client.PostAsJsonAsync("/api/v1/instruments", new
        {
            Symbol = symbol, Name = "Second", Type = "Bond",
            AssetClass = "FixedIncome", Status = "Active", LotSize = 1, IsMarginEligible = false,
        });
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task GetInstrument_NotFound_ShouldReturn404()
    {
        await AuthenticateAsync();
        var response = await _client.GetAsync($"/api/v1/instruments/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateAndDeleteInstrument_ShouldWork()
    {
        await AuthenticateAsync();
        var createResp = await _client.PostAsJsonAsync("/api/v1/instruments", new
        {
            Symbol = $"DEL-{Guid.NewGuid():N}"[..10],
            Name = "Delete Me",
            Type = "Stock",
            AssetClass = "Equities",
            Status = "Active",
            LotSize = 1,
            IsMarginEligible = false,
        });
        var instrument = await createResp.Content.ReadFromJsonAsync<InstrumentDto>();

        var delResp = await _client.DeleteAsync($"/api/v1/instruments/{instrument!.Id}");
        delResp.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var getResp = await _client.GetAsync($"/api/v1/instruments/{instrument.Id}");
        getResp.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateInstrument_ShouldReturnUpdated()
    {
        await AuthenticateAsync();
        var createResp = await _client.PostAsJsonAsync("/api/v1/instruments", new
        {
            Symbol = $"UPD-{Guid.NewGuid():N}"[..10],
            Name = "Original Name",
            Type = "Stock",
            AssetClass = "Equities",
            Status = "Active",
            LotSize = 1,
            IsMarginEligible = false,
        });
        var created = await createResp.Content.ReadFromJsonAsync<InstrumentDto>();

        var updateResp = await _client.PutAsJsonAsync($"/api/v1/instruments/{created!.Id}", new
        {
            Id = created.Id,
            Symbol = created.Symbol,
            Name = "Updated Name",
            Type = "Stock",
            AssetClass = "Equities",
            Status = "Active",
            LotSize = 100,
            IsMarginEligible = true,
            Description = "Updated description",
            RowVersion = created.RowVersion,
        });
        updateResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await updateResp.Content.ReadFromJsonAsync<InstrumentDto>();
        updated!.Name.Should().Be("Updated Name");
        updated.LotSize.Should().Be(100);
        updated.IsMarginEligible.Should().BeTrue();
        updated.Description.Should().Be("Updated description");
    }

    [Fact]
    public async Task UpdateInstrument_DuplicateSymbol_ShouldReturn409()
    {
        await AuthenticateAsync();
        var symbol1 = $"DS1-{Guid.NewGuid():N}"[..10];
        var symbol2 = $"DS2-{Guid.NewGuid():N}"[..10];

        await _client.PostAsJsonAsync("/api/v1/instruments", new
        {
            Symbol = symbol1, Name = "First", Type = "Stock",
            AssetClass = "Equities", Status = "Active", LotSize = 1, IsMarginEligible = false,
        });
        var create2Resp = await _client.PostAsJsonAsync("/api/v1/instruments", new
        {
            Symbol = symbol2, Name = "Second", Type = "Stock",
            AssetClass = "Equities", Status = "Active", LotSize = 1, IsMarginEligible = false,
        });
        var instrument2 = await create2Resp.Content.ReadFromJsonAsync<InstrumentDto>();

        // Try to update instrument2's symbol to match instrument1's
        var response = await _client.PutAsJsonAsync($"/api/v1/instruments/{instrument2!.Id}", new
        {
            Id = instrument2.Id,
            Symbol = symbol1, // duplicate
            Name = "Second",
            Type = "Stock",
            AssetClass = "Equities",
            Status = "Active",
            LotSize = 1,
            IsMarginEligible = false,
            RowVersion = instrument2.RowVersion,
        });
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task UpdateInstrument_RouteBodyIdMismatch_ShouldReturn400()
    {
        await AuthenticateAsync();
        var createResp = await _client.PostAsJsonAsync("/api/v1/instruments", new
        {
            Symbol = $"MIS-{Guid.NewGuid():N}"[..10],
            Name = "Mismatch Test",
            Type = "Stock",
            AssetClass = "Equities",
            Status = "Active",
            LotSize = 1,
            IsMarginEligible = false,
        });
        var created = await createResp.Content.ReadFromJsonAsync<InstrumentDto>();

        var response = await _client.PutAsJsonAsync($"/api/v1/instruments/{created!.Id}", new
        {
            Id = Guid.NewGuid(), // different from route ID
            Symbol = created.Symbol,
            Name = "Mismatch Test",
            Type = "Stock",
            AssetClass = "Equities",
            Status = "Active",
            LotSize = 1,
            IsMarginEligible = false,
            RowVersion = created.RowVersion,
        });
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ListInstruments_WithFilters_ShouldReturnFiltered()
    {
        await AuthenticateAsync();

        var symbol = $"FI-{Guid.NewGuid():N}"[..10];
        await _client.PostAsJsonAsync("/api/v1/instruments", new
        {
            Symbol = symbol,
            Name = "Filter Instrument",
            Type = "Stock",
            AssetClass = "Equities",
            Status = "Active",
            LotSize = 100,
            IsMarginEligible = true,
        });

        var response = await _client.GetAsync(
            $"/api/v1/instruments?page=1&pageSize=10&type=Stock&status=Active&isMarginEligible=true&q={symbol[..6]}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PagedResult<InstrumentListItemDto>>();
        result.Should().NotBeNull();
        result!.Items.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetById_ExistingInstrument_Returns200()
    {
        await AuthenticateAsync();
        var symbol = $"GBI-{Guid.NewGuid():N}"[..10];
        var createResp = await _client.PostAsJsonAsync("/api/v1/instruments", new
        {
            Symbol = symbol,
            Name = "GetById Test Instrument",
            Type = "Bond",
            AssetClass = "FixedIncome",
            Status = "Active",
            LotSize = 10,
            IsMarginEligible = true,
            Description = "test description",
        });
        createResp.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await createResp.Content.ReadFromJsonAsync<InstrumentDto>();

        var getResp = await _client.GetAsync($"/api/v1/instruments/{created!.Id}");
        getResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var fetched = await getResp.Content.ReadFromJsonAsync<InstrumentDto>();
        fetched!.Id.Should().Be(created.Id);
        fetched.Symbol.Should().Be(symbol);
        fetched.Name.Should().Be("GetById Test Instrument");
        fetched.Description.Should().Be("test description");
        fetched.LotSize.Should().Be(10);
        fetched.IsMarginEligible.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateInstrument_StaleRowVersion_ShouldReturn409()
    {
        await AuthenticateAsync();
        var symbol = $"SRV-{Guid.NewGuid():N}"[..10];
        var createResp = await _client.PostAsJsonAsync("/api/v1/instruments", new
        {
            Symbol = symbol,
            Name = "Stale RV Instrument",
            Type = "Stock",
            AssetClass = "Equities",
            Status = "Active",
            LotSize = 1,
            IsMarginEligible = false,
        });
        var created = await createResp.Content.ReadFromJsonAsync<InstrumentDto>();
        var staleRowVersion = created!.RowVersion;

        // First update succeeds — changes RowVersion
        await _client.PutAsJsonAsync($"/api/v1/instruments/{created.Id}", new
        {
            Id = created.Id,
            Symbol = symbol,
            Name = "First Update",
            Type = "Stock",
            AssetClass = "Equities",
            Status = "Active",
            LotSize = 50,
            IsMarginEligible = false,
            RowVersion = staleRowVersion,
        });

        // Second update with stale RowVersion — should fail
        var response = await _client.PutAsJsonAsync($"/api/v1/instruments/{created.Id}", new
        {
            Id = created.Id,
            Symbol = symbol,
            Name = "Should Fail",
            Type = "Stock",
            AssetClass = "Equities",
            Status = "Active",
            LotSize = 100,
            IsMarginEligible = false,
            RowVersion = staleRowVersion,
        });
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }
}
