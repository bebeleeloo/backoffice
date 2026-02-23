using Broker.Backoffice.Application.Abstractions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Broker.Backoffice.Application.TradePlatforms;

public sealed record GetAllTradePlatformsQuery : IRequest<IReadOnlyList<TradePlatformDto>>;

public sealed class GetAllTradePlatformsQueryHandler(IAppDbContext db)
    : IRequestHandler<GetAllTradePlatformsQuery, IReadOnlyList<TradePlatformDto>>
{
    public async Task<IReadOnlyList<TradePlatformDto>> Handle(GetAllTradePlatformsQuery request, CancellationToken ct)
    {
        return await db.TradePlatforms
            .OrderBy(t => t.Name)
            .Select(t => new TradePlatformDto(t.Id, t.Name, t.Description, t.IsActive))
            .ToListAsync(ct);
    }
}
