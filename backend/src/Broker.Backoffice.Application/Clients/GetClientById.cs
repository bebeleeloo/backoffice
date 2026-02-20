using Broker.Backoffice.Application.Abstractions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Broker.Backoffice.Application.Clients;

public sealed record GetClientByIdQuery(Guid Id) : IRequest<ClientDto>;

public sealed class GetClientByIdQueryHandler(IAppDbContext db) : IRequestHandler<GetClientByIdQuery, ClientDto>
{
    public async Task<ClientDto> Handle(GetClientByIdQuery request, CancellationToken ct)
    {
        var c = await db.Clients
            .Include(x => x.ResidenceCountry)
            .Include(x => x.CitizenshipCountry)
            .Include(x => x.Addresses).ThenInclude(a => a.Country)
            .Include(x => x.InvestmentProfile)
            .FirstOrDefaultAsync(x => x.Id == request.Id, ct)
            ?? throw new KeyNotFoundException($"Client {request.Id} not found");

        return ToDto(c);
    }

    internal static ClientDto ToDto(Domain.Clients.Client c) => new(
        c.Id, c.ClientType, c.ExternalId, c.Status,
        c.Email, c.Phone, c.PreferredLanguage, c.TimeZone,
        c.ResidenceCountryId, c.ResidenceCountry?.Iso2, c.ResidenceCountry?.Name, c.ResidenceCountry?.FlagEmoji,
        c.CitizenshipCountryId, c.CitizenshipCountry?.Iso2, c.CitizenshipCountry?.Name, c.CitizenshipCountry?.FlagEmoji,
        c.PepStatus, c.RiskLevel, c.KycStatus, c.KycReviewedAtUtc,
        c.FirstName, c.LastName, c.MiddleName, c.DateOfBirth, c.Gender,
        c.MaritalStatus, c.Education,
        c.Ssn, c.PassportNumber, c.DriverLicenseNumber,
        c.CompanyName, c.RegistrationNumber, c.TaxId,
        c.CreatedAt, c.RowVersion,
        c.Addresses.Select(a => new ClientAddressDto(
            a.Id, a.Type, a.Line1, a.Line2, a.City, a.State, a.PostalCode,
            a.CountryId, a.Country.Iso2, a.Country.Name, a.Country.FlagEmoji
        )).ToList(),
        c.InvestmentProfile is null ? null : new InvestmentProfileDto(
            c.InvestmentProfile.Id,
            c.InvestmentProfile.Objective,
            c.InvestmentProfile.RiskTolerance,
            c.InvestmentProfile.LiquidityNeeds,
            c.InvestmentProfile.TimeHorizon,
            c.InvestmentProfile.Knowledge,
            c.InvestmentProfile.Experience,
            c.InvestmentProfile.Notes));
}
