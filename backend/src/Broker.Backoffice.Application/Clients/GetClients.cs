using Broker.Backoffice.Application.Abstractions;
using Broker.Backoffice.Application.Common;
using Broker.Backoffice.Domain.Clients;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Broker.Backoffice.Application.Clients;

public sealed record GetClientsQuery : PagedQuery, IRequest<PagedResult<ClientListItemDto>>
{
    // Per-column text filters
    public string? Name { get; init; }
    public string? Email { get; init; }
    public string? Phone { get; init; }
    public string? ExternalId { get; init; }
    public string? ResidenceCountryName { get; init; }
    public string? CitizenshipCountryName { get; init; }

    // Multi-value enum filters
    public List<ClientStatus>? Status { get; init; }
    public List<ClientType>? ClientType { get; init; }
    public List<KycStatus>? KycStatus { get; init; }
    public List<RiskLevel>? RiskLevel { get; init; }

    // Country ID filters
    public Guid? ResidenceCountryId { get; init; }
    public List<Guid>? ResidenceCountryIds { get; init; }
    public List<Guid>? CitizenshipCountryIds { get; init; }

    // Date range
    public DateTime? CreatedFrom { get; init; }
    public DateTime? CreatedTo { get; init; }

    // Boolean
    public bool? PepStatus { get; init; }
}

public sealed class GetClientsQueryHandler(IAppDbContext db)
    : IRequestHandler<GetClientsQuery, PagedResult<ClientListItemDto>>
{
    public async Task<PagedResult<ClientListItemDto>> Handle(GetClientsQuery request, CancellationToken ct)
    {
        var query = db.Clients.AsQueryable();

        // Per-column text filters (EF.Functions.Like for SQL LIKE)
        if (!string.IsNullOrWhiteSpace(request.Name))
        {
            var namePattern = LikeHelper.ContainsPattern(request.Name);
            query = query.Where(c =>
                (c.FirstName != null && EF.Functions.Like(c.FirstName, namePattern)) ||
                (c.LastName != null && EF.Functions.Like(c.LastName, namePattern)) ||
                (c.CompanyName != null && EF.Functions.Like(c.CompanyName, namePattern)) ||
                EF.Functions.Like((c.FirstName ?? "") + " " + (c.LastName ?? ""), namePattern));
        }

        if (!string.IsNullOrWhiteSpace(request.Email))
        {
            var emailPattern = LikeHelper.ContainsPattern(request.Email);
            query = query.Where(c => EF.Functions.Like(c.Email, emailPattern));
        }

        if (!string.IsNullOrWhiteSpace(request.Phone))
        {
            var phonePattern = LikeHelper.ContainsPattern(request.Phone);
            query = query.Where(c => c.Phone != null && EF.Functions.Like(c.Phone, phonePattern));
        }

        if (!string.IsNullOrWhiteSpace(request.ExternalId))
        {
            var externalIdPattern = LikeHelper.ContainsPattern(request.ExternalId);
            query = query.Where(c => c.ExternalId != null && EF.Functions.Like(c.ExternalId, externalIdPattern));
        }

        if (!string.IsNullOrWhiteSpace(request.ResidenceCountryName))
        {
            var countryPattern = LikeHelper.ContainsPattern(request.ResidenceCountryName);
            query = query.Where(c => c.ResidenceCountry != null &&
                EF.Functions.Like(c.ResidenceCountry.Name, countryPattern));
        }

        if (!string.IsNullOrWhiteSpace(request.CitizenshipCountryName))
        {
            var countryPattern = LikeHelper.ContainsPattern(request.CitizenshipCountryName);
            query = query.Where(c => c.CitizenshipCountry != null &&
                EF.Functions.Like(c.CitizenshipCountry.Name, countryPattern));
        }

        // Global search (backward compat, not used in column filters)
        if (!string.IsNullOrWhiteSpace(request.Q))
            query = query.Where(c =>
                c.Email.Contains(request.Q) ||
                (c.FirstName != null && c.FirstName.Contains(request.Q)) ||
                (c.LastName != null && c.LastName.Contains(request.Q)) ||
                (c.CompanyName != null && c.CompanyName.Contains(request.Q)) ||
                (c.Phone != null && c.Phone.Contains(request.Q)) ||
                (c.ExternalId != null && c.ExternalId.Contains(request.Q)));

        // Multi-value enum filters
        if (request.Status is { Count: > 0 })
            query = query.Where(c => request.Status.Contains(c.Status));
        if (request.ClientType is { Count: > 0 })
            query = query.Where(c => request.ClientType.Contains(c.ClientType));
        if (request.KycStatus is { Count: > 0 })
            query = query.Where(c => request.KycStatus.Contains(c.KycStatus));
        if (request.RiskLevel is { Count: > 0 })
            query = query.Where(c => c.RiskLevel != null && request.RiskLevel.Contains(c.RiskLevel.Value));

        // Backward compat: single ResidenceCountryId fallback
        var residenceIds = request.ResidenceCountryIds is { Count: > 0 }
            ? request.ResidenceCountryIds
            : request.ResidenceCountryId.HasValue
                ? new List<Guid> { request.ResidenceCountryId.Value }
                : null;
        if (residenceIds is { Count: > 0 })
            query = query.Where(c => c.ResidenceCountryId != null && residenceIds.Contains(c.ResidenceCountryId.Value));

        if (request.CitizenshipCountryIds is { Count: > 0 })
            query = query.Where(c => c.CitizenshipCountryId != null && request.CitizenshipCountryIds.Contains(c.CitizenshipCountryId.Value));

        if (request.PepStatus.HasValue)
            query = query.Where(c => c.PepStatus == request.PepStatus.Value);

        if (request.CreatedFrom.HasValue)
            query = query.Where(c => c.CreatedAt >= request.CreatedFrom.Value);
        if (request.CreatedTo.HasValue)
            query = query.Where(c => c.CreatedAt < request.CreatedTo.Value.AddDays(1));

        var projected = query.SortBy(request.Sort ?? "-CreatedAt")
            .Select(c => new ClientListItemDto(
                c.Id,
                c.ClientType,
                c.ClientType == Domain.Clients.ClientType.Corporate
                    ? c.CompanyName ?? ""
                    : ((c.FirstName ?? "") + " " + (c.LastName ?? "")).Trim(),
                c.Email,
                c.Status,
                c.KycStatus,
                c.ResidenceCountry != null ? c.ResidenceCountry.Iso2 : null,
                c.ResidenceCountry != null ? c.ResidenceCountry.FlagEmoji : null,
                c.CreatedAt,
                c.RowVersion,
                c.Phone,
                c.ExternalId,
                c.PepStatus,
                c.RiskLevel,
                c.ResidenceCountry != null ? c.ResidenceCountry.Name : null,
                c.CitizenshipCountry != null ? c.CitizenshipCountry.Iso2 : null,
                c.CitizenshipCountry != null ? c.CitizenshipCountry.FlagEmoji : null,
                c.CitizenshipCountry != null ? c.CitizenshipCountry.Name : null));

        return await projected.ToPagedResultAsync(request.Page, request.PageSize, ct);
    }
}
