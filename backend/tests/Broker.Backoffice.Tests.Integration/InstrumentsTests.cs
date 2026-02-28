using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Broker.Backoffice.Application.Auth;
using Broker.Backoffice.Application.Common;
using Broker.Backoffice.Application.Instruments;
using FluentAssertions;

namespace Broker.Backoffice.Tests.Integration;

[Collection("Integration")]
public class InstrumentsTests(CustomWebApplicationFactory factory)
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
}
