using Broker.Backoffice.Application.Abstractions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Broker.Backoffice.Application.Exchanges;

public sealed record GetAllExchangesQuery : IRequest<IReadOnlyList<ExchangeDto>>;

public sealed class GetAllExchangesQueryHandler(IAppDbContext db)
    : IRequestHandler<GetAllExchangesQuery, IReadOnlyList<ExchangeDto>>
{
    public async Task<IReadOnlyList<ExchangeDto>> Handle(GetAllExchangesQuery request, CancellationToken ct)
    {
        return await db.Exchanges
            .OrderBy(e => e.Code)
            .Select(e => new ExchangeDto(e.Id, e.Code, e.Name, e.CountryId, e.IsActive))
            .ToListAsync(ct);
    }
}
