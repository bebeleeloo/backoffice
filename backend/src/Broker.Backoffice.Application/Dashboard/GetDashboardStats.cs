using Broker.Backoffice.Application.Abstractions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Broker.Backoffice.Application.Dashboard;

public sealed record DashboardStatsDto
{
    public int TotalClients { get; init; }
    public Dictionary<string, int> ClientsByStatus { get; init; } = new();
    public Dictionary<string, int> ClientsByType { get; init; } = new();

    public int TotalAccounts { get; init; }
    public Dictionary<string, int> AccountsByStatus { get; init; } = new();
    public Dictionary<string, int> AccountsByType { get; init; } = new();

    public int TotalInstruments { get; init; }
    public Dictionary<string, int> InstrumentsByStatus { get; init; } = new();
    public Dictionary<string, int> InstrumentsByType { get; init; } = new();

    public int TotalUsers { get; init; }
    public int ActiveUsers { get; init; }
}

public sealed record GetDashboardStatsQuery : IRequest<DashboardStatsDto>;

public sealed class GetDashboardStatsQueryHandler(IAppDbContext db)
    : IRequestHandler<GetDashboardStatsQuery, DashboardStatsDto>
{
    public async Task<DashboardStatsDto> Handle(GetDashboardStatsQuery request, CancellationToken ct)
    {
        var clientsByStatus = await db.Clients
            .GroupBy(c => c.Status)
            .Select(g => new { Key = g.Key.ToString(), Count = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Count, ct);

        var clientsByType = await db.Clients
            .GroupBy(c => c.ClientType)
            .Select(g => new { Key = g.Key.ToString(), Count = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Count, ct);

        var accountsByStatus = await db.Accounts
            .GroupBy(a => a.Status)
            .Select(g => new { Key = g.Key.ToString(), Count = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Count, ct);

        var accountsByType = await db.Accounts
            .GroupBy(a => a.AccountType)
            .Select(g => new { Key = g.Key.ToString(), Count = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Count, ct);

        var instrumentsByStatus = await db.Instruments
            .GroupBy(i => i.Status)
            .Select(g => new { Key = g.Key.ToString(), Count = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Count, ct);

        var instrumentsByType = await db.Instruments
            .GroupBy(i => i.Type)
            .Select(g => new { Key = g.Key.ToString(), Count = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Count, ct);

        var totalUsers = await db.Users.CountAsync(ct);
        var activeUsers = await db.Users.CountAsync(u => u.IsActive, ct);

        return new DashboardStatsDto
        {
            TotalClients = clientsByStatus.Values.Sum(),
            ClientsByStatus = clientsByStatus,
            ClientsByType = clientsByType,
            TotalAccounts = accountsByStatus.Values.Sum(),
            AccountsByStatus = accountsByStatus,
            AccountsByType = accountsByType,
            TotalInstruments = instrumentsByStatus.Values.Sum(),
            InstrumentsByStatus = instrumentsByStatus,
            InstrumentsByType = instrumentsByType,
            TotalUsers = totalUsers,
            ActiveUsers = activeUsers,
        };
    }
}
