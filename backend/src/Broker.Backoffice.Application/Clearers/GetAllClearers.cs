using Broker.Backoffice.Application.Abstractions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Broker.Backoffice.Application.Clearers;

public sealed record GetAllClearersQuery : IRequest<IReadOnlyList<ClearerDto>>;

public sealed class GetAllClearersQueryHandler(IAppDbContext db)
    : IRequestHandler<GetAllClearersQuery, IReadOnlyList<ClearerDto>>
{
    public async Task<IReadOnlyList<ClearerDto>> Handle(GetAllClearersQuery request, CancellationToken ct)
    {
        return await db.Clearers
            .OrderBy(c => c.Name)
            .Select(c => new ClearerDto(c.Id, c.Name, c.Description, c.IsActive))
            .ToListAsync(ct);
    }
}
