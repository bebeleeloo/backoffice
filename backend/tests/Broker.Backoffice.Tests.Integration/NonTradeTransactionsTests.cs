using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Broker.Backoffice.Application.Accounts;
using Broker.Backoffice.Application.Auth;
using Broker.Backoffice.Application.Common;
using Broker.Backoffice.Application.Currencies;
using Broker.Backoffice.Application.Orders.NonTradeOrders;
using Broker.Backoffice.Application.Transactions.NonTradeTransactions;
using Broker.Backoffice.Domain.Transactions;
using FluentAssertions;

namespace Broker.Backoffice.Tests.Integration;

[Collection("Integration")]
public class NonTradeTransactionsTests(CustomWebApplicationFactory factory)
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

    private async Task<(Guid OrderId, Guid CurrencyId)> CreateNonTradeOrderAsync()
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

        // 2. Get first active currency
        var currenciesResp = await _client.GetAsync("/api/v1/currencies");
        var currencies = await currenciesResp.Content.ReadFromJsonAsync<List<CurrencyDto>>();
        var currencyId = currencies!.First().Id;

        // 3. Create non-trade order
        var orderResp = await _client.PostAsJsonAsync("/api/v1/non-trade-orders", new
        {
            AccountId = account!.Id,
            OrderDate = DateTime.UtcNow.ToString("O"),
            NonTradeType = "Deposit",
            Amount = 1000.00m,
            CurrencyId = currencyId,
        });
        var order = await orderResp.Content.ReadFromJsonAsync<NonTradeOrderDto>();

        return (order!.Id, currencyId);
    }

    [Fact]
    public async Task ListNonTradeTransactions_Authenticated_ShouldReturnPaged()
    {
        await AuthenticateAsync();
        var response = await _client.GetAsync("/api/v1/non-trade-transactions?page=1&pageSize=10");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PagedResult<NonTradeTransactionListItemDto>>();
        result.Should().NotBeNull();
        result!.Page.Should().Be(1);
    }

    [Fact]
    public async Task ListNonTradeTransactions_Unauthenticated_ShouldReturn401()
    {
        var response = await _client.GetAsync("/api/v1/non-trade-transactions");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetNonTradeTransaction_NotFound_ShouldReturn404()
    {
        await AuthenticateAsync();
        var response = await _client.GetAsync($"/api/v1/non-trade-transactions/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateNonTradeTransaction_ShouldReturnCreated()
    {
        await AuthenticateAsync();
        var (orderId, currencyId) = await CreateNonTradeOrderAsync();

        var response = await _client.PostAsJsonAsync("/api/v1/non-trade-transactions", new
        {
            OrderId = orderId,
            TransactionDate = DateTime.UtcNow.ToString("O"),
            Amount = 1000.00m,
            CurrencyId = currencyId,
        });
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var transaction = await response.Content.ReadFromJsonAsync<NonTradeTransactionDto>();
        transaction.Should().NotBeNull();
        transaction!.OrderId.Should().Be(orderId);
        transaction.CurrencyId.Should().Be(currencyId);
        transaction.Amount.Should().Be(1000.00m);
        transaction.Status.Should().Be(TransactionStatus.Pending);
        transaction.TransactionNumber.Should().StartWith("NTT-");
    }

    [Fact]
    public async Task CreateAndDeleteNonTradeTransaction_ShouldWork()
    {
        await AuthenticateAsync();
        var (orderId, currencyId) = await CreateNonTradeOrderAsync();

        var createResp = await _client.PostAsJsonAsync("/api/v1/non-trade-transactions", new
        {
            OrderId = orderId,
            TransactionDate = DateTime.UtcNow.ToString("O"),
            Amount = 500.00m,
            CurrencyId = currencyId,
        });
        var transaction = await createResp.Content.ReadFromJsonAsync<NonTradeTransactionDto>();

        var delResp = await _client.DeleteAsync($"/api/v1/non-trade-transactions/{transaction!.Id}");
        delResp.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var getResp = await _client.GetAsync($"/api/v1/non-trade-transactions/{transaction.Id}");
        getResp.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateNonTradeTransaction_WithoutOrder_ShouldReturnCreated()
    {
        await AuthenticateAsync();
        var currenciesResp = await _client.GetAsync("/api/v1/currencies");
        var currencies = await currenciesResp.Content.ReadFromJsonAsync<List<CurrencyDto>>();
        var currencyId = currencies!.First().Id;

        var response = await _client.PostAsJsonAsync("/api/v1/non-trade-transactions", new
        {
            TransactionDate = DateTime.UtcNow.ToString("O"),
            Amount = 500.00m,
            CurrencyId = currencyId,
        });
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var transaction = await response.Content.ReadFromJsonAsync<NonTradeTransactionDto>();
        transaction.Should().NotBeNull();
        transaction!.OrderId.Should().BeNull();
        transaction.OrderNumber.Should().BeNull();
        transaction.Amount.Should().Be(500.00m);
        transaction.Status.Should().Be(TransactionStatus.Pending);
    }

    [Fact]
    public async Task CreateNonTradeTransaction_InvalidData_ShouldReturn400()
    {
        await AuthenticateAsync();
        var response = await _client.PostAsJsonAsync("/api/v1/non-trade-transactions", new
        {
            Amount = 0, // invalid — must be != 0
        });
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
