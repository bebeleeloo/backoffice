using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Broker.Backoffice.Application.Accounts;
using Broker.Backoffice.Application.Auth;
using Broker.Backoffice.Application.Common;
using Broker.Backoffice.Application.Instruments;
using Broker.Backoffice.Application.Orders.TradeOrders;
using Broker.Backoffice.Application.Transactions.TradeTransactions;
using Broker.Backoffice.Domain.Transactions;
using FluentAssertions;

namespace Broker.Backoffice.Tests.Integration;

[Collection("Integration")]
public class TradeTransactionsTests(CustomWebApplicationFactory factory)
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
    public async Task CreateTradeTransaction_InvalidData_ShouldReturn400()
    {
        await AuthenticateAsync();
        var response = await _client.PostAsJsonAsync("/api/v1/trade-transactions", new
        {
            Quantity = 0, // invalid — must be > 0
        });
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
