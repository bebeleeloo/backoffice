using System.Net;
using System.Net.Http.Json;
using Broker.Backoffice.Application.Accounts;
using Broker.Backoffice.Application.Common;
using Broker.Backoffice.Application.Instruments;
using Broker.Backoffice.Application.Orders.TradeOrders;
using FluentAssertions;

namespace Broker.Backoffice.Tests.Integration;

public class TradeOrdersTests(CustomWebApplicationFactory factory) : IntegrationTestBase(factory)
{

    private async Task<(Guid AccountId, Guid InstrumentId)> CreatePrerequisitesAsync()
    {
        var accountResp = await _client.PostAsJsonAsync("/api/v1/accounts", new
        {
            Number = $"ACC-{Guid.NewGuid():N}"[..20],
            Status = "Active",
            AccountType = "Individual",
            MarginType = "Cash",
            OptionLevel = "Level0",
            Tariff = "Basic",
        });
        var account = await accountResp.Content.ReadFromJsonAsync<AccountDto>();

        var instrumentResp = await _client.PostAsJsonAsync("/api/v1/instruments", new
        {
            Symbol = $"SYM-{Guid.NewGuid():N}"[..10],
            Name = "Test Inst",
            Type = "Stock",
            AssetClass = "Equities",
            Status = "Active",
            LotSize = 1,
            IsMarginEligible = false,
        });
        var instrument = await instrumentResp.Content.ReadFromJsonAsync<InstrumentDto>();

        return (account!.Id, instrument!.Id);
    }

    [Fact]
    public async Task ListTradeOrders_Authenticated_ShouldReturnPaged()
    {
        await AuthenticateAsync();
        var response = await _client.GetAsync("/api/v1/trade-orders?page=1&pageSize=10");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PagedResult<TradeOrderListItemDto>>();
        result.Should().NotBeNull();
        result!.Page.Should().Be(1);
    }

