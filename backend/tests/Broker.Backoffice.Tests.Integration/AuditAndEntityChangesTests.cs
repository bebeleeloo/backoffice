using System.Net;
using System.Net.Http.Json;
using Broker.Backoffice.Application.AuditLogs;
using Broker.Backoffice.Application.Common;
using Broker.Backoffice.Application.EntityChanges;
using FluentAssertions;

namespace Broker.Backoffice.Tests.Integration;

public class AuditAndEntityChangesTests(CustomWebApplicationFactory factory) : IntegrationTestBase(factory)
{

    [Fact]
    public async Task ListAudit_ShouldReturnPaged()
    {
        await AuthenticateAsync();
        var response = await _client.GetAsync("/api/v1/audit?page=1&pageSize=10");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PagedResult<AuditLogDto>>();
        result.Should().NotBeNull();
        result!.Page.Should().Be(1);
    }

    [Fact]
    public async Task GetAuditById_NotFound_ShouldReturn404()
    {
        await AuthenticateAsync();
        var response = await _client.GetAsync($"/api/v1/audit/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ListEntityChanges_ShouldReturnPaged()
    {
        await AuthenticateAsync();
        var response = await _client.GetAsync("/api/v1/entity-changes?entityType=User&entityId=" + Guid.NewGuid() + "&page=1&pageSize=10");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PagedResult<OperationDto>>();
        result.Should().NotBeNull();
        result!.Page.Should().Be(1);
    }

    [Fact]
    public async Task ListAllEntityChanges_ShouldReturnPaged()
    {
        await AuthenticateAsync();
        var response = await _client.GetAsync("/api/v1/entity-changes/all?page=1&pageSize=10");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PagedResult<GlobalOperationDto>>();
        result.Should().NotBeNull();
        result!.Page.Should().Be(1);
    }

    [Fact]
    public async Task ListAudit_WithFilters_ShouldReturnFiltered()
    {
        await AuthenticateAsync();
        var response = await _client.GetAsync(
            "/api/v1/audit?page=1&pageSize=10&isSuccess=true&method=POST&q=admin");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PagedResult<AuditLogDto>>();
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task ListAllEntityChanges_WithFilters_ShouldReturn200()
    {
        await AuthenticateAsync();
        var response = await _client.GetAsync(
            "/api/v1/entity-changes/all?page=1&pageSize=10&entityType=Client&changeType=Create");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PagedResult<GlobalOperationDto>>();
        result.Should().NotBeNull();
    }
}
