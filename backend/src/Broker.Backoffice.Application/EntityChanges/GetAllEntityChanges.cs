using Broker.Backoffice.Application.Abstractions;
using Broker.Backoffice.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Broker.Backoffice.Application.EntityChanges;

public sealed record GetAllEntityChangesQuery : PagedQuery, IRequest<PagedResult<GlobalOperationDto>>
{
    public DateTime? From { get; init; }
    public DateTime? To { get; init; }
    public string[]? UserName { get; init; }
    public string? EntityType { get; init; }
    public string? ChangeType { get; init; }
}

public sealed class GetAllEntityChangesQueryHandler(IAppDbContext db)
    : IRequestHandler<GetAllEntityChangesQuery, PagedResult<GlobalOperationDto>>
{
    public async Task<PagedResult<GlobalOperationDto>> Handle(
        GetAllEntityChangesQuery request, CancellationToken ct)
    {
        var query = db.EntityChanges.AsQueryable();

        // Apply filters on raw rows before grouping
        if (request.From.HasValue)
            query = query.Where(e => e.Timestamp >= request.From.Value);
        if (request.To.HasValue)
            query = query.Where(e => e.Timestamp <= request.To.Value);
        if (request.UserName is { Length: > 0 })
            query = query.Where(e => e.UserName != null && request.UserName.Contains(e.UserName));
        if (!string.IsNullOrWhiteSpace(request.EntityType))
            query = query.Where(e => e.EntityType == request.EntityType);
        if (!string.IsNullOrWhiteSpace(request.Q))
            query = query.Where(e =>
                (e.UserName != null && e.UserName.Contains(request.Q)) ||
                e.EntityType.Contains(request.Q) ||
                (e.EntityDisplayName != null && e.EntityDisplayName.Contains(request.Q)));

        // Filter by change type: only include operations that have this change type
        if (!string.IsNullOrWhiteSpace(request.ChangeType))
        {
            var matchingOpIds = db.EntityChanges
                .Where(e => e.ChangeType == request.ChangeType)
                .Select(e => e.OperationId)
                .Distinct();
            query = query.Where(e => matchingOpIds.Contains(e.OperationId));
        }

        // Group into operations
        var grouped = query
            .GroupBy(e => new { e.OperationId, e.EntityType, e.EntityId })
            .Select(g => new
            {
                g.Key.OperationId,
                g.Key.EntityType,
                g.Key.EntityId,
                Timestamp = g.Min(e => e.Timestamp),
                EntityDisplayName = g.Max(e => e.EntityDisplayName),
                UserName = g.Max(e => e.UserName),
            });

        // Apply sorting
        var desc = string.IsNullOrWhiteSpace(request.Sort) || request.Sort.StartsWith('-');
        var sortField = string.IsNullOrWhiteSpace(request.Sort)
            ? "Timestamp"
            : (request.Sort.StartsWith('-') ? request.Sort[1..] : request.Sort);

        var ordered = sortField.ToLowerInvariant() switch
        {
            "entitydisplayname" => desc
                ? grouped.OrderByDescending(x => x.EntityDisplayName)
                : grouped.OrderBy(x => x.EntityDisplayName),
            "username" => desc
                ? grouped.OrderByDescending(x => x.UserName)
                : grouped.OrderBy(x => x.UserName),
            "entitytype" => desc
                ? grouped.OrderByDescending(x => x.EntityType)
                : grouped.OrderBy(x => x.EntityType),
            _ => desc
                ? grouped.OrderByDescending(x => x.Timestamp)
                : grouped.OrderBy(x => x.Timestamp),
        };

        var totalCount = await ordered.CountAsync(ct);

        var operations = await ordered
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(ct);

        if (operations.Count == 0)
        {
            return new PagedResult<GlobalOperationDto>
            {
                Items = [],
                TotalCount = totalCount,
                Page = request.Page,
                PageSize = request.PageSize
            };
        }

        var operationIds = operations.Select(o => o.OperationId).ToList();

        // Load all changes for these operations
        var changes = await db.EntityChanges
            .Where(e => operationIds.Contains(e.OperationId))
            .OrderBy(e => e.Timestamp)
            .ToListAsync(ct);

        // Group into DTOs
        var items = operations.Select(op =>
        {
            var opChanges = changes
                .Where(c => c.OperationId == op.OperationId
                             && c.EntityType == op.EntityType
                             && c.EntityId == op.EntityId)
                .ToList();

            if (opChanges.Count == 0)
                return null;

            var first = opChanges.First();

            var groups = opChanges
                .GroupBy(c => (c.RelatedEntityType, c.RelatedEntityId))
                .Select(g => new EntityChangeGroupDto(
                    g.Key.RelatedEntityType,
                    g.Key.RelatedEntityId,
                    g.First().RelatedEntityDisplayName,
                    g.First().ChangeType,
                    g.Select(c => new FieldChangeDto(
                        c.FieldName, c.ChangeType, c.OldValue, c.NewValue))
                    .ToList()))
                .ToList();

            var changeTypes = opChanges.Select(c => c.ChangeType).Distinct().ToList();
            var operationChangeType = changeTypes.Count == 1 ? changeTypes[0] : "Modified";

            return new GlobalOperationDto(
                op.OperationId,
                op.Timestamp,
                first.UserId,
                first.UserName,
                op.EntityType,
                op.EntityId,
                first.EntityDisplayName,
                operationChangeType,
                groups);
        }).Where(x => x != null).ToList();

        return new PagedResult<GlobalOperationDto>
        {
            Items = items!,
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize
        };
    }
}
