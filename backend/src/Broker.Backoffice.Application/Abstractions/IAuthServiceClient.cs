namespace Broker.Backoffice.Application.Abstractions;

public sealed record UserStatsDto(int TotalUsers, int ActiveUsers);

public interface IAuthServiceClient
{
    Task<UserStatsDto> GetUserStatsAsync(CancellationToken ct = default);
}
