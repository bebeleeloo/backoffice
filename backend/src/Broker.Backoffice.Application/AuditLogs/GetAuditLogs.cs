using Broker.Backoffice.Application.Abstractions;
using Broker.Backoffice.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Broker.Backoffice.Application.AuditLogs;

public sealed record GetAuditLogsQuery : PagedQuery, IRequest<PagedResult<AuditLogDto>>
{
    public DateTime? From { get; init; }
    public DateTime? To { get; init; }
    public Guid? UserId { get; init; }
    public string? Action { get; init; }
    public string? EntityType { get; init; }
    public bool? IsSuccess { get; init; }
    public string? UserName { get; init; }
    public string? Method { get; init; }
    public string? Path { get; init; }
    public int? StatusCode { get; init; }
}

public sealed class GetAuditLogsQueryHandler(IAppDbContext db)
    : IRequestHandler<GetAuditLogsQuery, PagedResult<AuditLogDto>>
{
    public async Task<PagedResult<AuditLogDto>> Handle(GetAuditLogsQuery request, CancellationToken ct)
    {
        var query = db.AuditLogs.AsQueryable();

        if (request.From.HasValue) query = query.Where(a => a.CreatedAt >= request.From.Value);
        if (request.To.HasValue) query = query.Where(a => a.CreatedAt <= request.To.Value);
        if (request.UserId.HasValue) query = query.Where(a => a.UserId == request.UserId.Value);
        if (!string.IsNullOrWhiteSpace(request.Action)) query = query.Where(a => a.Action.Contains(request.Action));
        if (!string.IsNullOrWhiteSpace(request.EntityType)) query = query.Where(a => a.EntityType == request.EntityType);
        if (request.IsSuccess.HasValue) query = query.Where(a => a.IsSuccess == request.IsSuccess.Value);
        if (!string.IsNullOrWhiteSpace(request.UserName)) query = query.Where(a => a.UserName != null && a.UserName.Contains(request.UserName));
        if (!string.IsNullOrWhiteSpace(request.Method)) query = query.Where(a => a.Method == request.Method);
        if (!string.IsNullOrWhiteSpace(request.Path)) query = query.Where(a => a.Path.Contains(request.Path));
        if (request.StatusCode.HasValue) query = query.Where(a => a.StatusCode == request.StatusCode.Value);

        if (!string.IsNullOrWhiteSpace(request.Q))
            query = query.Where(a =>
                (a.UserName != null && a.UserName.Contains(request.Q)) ||
                a.Action.Contains(request.Q) ||
                a.Path.Contains(request.Q));

        var projected = query.SortBy(request.Sort ?? "-CreatedAt")
            .Select(a => new AuditLogDto(
                a.Id, a.UserId, a.UserName, a.Action, a.EntityType, a.EntityId,
                a.BeforeJson, a.AfterJson, a.CorrelationId, a.IpAddress,
                a.Path, a.Method, a.StatusCode, a.IsSuccess, a.CreatedAt));

        return await projected.ToPagedResultAsync(request.Page, request.PageSize, ct);
    }
}
