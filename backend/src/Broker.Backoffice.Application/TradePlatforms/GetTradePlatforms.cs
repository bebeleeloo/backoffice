using Broker.Backoffice.Application.Abstractions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Broker.Backoffice.Application.TradePlatforms;

public sealed record TradePlatformDto(Guid Id, string Name, string? Description, bool IsActive);

public sealed record GetTradePlatformsQuery : IRequest<IReadOnlyList<TradePlatformDto>>;

public sealed class GetTradePlatformsQueryHandler(IAppDbContext db)
    : IRequestHandler<GetTradePlatformsQuery, IReadOnlyList<TradePlatformDto>>
{
    public async Task<IReadOnlyList<TradePlatformDto>> Handle(GetTradePlatformsQuery request, CancellationToken ct)
    {
        return await db.TradePlatforms
            .Where(t => t.IsActive)
            .OrderBy(t => t.Name)
            .Select(t => new TradePlatformDto(t.Id, t.Name, t.Description, t.IsActive))
            .ToListAsync(ct);
    }
}
