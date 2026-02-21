using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Broker.Backoffice.Application.Accounts;
using Broker.Backoffice.Application.Auth;
using Broker.Backoffice.Application.Common;
using Broker.Backoffice.Domain.Accounts;
using FluentAssertions;

namespace Broker.Backoffice.Tests.Integration;

[Collection("Integration")]
public class AccountsTests(CustomWebApplicationFactory factory)
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
}
