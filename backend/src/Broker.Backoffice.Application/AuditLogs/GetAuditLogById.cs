using Broker.Backoffice.Application.Abstractions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Broker.Backoffice.Application.AuditLogs;

public sealed record GetAuditLogByIdQuery(Guid Id) : IRequest<AuditLogDto>;

public sealed class GetAuditLogByIdQueryHandler(IAppDbContext db)
    : IRequestHandler<GetAuditLogByIdQuery, AuditLogDto>
{
    public async Task<AuditLogDto> Handle(GetAuditLogByIdQuery request, CancellationToken ct)
    {
        var a = await db.AuditLogs.FirstOrDefaultAsync(x => x.Id == request.Id, ct)
            ?? throw new KeyNotFoundException($"AuditLog {request.Id} not found");

        return new AuditLogDto(a.Id, a.UserId, a.UserName, a.Action, a.EntityType, a.EntityId,
            a.BeforeJson, a.AfterJson, a.CorrelationId, a.IpAddress,
            a.Path, a.Method, a.StatusCode, a.IsSuccess, a.CreatedAt);
    }
}
