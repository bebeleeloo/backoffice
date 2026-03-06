using Broker.Auth.Application.Abstractions;
using Broker.Auth.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Broker.Auth.Application.EntityChanges;

public sealed record GetEntityChangesQuery(
    string EntityType,
    Guid EntityId,
    int Page = 1,
    int PageSize = 20) : IRequest<PagedResult<OperationDto>>;

public sealed class GetEntityChangesQueryHandler(IAuthDbContext db)
    : IRequestHandler<GetEntityChangesQuery, PagedResult<OperationDto>>
{
    public async Task<PagedResult<OperationDto>> Handle(
        GetEntityChangesQuery request, CancellationToken ct)
    {
        var entityIdStr = request.EntityId.ToString();

        var operationQuery = db.EntityChanges
            .Where(e => e.EntityType == request.EntityType && e.EntityId == entityIdStr)
            .GroupBy(e => e.OperationId)
            .Select(g => new
            {
                OperationId = g.Key,
                Timestamp = g.Min(e => e.Timestamp)
            })
            .OrderByDescending(x => x.Timestamp);

        var totalCount = await operationQuery.CountAsync(ct);

        var operations = await operationQuery
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(ct);

        if (operations.Count == 0)
        {
            return new PagedResult<OperationDto>
            {
                Items = [],
                TotalCount = totalCount,
                Page = request.Page,
                PageSize = request.PageSize
            };
        }

        var operationIds = operations.Select(o => o.OperationId).ToList();

        var changes = await db.EntityChanges
            .Where(e => operationIds.Contains(e.OperationId)
                        && e.EntityType == request.EntityType
                        && e.EntityId == entityIdStr)
            .OrderBy(e => e.Timestamp)
            .ToListAsync(ct);

        var items = operations.Select(op =>
        {
            var opChanges = changes.Where(c => c.OperationId == op.OperationId).ToList();
            var first = opChanges.First();

            var groups = opChanges
                .GroupBy(c => (c.RelatedEntityType, c.RelatedEntityId))
                .Select(g =>
                {
                    var groupChangeType = g.First().ChangeType;
                    return new EntityChangeGroupDto(
                        g.Key.RelatedEntityType,
                        g.Key.RelatedEntityId,
                        g.First().RelatedEntityDisplayName,
                        groupChangeType,
                        g.Select(c => new FieldChangeDto(
                            c.FieldName, c.ChangeType, c.OldValue, c.NewValue))
                        .ToList());
                })
                .ToList();

            var changeTypes = opChanges.Select(c => c.ChangeType).Distinct().ToList();
            var operationChangeType = changeTypes.Count == 1
                ? changeTypes[0]
                : "Modified";

            return new OperationDto(
                op.OperationId,
                op.Timestamp,
                first.UserId,
                first.UserName,
                first.EntityDisplayName,
                operationChangeType,
                groups);
        }).ToList();

        return new PagedResult<OperationDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize
        };
    }
}
