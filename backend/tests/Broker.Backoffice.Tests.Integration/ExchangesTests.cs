using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Broker.Backoffice.Application.Auth;
using Broker.Backoffice.Application.Exchanges;
using FluentAssertions;

namespace Broker.Backoffice.Tests.Integration;

[Collection("Integration")]
public class ExchangesTests(CustomWebApplicationFactory factory)
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
    public async Task ListActive_ShouldReturnList()
    {
        await AuthenticateAsync();
        var response = await _client.GetAsync("/api/v1/exchanges");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<List<ExchangeDto>>();
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task ListAll_ShouldReturnList()
    {
        await AuthenticateAsync();
        var response = await _client.GetAsync("/api/v1/exchanges/all");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<List<ExchangeDto>>();
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task Create_ShouldReturn200()
    {
        await AuthenticateAsync();
        var code = $"EX{Guid.NewGuid():N}"[..8];
        var response = await _client.PostAsJsonAsync("/api/v1/exchanges", new
        {
            Code = code,
            Name = "Test Exchange",
        });
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var exchange = await response.Content.ReadFromJsonAsync<ExchangeDto>();
        exchange.Should().NotBeNull();
        exchange!.Code.Should().Be(code);
        exchange.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task Create_DuplicateCode_ShouldReturn409()
    {
        await AuthenticateAsync();
        var code = $"DX{Guid.NewGuid():N}"[..8];
        await _client.PostAsJsonAsync("/api/v1/exchanges", new { Code = code, Name = "First" });
        var response = await _client.PostAsJsonAsync("/api/v1/exchanges", new { Code = code, Name = "Second" });
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Update_ShouldReturn200()
    {
        await AuthenticateAsync();
        var code = $"UX{Guid.NewGuid():N}"[..8];
        var createResp = await _client.PostAsJsonAsync("/api/v1/exchanges", new
        {
            Code = code,
            Name = "Original Exchange",
        });
        var created = await createResp.Content.ReadFromJsonAsync<ExchangeDto>();

        var updateResp = await _client.PutAsJsonAsync($"/api/v1/exchanges/{created!.Id}", new
        {
            Id = created.Id,
            Code = created.Code,
            Name = "Updated Exchange",
            IsActive = false,
        });
        updateResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await updateResp.Content.ReadFromJsonAsync<ExchangeDto>();
        updated!.Name.Should().Be("Updated Exchange");
        updated.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task CreateAndDelete_ShouldWork()
    {
        await AuthenticateAsync();
        var code = $"ZX{Guid.NewGuid():N}"[..8];
        var createResp = await _client.PostAsJsonAsync("/api/v1/exchanges", new
        {
            Code = code,
            Name = "Delete Me",
        });
        var created = await createResp.Content.ReadFromJsonAsync<ExchangeDto>();

        var delResp = await _client.DeleteAsync($"/api/v1/exchanges/{created!.Id}");
        delResp.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }
}
