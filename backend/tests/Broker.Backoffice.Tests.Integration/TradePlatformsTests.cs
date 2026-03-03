using System.Net;
using System.Net.Http.Json;
using Broker.Backoffice.Application.TradePlatforms;
using FluentAssertions;

namespace Broker.Backoffice.Tests.Integration;

public class TradePlatformsTests(CustomWebApplicationFactory factory) : IntegrationTestBase(factory)
{

    [Fact]
    public async Task ListActive_ShouldReturnList()
    {
        await AuthenticateAsync();
        var response = await _client.GetAsync("/api/v1/trade-platforms");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<List<TradePlatformDto>>();
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task ListAll_ShouldReturnList()
    {
        await AuthenticateAsync();
        var response = await _client.GetAsync("/api/v1/trade-platforms/all");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<List<TradePlatformDto>>();
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task Create_ShouldReturn200()
    {
        await AuthenticateAsync();
        var name = $"Platform-{Guid.NewGuid():N}"[..20];
        var response = await _client.PostAsJsonAsync("/api/v1/trade-platforms", new
        {
            Name = name,
            Description = "Test platform",
        });
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var platform = await response.Content.ReadFromJsonAsync<TradePlatformDto>();
        platform.Should().NotBeNull();
        platform!.Name.Should().Be(name);
        platform.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task Create_DuplicateName_ShouldReturn409()
    {
        await AuthenticateAsync();
        var name = $"DupPlat-{Guid.NewGuid():N}"[..20];
        await _client.PostAsJsonAsync("/api/v1/trade-platforms", new { Name = name });
        var response = await _client.PostAsJsonAsync("/api/v1/trade-platforms", new { Name = name });
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Update_ShouldReturn200()
    {
        await AuthenticateAsync();
        var name = $"UpdPlat-{Guid.NewGuid():N}"[..20];
        var createResp = await _client.PostAsJsonAsync("/api/v1/trade-platforms", new
        {
            Name = name,
            Description = "Original",
        });
        var created = await createResp.Content.ReadFromJsonAsync<TradePlatformDto>();

        var updateResp = await _client.PutAsJsonAsync($"/api/v1/trade-platforms/{created!.Id}", new
        {
            Id = created.Id,
            Name = created.Name,
            Description = "Updated",
            IsActive = false,
        });
        updateResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await updateResp.Content.ReadFromJsonAsync<TradePlatformDto>();
        updated!.Description.Should().Be("Updated");
        updated.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task CreateAndDelete_ShouldWork()
    {
        await AuthenticateAsync();
        var name = $"DelPlat-{Guid.NewGuid():N}"[..20];
        var createResp = await _client.PostAsJsonAsync("/api/v1/trade-platforms", new
        {
            Name = name,
        });
        var created = await createResp.Content.ReadFromJsonAsync<TradePlatformDto>();

        var delResp = await _client.DeleteAsync($"/api/v1/trade-platforms/{created!.Id}");
        delResp.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Update_DuplicateName_ShouldReturn409()
    {
        await AuthenticateAsync();
        var name1 = $"Plat1-{Guid.NewGuid():N}"[..20];
        var name2 = $"Plat2-{Guid.NewGuid():N}"[..20];

        await _client.PostAsJsonAsync("/api/v1/trade-platforms", new { Name = name1 });
        var create2Resp = await _client.PostAsJsonAsync("/api/v1/trade-platforms", new { Name = name2 });
        var platform2 = await create2Resp.Content.ReadFromJsonAsync<TradePlatformDto>();

        var response = await _client.PutAsJsonAsync($"/api/v1/trade-platforms/{platform2!.Id}", new
        {
            Id = platform2.Id,
            Name = name1, // duplicate
            IsActive = true,
        });
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }
}
