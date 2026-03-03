using System.Net;
using System.Net.Http.Json;
using Broker.Backoffice.Application.Currencies;
using FluentAssertions;

namespace Broker.Backoffice.Tests.Integration;

public class CurrenciesTests(CustomWebApplicationFactory factory) : IntegrationTestBase(factory)
{

    [Fact]
    public async Task ListActive_ShouldReturnList()
    {
        await AuthenticateAsync();
        var response = await _client.GetAsync("/api/v1/currencies");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<List<CurrencyDto>>();
        result.Should().NotBeNull();
        result!.Should().NotBeEmpty();
    }

    [Fact]
    public async Task ListAll_ShouldReturnList()
    {
        await AuthenticateAsync();
        var response = await _client.GetAsync("/api/v1/currencies/all");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<List<CurrencyDto>>();
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task Create_ShouldReturn200()
    {
        await AuthenticateAsync();
        var code = $"X{Guid.NewGuid():N}"[..3];
        var response = await _client.PostAsJsonAsync("/api/v1/currencies", new
        {
            Code = code,
            Name = "Test Currency",
            Symbol = "T$",
        });
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var currency = await response.Content.ReadFromJsonAsync<CurrencyDto>();
        currency.Should().NotBeNull();
        currency!.Code.Should().Be(code);
        currency.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task Create_DuplicateCode_ShouldReturn409()
    {
        await AuthenticateAsync();
        var code = $"D{Guid.NewGuid():N}"[..3];
        await _client.PostAsJsonAsync("/api/v1/currencies", new { Code = code, Name = "First" });
        var response = await _client.PostAsJsonAsync("/api/v1/currencies", new { Code = code, Name = "Second" });
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Update_ShouldReturn200()
    {
        await AuthenticateAsync();
        var code = $"U{Guid.NewGuid():N}"[..3];
        var createResp = await _client.PostAsJsonAsync("/api/v1/currencies", new
        {
            Code = code,
            Name = "Original",
        });
        var created = await createResp.Content.ReadFromJsonAsync<CurrencyDto>();

        var updateResp = await _client.PutAsJsonAsync($"/api/v1/currencies/{created!.Id}", new
        {
            Id = created.Id,
            Code = created.Code,
            Name = "Updated",
            IsActive = false,
        });
        updateResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await updateResp.Content.ReadFromJsonAsync<CurrencyDto>();
        updated!.Name.Should().Be("Updated");
        updated.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task CreateAndDelete_ShouldWork()
    {
        await AuthenticateAsync();
        var code = $"Z{Guid.NewGuid():N}"[..3];
        var createResp = await _client.PostAsJsonAsync("/api/v1/currencies", new
        {
            Code = code,
            Name = "Delete Me",
        });
        var created = await createResp.Content.ReadFromJsonAsync<CurrencyDto>();

        var delResp = await _client.DeleteAsync($"/api/v1/currencies/{created!.Id}");
        delResp.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Update_DuplicateCode_ShouldReturn409()
    {
        await AuthenticateAsync();
        var code1 = $"A{Guid.NewGuid():N}"[..3];
        var code2 = $"B{Guid.NewGuid():N}"[..3];

        await _client.PostAsJsonAsync("/api/v1/currencies", new { Code = code1, Name = "First" });
        var create2Resp = await _client.PostAsJsonAsync("/api/v1/currencies", new { Code = code2, Name = "Second" });
        var currency2 = await create2Resp.Content.ReadFromJsonAsync<CurrencyDto>();

        var response = await _client.PutAsJsonAsync($"/api/v1/currencies/{currency2!.Id}", new
        {
            Id = currency2.Id,
            Code = code1, // duplicate
            Name = "Second",
            IsActive = true,
        });
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }
}
