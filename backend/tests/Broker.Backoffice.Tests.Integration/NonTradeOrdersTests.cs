using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Broker.Backoffice.Application.Accounts;
using Broker.Backoffice.Application.Auth;
using Broker.Backoffice.Application.Common;
using Broker.Backoffice.Application.Currencies;
using Broker.Backoffice.Application.Orders.NonTradeOrders;
using FluentAssertions;

namespace Broker.Backoffice.Tests.Integration;

[Collection("Integration")]
public class NonTradeOrdersTests(CustomWebApplicationFactory factory)
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

    private async Task<(Guid AccountId, Guid CurrencyId)> CreatePrerequisitesAsync()
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

        var currenciesResp = await _client.GetAsync("/api/v1/currencies");
        var currencies = await currenciesResp.Content.ReadFromJsonAsync<List<CurrencyDto>>();
        var currencyId = currencies!.First().Id;

        return (account!.Id, currencyId);
    }

    [Fact]
    public async Task ListNonTradeOrders_Authenticated_ShouldReturnPaged()
    {
        await AuthenticateAsync();
        var response = await _client.GetAsync("/api/v1/non-trade-orders?page=1&pageSize=10");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PagedResult<NonTradeOrderListItemDto>>();
        result.Should().NotBeNull();
        result!.Page.Should().Be(1);
    }

    [Fact]
    public async Task ListNonTradeOrders_Unauthenticated_ShouldReturn401()
    {
        var response = await _client.GetAsync("/api/v1/non-trade-orders");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateNonTradeOrder_ShouldReturnCreated()
    {
        await AuthenticateAsync();
        var (accountId, currencyId) = await CreatePrerequisitesAsync();

        var response = await _client.PostAsJsonAsync("/api/v1/non-trade-orders", new
        {
            AccountId = accountId,
            OrderDate = DateTime.UtcNow.ToString("O"),
            NonTradeType = "Deposit",
            Amount = 1000.00m,
            CurrencyId = currencyId,
        });
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var order = await response.Content.ReadFromJsonAsync<NonTradeOrderDto>();
        order.Should().NotBeNull();
        order!.AccountId.Should().Be(accountId);
        order.CurrencyId.Should().Be(currencyId);
        order.Amount.Should().Be(1000.00m);
        order.OrderNumber.Should().StartWith("NTO-");
    }

    [Fact]
    public async Task GetNonTradeOrder_NotFound_ShouldReturn404()
    {
        await AuthenticateAsync();
        var response = await _client.GetAsync($"/api/v1/non-trade-orders/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateAndDeleteNonTradeOrder_ShouldWork()
    {
        await AuthenticateAsync();
        var (accountId, currencyId) = await CreatePrerequisitesAsync();

        var createResp = await _client.PostAsJsonAsync("/api/v1/non-trade-orders", new
        {
            AccountId = accountId,
            OrderDate = DateTime.UtcNow.ToString("O"),
            NonTradeType = "Withdrawal",
            Amount = -500.00m,
            CurrencyId = currencyId,
        });
        var order = await createResp.Content.ReadFromJsonAsync<NonTradeOrderDto>();

        var delResp = await _client.DeleteAsync($"/api/v1/non-trade-orders/{order!.Id}");
        delResp.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var getResp = await _client.GetAsync($"/api/v1/non-trade-orders/{order.Id}");
        getResp.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateNonTradeOrder_ShouldReturnUpdated()
    {
        await AuthenticateAsync();
        var (accountId, currencyId) = await CreatePrerequisitesAsync();

        var createResp = await _client.PostAsJsonAsync("/api/v1/non-trade-orders", new
        {
            AccountId = accountId,
            OrderDate = DateTime.UtcNow.ToString("O"),
            NonTradeType = "Deposit",
            Amount = 1000.00m,
            CurrencyId = currencyId,
        });
        var created = await createResp.Content.ReadFromJsonAsync<NonTradeOrderDto>();

        var updateResp = await _client.PutAsJsonAsync($"/api/v1/non-trade-orders/{created!.Id}", new
        {
            Id = created.Id,
            AccountId = accountId,
            OrderDate = created.OrderDate.ToString("O"),
            Status = "InProgress",
            NonTradeType = "Deposit",
            Amount = 2000.00m,
            CurrencyId = currencyId,
            Comment = "Updated",
            RowVersion = created.RowVersion,
        });
        updateResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await updateResp.Content.ReadFromJsonAsync<NonTradeOrderDto>();
        updated!.Amount.Should().Be(2000.00m);
        updated.Comment.Should().Be("Updated");
    }
}
