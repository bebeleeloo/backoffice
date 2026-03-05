using System.Net.Http.Json;
using Broker.Backoffice.Application.Abstractions;

namespace Broker.Backoffice.Infrastructure.Services;

public sealed class AuthServiceClient(HttpClient httpClient) : IAuthServiceClient
{
    public async Task<UserStatsDto> GetUserStatsAsync(CancellationToken ct = default)
    {
        var stats = await httpClient.GetFromJsonAsync<UserStatsDto>("/api/v1/users/stats", ct);
        return stats ?? new UserStatsDto(0, 0);
    }
}
