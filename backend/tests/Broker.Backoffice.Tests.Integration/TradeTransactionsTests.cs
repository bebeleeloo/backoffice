using System.Net;
using System.Net.Http.Json;
using Broker.Backoffice.Application.Accounts;
using Broker.Backoffice.Application.Common;
using Broker.Backoffice.Application.Instruments;
using Broker.Backoffice.Application.Orders.TradeOrders;
using Broker.Backoffice.Application.Transactions.TradeTransactions;
using Broker.Backoffice.Domain.Transactions;
using FluentAssertions;

namespace Broker.Backoffice.Tests.Integration;

public class TradeTransactionsTests(CustomWebApplicationFactory factory) : IntegrationTestBase(factory)
{

    private async Task<(Guid OrderId, Guid InstrumentId)> CreateTradeOrderAsync()
    {
        // 1. Create account
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

        // 2. Create instrument
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

        // 3. Create trade order
        var orderResp = await _client.PostAsJsonAsync("/api/v1/trade-orders", new
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
        var order = await orderResp.Content.ReadFromJsonAsync<TradeOrderDto>();

        return (order!.Id, instrument.Id);
    }

    [Fact]
    public async Task ListTradeTransactions_Authenticated_ShouldReturnPaged()
    {
        await AuthenticateAsync();
        var response = await _client.GetAsync("/api/v1/trade-transactions?page=1&pageSize=10");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PagedResult<TradeTransactionListItemDto>>();
        result.Should().NotBeNull();
        result!.Page.Should().Be(1);
    }

    [Fact]
    public async Task ListTradeTransactions_Unauthenticated_ShouldReturn401()
    {
        var response = await _client.GetAsync("/api/v1/trade-transactions");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetTradeTransaction_NotFound_ShouldReturn404()
    {
        await AuthenticateAsync();
        var response = await _client.GetAsync($"/api/v1/trade-transactions/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateTradeTransaction_ShouldReturnCreated()
    {
        await AuthenticateAsync();
        var (orderId, instrumentId) = await CreateTradeOrderAsync();

        var response = await _client.PostAsJsonAsync("/api/v1/trade-transactions", new
        {
            OrderId = orderId,
            InstrumentId = instrumentId,
            TransactionDate = DateTime.UtcNow.ToString("O"),
            Side = "Buy",
            Quantity = 100,
            Price = 50.00m,
        });
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var transaction = await response.Content.ReadFromJsonAsync<TradeTransactionDto>();
        transaction.Should().NotBeNull();
        transaction!.OrderId.Should().Be(orderId);
        transaction.InstrumentId.Should().Be(instrumentId);
        transaction.Side.Should().Be(Domain.Orders.TradeSide.Buy);
        transaction.Quantity.Should().Be(100);
        transaction.Price.Should().Be(50.00m);
        transaction.Status.Should().Be(TransactionStatus.Pending);
        transaction.TransactionNumber.Should().StartWith("TT-");
    }

    [Fact]
    public async Task CreateAndDeleteTradeTransaction_ShouldWork()
    {
        await AuthenticateAsync();
        var (orderId, instrumentId) = await CreateTradeOrderAsync();

        var createResp = await _client.PostAsJsonAsync("/api/v1/trade-transactions", new
        {
            OrderId = orderId,
            InstrumentId = instrumentId,
            TransactionDate = DateTime.UtcNow.ToString("O"),
            Side = "Buy",
            Quantity = 50,
            Price = 25.00m,
        });
        var transaction = await createResp.Content.ReadFromJsonAsync<TradeTransactionDto>();

        var delResp = await _client.DeleteAsync($"/api/v1/trade-transactions/{transaction!.Id}");
        delResp.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var getResp = await _client.GetAsync($"/api/v1/trade-transactions/{transaction.Id}");
        getResp.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateTradeTransaction_WithoutOrder_ShouldReturnCreated()
    {
        await AuthenticateAsync();
        var (_, instrumentId) = await CreateTradeOrderAsync();

        var response = await _client.PostAsJsonAsync("/api/v1/trade-transactions", new
        {
            InstrumentId = instrumentId,
            TransactionDate = DateTime.UtcNow.ToString("O"),
            Side = "Buy",
            Quantity = 100,
            Price = 50.00m,
        });
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var transaction = await response.Content.ReadFromJsonAsync<TradeTransactionDto>();
        transaction.Should().NotBeNull();
        transaction!.OrderId.Should().BeNull();
        transaction.OrderNumber.Should().BeNull();
        transaction.Status.Should().Be(TransactionStatus.Pending);
    }

    [Fact]
    public async Task CreateTradeTransaction_SideMismatchWithOrder_ShouldReturn409()
    {
        await AuthenticateAsync();
        var (orderId, instrumentId) = await CreateTradeOrderAsync(); // order has Side = "Buy"

        var response = await _client.PostAsJsonAsync("/api/v1/trade-transactions", new
        {
            OrderId = orderId,
            InstrumentId = instrumentId,
            TransactionDate = DateTime.UtcNow.ToString("O"),
            Side = "Sell", // mismatch — order is Buy
            Quantity = 100,
            Price = 50.00m,
        });
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task CreateTradeTransaction_InvalidData_ShouldReturn400()
    {
        await AuthenticateAsync();
        var response = await _client.PostAsJsonAsync("/api/v1/trade-transactions", new
        {
            Quantity = 0, // invalid — must be > 0
        });
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateTradeTransaction_InvalidInstrumentId_ShouldReturn404()
    {
        await AuthenticateAsync();
        var (orderId, _) = await CreateTradeOrderAsync();

        var response = await _client.PostAsJsonAsync("/api/v1/trade-transactions", new
        {
            OrderId = orderId,
            InstrumentId = Guid.NewGuid(), // non-existent instrument
            TransactionDate = DateTime.UtcNow.ToString("O"),
            Side = "Buy",
            Quantity = 100,
            Price = 50.00m,
        });
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateTradeTransaction_InvalidOrderId_ShouldReturn404()
    {
        await AuthenticateAsync();
        var (_, instrumentId) = await CreateTradeOrderAsync();

        var response = await _client.PostAsJsonAsync("/api/v1/trade-transactions", new
        {
            OrderId = Guid.NewGuid(), // non-existent order
            InstrumentId = instrumentId,
            TransactionDate = DateTime.UtcNow.ToString("O"),
            Side = "Buy",
            Quantity = 100,
            Price = 50.00m,
        });
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateTradeTransaction_RouteBodyIdMismatch_ShouldReturn400()
    {
        await AuthenticateAsync();
        var (orderId, instrumentId) = await CreateTradeOrderAsync();

        var createResp = await _client.PostAsJsonAsync("/api/v1/trade-transactions", new
        {
            OrderId = orderId,
            InstrumentId = instrumentId,
            TransactionDate = DateTime.UtcNow.ToString("O"),
            Side = "Buy",
            Quantity = 100,
            Price = 50.00m,
        });
        var created = await createResp.Content.ReadFromJsonAsync<TradeTransactionDto>();

        var response = await _client.PutAsJsonAsync($"/api/v1/trade-transactions/{created!.Id}", new
        {
            Id = Guid.NewGuid(), // different from route ID
            InstrumentId = instrumentId,
            TransactionDate = created.TransactionDate.ToString("O"),
            Status = "Completed",
            Side = "Buy",
            Quantity = 100,
            Price = 50.00m,
            RowVersion = created.RowVersion,
        });
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateTradeTransaction_ShouldReturnUpdated()
    {
        await AuthenticateAsync();
        var (orderId, instrumentId) = await CreateTradeOrderAsync();

        var createResp = await _client.PostAsJsonAsync("/api/v1/trade-transactions", new
        {
            OrderId = orderId,
            InstrumentId = instrumentId,
            TransactionDate = DateTime.UtcNow.ToString("O"),
            Side = "Buy",
            Quantity = 100,
            Price = 50.00m,
        });
        var created = await createResp.Content.ReadFromJsonAsync<TradeTransactionDto>();

        var updateResp = await _client.PutAsJsonAsync($"/api/v1/trade-transactions/{created!.Id}", new
        {
            Id = created.Id,
            OrderId = orderId,
            InstrumentId = instrumentId,
            TransactionDate = created.TransactionDate.ToString("O"),
            Status = "Settled",
            Side = "Buy",
            Quantity = 200,
            Price = 55.00m,
            Commission = 9.99m,
            Comment = "Updated",
            RowVersion = created.RowVersion,
        });
        updateResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await updateResp.Content.ReadFromJsonAsync<TradeTransactionDto>();
        updated!.Quantity.Should().Be(200);
        updated.Price.Should().Be(55.00m);
        updated.Comment.Should().Be("Updated");
    }

    [Fact]
    public async Task UpdateTradeTransaction_StaleRowVersion_ShouldReturn409()
    {
        await AuthenticateAsync();
        var (orderId, instrumentId) = await CreateTradeOrderAsync();

        var createResp = await _client.PostAsJsonAsync("/api/v1/trade-transactions", new
        {
            OrderId = orderId,
            InstrumentId = instrumentId,
            TransactionDate = DateTime.UtcNow.ToString("O"),
            Side = "Buy",
            Quantity = 100,
            Price = 50.00m,
        });
        var created = await createResp.Content.ReadFromJsonAsync<TradeTransactionDto>();
        var staleRowVersion = created!.RowVersion;

        // First update succeeds
        await _client.PutAsJsonAsync($"/api/v1/trade-transactions/{created.Id}", new
        {
            Id = created.Id,
            OrderId = orderId,
            InstrumentId = instrumentId,
            TransactionDate = created.TransactionDate.ToString("O"),
            Status = "Settled",
            Side = "Buy",
            Quantity = 200,
            Price = 55.00m,
            RowVersion = staleRowVersion,
        });

        // Second update with stale RowVersion — should fail
        var response = await _client.PutAsJsonAsync($"/api/v1/trade-transactions/{created.Id}", new
        {
            Id = created.Id,
            OrderId = orderId,
            InstrumentId = instrumentId,
            TransactionDate = created.TransactionDate.ToString("O"),
            Status = "Settled",
            Side = "Buy",
            Quantity = 300,
            Price = 60.00m,
            RowVersion = staleRowVersion,
        });
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task GetTradeTransactionsByOrder_ShouldReturnList()
    {
        await AuthenticateAsync();
        var (orderId, instrumentId) = await CreateTradeOrderAsync();

        // Create a transaction for this order
        await _client.PostAsJsonAsync("/api/v1/trade-transactions", new
        {
            OrderId = orderId,
            InstrumentId = instrumentId,
            TransactionDate = DateTime.UtcNow.ToString("O"),
            Side = "Buy",
            Quantity = 100,
            Price = 50.00m,
        });

        var response = await _client.GetAsync($"/api/v1/trade-transactions/by-order/{orderId}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var transactions = await response.Content.ReadFromJsonAsync<List<TradeTransactionListItemDto>>();
        transactions.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetTradeTransactionsByOrder_InvalidOrder_ShouldReturn404()
    {
        await AuthenticateAsync();
        var response = await _client.GetAsync($"/api/v1/trade-transactions/by-order/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ListTradeTransactions_WithFilters_ShouldReturnFiltered()
    {
        await AuthenticateAsync();
        var (orderId, instrumentId) = await CreateTradeOrderAsync();

        var createResp = await _client.PostAsJsonAsync("/api/v1/trade-transactions", new
        {
            OrderId = orderId,
            InstrumentId = instrumentId,
            TransactionDate = DateTime.UtcNow.ToString("O"),
            Side = "Buy",
            Quantity = 100,
            Price = 50.00m,
        });
        var tx = await createResp.Content.ReadFromJsonAsync<TradeTransactionDto>();

        var response = await _client.GetAsync(
            $"/api/v1/trade-transactions?page=1&pageSize=10&status=Pending&side=Buy&q={tx!.TransactionNumber}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PagedResult<TradeTransactionListItemDto>>();
        result.Should().NotBeNull();
        result!.Items.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetById_ExistingTradeTransaction_Returns200()
    {
        await AuthenticateAsync();
        var (orderId, instrumentId) = await CreateTradeOrderAsync();

        var createResp = await _client.PostAsJsonAsync("/api/v1/trade-transactions", new
        {
            OrderId = orderId,
            InstrumentId = instrumentId,
            TransactionDate = DateTime.UtcNow.ToString("O"),
            Side = "Buy",
            Quantity = 150,
            Price = 75.50m,
            Commission = 5.99m,
            Comment = "GetById test transaction",
        });
        createResp.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await createResp.Content.ReadFromJsonAsync<TradeTransactionDto>();

        var getResp = await _client.GetAsync($"/api/v1/trade-transactions/{created!.Id}");
        getResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var fetched = await getResp.Content.ReadFromJsonAsync<TradeTransactionDto>();
        fetched!.Id.Should().Be(created.Id);
        fetched.OrderId.Should().Be(orderId);
        fetched.InstrumentId.Should().Be(instrumentId);
        fetched.Quantity.Should().Be(150);
        fetched.Price.Should().Be(75.50m);
        fetched.Commission.Should().Be(5.99m);
        fetched.Comment.Should().Be("GetById test transaction");
        fetched.TransactionNumber.Should().StartWith("TT-");
        fetched.Status.Should().Be(TransactionStatus.Pending);
    }
}
