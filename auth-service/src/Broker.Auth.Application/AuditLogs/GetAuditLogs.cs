using Broker.Auth.Application.Abstractions;
using Broker.Auth.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Broker.Auth.Application.AuditLogs;

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

public sealed class GetAuditLogsQueryHandler(IAuthDbContext db)
    : IRequestHandler<GetAuditLogsQuery, PagedResult<AuditLogDto>>
{
    public async Task<PagedResult<AuditLogDto>> Handle(GetAuditLogsQuery request, CancellationToken ct)
    {
        var query = db.AuditLogs.AsQueryable();

        if (request.From.HasValue) query = query.Where(a => a.CreatedAt >= request.From.Value);
        if (request.To.HasValue) query = query.Where(a => a.CreatedAt <= request.To.Value);
        if (request.UserId.HasValue) query = query.Where(a => a.UserId == request.UserId.Value);
        if (!string.IsNullOrWhiteSpace(request.Action))
        {
            var pattern = LikeHelper.ContainsPattern(request.Action);
            query = query.Where(a => EF.Functions.Like(a.Action, pattern));
        }
        if (!string.IsNullOrWhiteSpace(request.EntityType)) query = query.Where(a => a.EntityType == request.EntityType);
        if (request.IsSuccess.HasValue) query = query.Where(a => a.IsSuccess == request.IsSuccess.Value);
        if (!string.IsNullOrWhiteSpace(request.UserName))
        {
            var pattern = LikeHelper.ContainsPattern(request.UserName);
            query = query.Where(a => a.UserName != null && EF.Functions.Like(a.UserName, pattern));
        }
        if (!string.IsNullOrWhiteSpace(request.Method)) query = query.Where(a => a.Method == request.Method);
        if (!string.IsNullOrWhiteSpace(request.Path))
        {
            var pattern = LikeHelper.ContainsPattern(request.Path);
            query = query.Where(a => EF.Functions.Like(a.Path, pattern));
        }
        if (request.StatusCode.HasValue) query = query.Where(a => a.StatusCode == request.StatusCode.Value);

        if (!string.IsNullOrWhiteSpace(request.Q))
        {
            var qPattern = LikeHelper.ContainsPattern(request.Q);
            query = query.Where(a =>
                (a.UserName != null && EF.Functions.Like(a.UserName, qPattern)) ||
                EF.Functions.Like(a.Action, qPattern) ||
                EF.Functions.Like(a.Path, qPattern));
        }

        var projected = query.SortBy(request.Sort ?? "-CreatedAt")
            .Select(a => new AuditLogDto(
                a.Id, a.UserId, a.UserName, a.Action, a.EntityType, a.EntityId,
                a.BeforeJson, a.AfterJson, a.CorrelationId, a.IpAddress,
                a.Path, a.Method, a.StatusCode, a.IsSuccess, a.CreatedAt));

        return await projected.ToPagedResultAsync(request.Page, request.PageSize, ct);
    }
}