    [Fact]
    public async Task ListTradeOrders_Unauthenticated_ShouldReturn401()
    {
        var response = await _client.GetAsync("/api/v1/trade-orders");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateTradeOrder_ShouldReturnCreated()
    {
        await AuthenticateAsync();
        var (accountId, instrumentId) = await CreatePrerequisitesAsync();

        var response = await _client.PostAsJsonAsync("/api/v1/trade-orders", new
        {
            AccountId = accountId,
            InstrumentId = instrumentId,
            OrderDate = DateTime.UtcNow.ToString("O"),
            Side = "Buy",
            OrderType = "Market",
            TimeInForce = "Day",
            Quantity = 100,
            Price = 50.00m,
        });
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var order = await response.Content.ReadFromJsonAsync<TradeOrderDto>();
        order.Should().NotBeNull();
        order!.AccountId.Should().Be(accountId);
        order.InstrumentId.Should().Be(instrumentId);
        order.OrderNumber.Should().StartWith("TO-");
    }

    [Fact]
    public async Task CreateTradeOrder_InvalidAccountId_ShouldReturn404()
    {
        await AuthenticateAsync();
        var instrumentResp = await _client.PostAsJsonAsync("/api/v1/instruments", new
        {
            Symbol = $"SYM-{Guid.NewGuid():N}"[..10],
            Name = "Test Inst",
            Type = "Stock",
            AssetClass = "Equities",
            Status = "Active",
            LotSize = 1,
            IsMarginEligible = false,
        });
        var instrument = await instrumentResp.Content.ReadFromJsonAsync<InstrumentDto>();

        var response = await _client.PostAsJsonAsync("/api/v1/trade-orders", new
        {
            AccountId = Guid.NewGuid(),
            InstrumentId = instrument!.Id,
            OrderDate = DateTime.UtcNow.ToString("O"),
            Side = "Buy",
            OrderType = "Market",
            TimeInForce = "Day",
            Quantity = 100,
            Price = 50.00m,
        });
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetTradeOrder_NotFound_ShouldReturn404()
    {
        await AuthenticateAsync();
        var response = await _client.GetAsync($"/api/v1/trade-orders/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateAndDeleteTradeOrder_ShouldWork()
    {
        await AuthenticateAsync();
        var (accountId, instrumentId) = await CreatePrerequisitesAsync();

        var createResp = await _client.PostAsJsonAsync("/api/v1/trade-orders", new
        {
            AccountId = accountId,
            InstrumentId = instrumentId,
            OrderDate = DateTime.UtcNow.ToString("O"),
            Side = "Sell",
            OrderType = "Market",
            TimeInForce = "Day",
            Quantity = 50,
            Price = 25.00m,
        });
        var order = await createResp.Content.ReadFromJsonAsync<TradeOrderDto>();

        var delResp = await _client.DeleteAsync($"/api/v1/trade-orders/{order!.Id}");
        delResp.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var getResp = await _client.GetAsync($"/api/v1/trade-orders/{order.Id}");
        getResp.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateTradeOrder_ShouldReturnUpdated()
    {
        await AuthenticateAsync();
        var (accountId, instrumentId) = await CreatePrerequisitesAsync();

        var createResp = await _client.PostAsJsonAsync("/api/v1/trade-orders", new
        {
            AccountId = accountId,
            InstrumentId = instrumentId,
            OrderDate = DateTime.UtcNow.ToString("O"),
            Side = "Buy",
            OrderType = "Market",
            TimeInForce = "Day",
            Quantity = 100,
            Price = 50.00m,
        });
        var created = await createResp.Content.ReadFromJsonAsync<TradeOrderDto>();

        var updateResp = await _client.PutAsJsonAsync($"/api/v1/trade-orders/{created!.Id}", new
        {
            Id = created.Id,
            AccountId = accountId,
            InstrumentId = instrumentId,
            OrderDate = created.OrderDate.ToString("O"),
            Status = "InProgress",
            Side = "Buy",
            OrderType = "Market",
            TimeInForce = "Day",
            Quantity = 200,
            Price = 55.00m,
            ExecutedQuantity = 0,
            Comment = "Updated",
            RowVersion = created.RowVersion,
        });
        updateResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await updateResp.Content.ReadFromJsonAsync<TradeOrderDto>();
        updated!.Quantity.Should().Be(200);
        updated.Comment.Should().Be("Updated");
    }

    [Fact]
    public async Task CreateTradeOrder_LimitWithoutPrice_ShouldReturn400()
    {
        await AuthenticateAsync();
        var (accountId, instrumentId) = await CreatePrerequisitesAsync();

        var response = await _client.PostAsJsonAsync("/api/v1/trade-orders", new
        {
            AccountId = accountId,
            InstrumentId = instrumentId,
            OrderDate = DateTime.UtcNow.ToString("O"),
            Side = "Buy",
            OrderType = "Limit",
            TimeInForce = "Day",
            Quantity = 100,
            // Price intentionally omitted — required for Limit orders
        });
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateTradeOrder_StopWithoutStopPrice_ShouldReturn400()
    {
        await AuthenticateAsync();
        var (accountId, instrumentId) = await CreatePrerequisitesAsync();

        var response = await _client.PostAsJsonAsync("/api/v1/trade-orders", new
        {
            AccountId = accountId,
            InstrumentId = instrumentId,
            OrderDate = DateTime.UtcNow.ToString("O"),
            Side = "Buy",
            OrderType = "Stop",
            TimeInForce = "Day",
            Quantity = 100,
            Price = 50.00m,
            // StopPrice intentionally omitted — required for Stop orders
        });
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateTradeOrder_GTDWithoutExpiration_ShouldReturn400()
    {
        await AuthenticateAsync();
        var (accountId, instrumentId) = await CreatePrerequisitesAsync();

        var response = await _client.PostAsJsonAsync("/api/v1/trade-orders", new
        {
            AccountId = accountId,
            InstrumentId = instrumentId,
            OrderDate = DateTime.UtcNow.ToString("O"),
            Side = "Buy",
            OrderType = "Market",
            TimeInForce = "GTD",
            Quantity = 100,
            Price = 50.00m,
            // ExpirationDate intentionally omitted — required for GTD
        });
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateTradeOrder_RouteBodyIdMismatch_ShouldReturn400()
    {
        await AuthenticateAsync();
        var (accountId, instrumentId) = await CreatePrerequisitesAsync();

        var createResp = await _client.PostAsJsonAsync("/api/v1/trade-orders", new
        {
            AccountId = accountId,
            InstrumentId = instrumentId,
            OrderDate = DateTime.UtcNow.ToString("O"),
            Side = "Buy",
            OrderType = "Market",
            TimeInForce = "Day",
            Quantity = 100,
            Price = 50.00m,
        });
        var created = await createResp.Content.ReadFromJsonAsync<TradeOrderDto>();

        var response = await _client.PutAsJsonAsync($"/api/v1/trade-orders/{created!.Id}", new
        {
            Id = Guid.NewGuid(), // different from route ID
            AccountId = accountId,
            InstrumentId = instrumentId,
            OrderDate = created.OrderDate.ToString("O"),
            Status = "InProgress",
            Side = "Buy",
            OrderType = "Market",
            TimeInForce = "Day",
            Quantity = 100,
            Price = 50.00m,
            ExecutedQuantity = 0,
            RowVersion = created.RowVersion,
        });
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ListTradeOrders_WithFilters_ShouldReturnFiltered()
    {
        await AuthenticateAsync();
        var (accountId, instrumentId) = await CreatePrerequisitesAsync();

        var createResp = await _client.PostAsJsonAsync("/api/v1/trade-orders", new
        {
            AccountId = accountId,
            InstrumentId = instrumentId,
            OrderDate = DateTime.UtcNow.ToString("O"),
            Side = "Buy",
            OrderType = "Market",
            TimeInForce = "Day",
            Quantity = 100,
            Price = 50.00m,
        });
        var order = await createResp.Content.ReadFromJsonAsync<TradeOrderDto>();

        var response = await _client.GetAsync(
            $"/api/v1/trade-orders?page=1&pageSize=10&status=New&side=Buy&orderType=Market&q={order!.OrderNumber}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PagedResult<TradeOrderListItemDto>>();
        result.Should().NotBeNull();
        result!.Items.Should().NotBeEmpty();
    }

    [Fact]
    public async Task ListTradeOrders_SortByInstrumentSymbol_ShouldReturn200()
    {
        await AuthenticateAsync();
        var response = await _client.GetAsync(
            "/api/v1/trade-orders?page=1&pageSize=10&sort=instrumentSymbol asc");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PagedResult<TradeOrderListItemDto>>();
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task GetById_ExistingTradeOrder_Returns200()
    {
        await AuthenticateAsync();
        var (accountId, instrumentId) = await CreatePrerequisitesAsync();

        var createResp = await _client.PostAsJsonAsync("/api/v1/trade-orders", new
        {
            AccountId = accountId,
            InstrumentId = instrumentId,
            OrderDate = DateTime.UtcNow.ToString("O"),
            Side = "Buy",
            OrderType = "Market",
            TimeInForce = "Day",
            Quantity = 75,
            Price = 42.50m,
            Comment = "GetById test order",
        });
        createResp.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await createResp.Content.ReadFromJsonAsync<TradeOrderDto>();

        var getResp = await _client.GetAsync($"/api/v1/trade-orders/{created!.Id}");
        getResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var fetched = await getResp.Content.ReadFromJsonAsync<TradeOrderDto>();
        fetched!.Id.Should().Be(created.Id);
        fetched.AccountId.Should().Be(accountId);
        fetched.InstrumentId.Should().Be(instrumentId);
        fetched.Quantity.Should().Be(75);
        fetched.Price.Should().Be(42.50m);
        fetched.Comment.Should().Be("GetById test order");
        fetched.OrderNumber.Should().StartWith("TO-");
    }

    [Fact]
    public async Task UpdateTradeOrder_StaleRowVersion_ShouldReturn409()
    {
        await AuthenticateAsync();
        var (accountId, instrumentId) = await CreatePrerequisitesAsync();

        var createResp = await _client.PostAsJsonAsync("/api/v1/trade-orders", new
        {
            AccountId = accountId,
            InstrumentId = instrumentId,
            OrderDate = DateTime.UtcNow.ToString("O"),
            Side = "Buy",
            OrderType = "Market",
            TimeInForce = "Day",
            Quantity = 100,
            Price = 50.00m,
        });
        var created = await createResp.Content.ReadFromJsonAsync<TradeOrderDto>();
        var staleRowVersion = created!.RowVersion;

        // First update succeeds — changes RowVersion
        await _client.PutAsJsonAsync($"/api/v1/trade-orders/{created.Id}", new
        {
            Id = created.Id,
            AccountId = accountId,
            InstrumentId = instrumentId,
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

        // Second update with stale RowVersion — should fail
        var response = await _client.PutAsJsonAsync($"/api/v1/trade-orders/{created.Id}", new
        {
            Id = created.Id,
            AccountId = accountId,
            InstrumentId = instrumentId,
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
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }
}
