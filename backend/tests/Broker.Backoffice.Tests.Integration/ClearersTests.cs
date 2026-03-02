using System.Net;
using System.Net.Http.Json;
using Broker.Backoffice.Application.Clearers;
using FluentAssertions;

namespace Broker.Backoffice.Tests.Integration;

public class ClearersTests(CustomWebApplicationFactory factory) : IntegrationTestBase(factory)
{

    [Fact]
    public async Task ListActive_ShouldReturnList()
    {
        await AuthenticateAsync();
        var response = await _client.GetAsync("/api/v1/clearers");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<List<ClearerDto>>();
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task ListAll_ShouldReturnList()
    {
        await AuthenticateAsync();
        var response = await _client.GetAsync("/api/v1/clearers/all");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<List<ClearerDto>>();
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task Create_ShouldReturn200()
    {
        await AuthenticateAsync();
        var response = await _client.PostAsJsonAsync("/api/v1/clearers", new
        {
            Name = $"Clearer-{Guid.NewGuid():N}"[..20],
            Description = "Test clearer",
        });
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var clearer = await response.Content.ReadFromJsonAsync<ClearerDto>();
        clearer.Should().NotBeNull();
        clearer!.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task Create_DuplicateName_ShouldReturn409()
    {
        await AuthenticateAsync();
        var name = $"DupClearer-{Guid.NewGuid():N}"[..20];
        await _client.PostAsJsonAsync("/api/v1/clearers", new { Name = name });
        var response = await _client.PostAsJsonAsync("/api/v1/clearers", new { Name = name });
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Update_ShouldReturn200()
    {
        await AuthenticateAsync();
        var createResp = await _client.PostAsJsonAsync("/api/v1/clearers", new
        {
            Name = $"UpdClearer-{Guid.NewGuid():N}"[..20],
            Description = "Original",
        });
        var created = await createResp.Content.ReadFromJsonAsync<ClearerDto>();

        var updateResp = await _client.PutAsJsonAsync($"/api/v1/clearers/{created!.Id}", new
        {
            Id = created.Id,
            Name = created.Name,
            Description = "Updated",
            IsActive = false,
        });
        updateResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await updateResp.Content.ReadFromJsonAsync<ClearerDto>();
        updated!.Description.Should().Be("Updated");
        updated.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task CreateAndDelete_ShouldWork()
    {
        await AuthenticateAsync();
        var createResp = await _client.PostAsJsonAsync("/api/v1/clearers", new
        {
            Name = $"DelClearer-{Guid.NewGuid():N}"[..20],
        });
        var created = await createResp.Content.ReadFromJsonAsync<ClearerDto>();

        var delResp = await _client.DeleteAsync($"/api/v1/clearers/{created!.Id}");
        delResp.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }
}
