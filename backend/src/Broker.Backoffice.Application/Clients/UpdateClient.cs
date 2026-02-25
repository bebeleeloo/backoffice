using System.Text.Json;
using Broker.Backoffice.Application.Abstractions;
using Broker.Backoffice.Domain.Clients;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Broker.Backoffice.Application.Clients;

public sealed record UpdateClientCommand(
    Guid Id,
    ClientType ClientType,
    string? ExternalId,
    ClientStatus Status,
    string Email,
    string? Phone,
    string? PreferredLanguage,
    string? TimeZone,
    Guid? ResidenceCountryId,
    Guid? CitizenshipCountryId,
    bool PepStatus,
    RiskLevel? RiskLevel,
    KycStatus KycStatus,
    DateTime? KycReviewedAtUtc,
    string? FirstName,
    string? LastName,
    string? MiddleName,
    DateOnly? DateOfBirth,
    Gender? Gender,
    MaritalStatus? MaritalStatus,
    Education? Education,
    string? Ssn,
    string? PassportNumber,
    string? DriverLicenseNumber,
    string? CompanyName,
    string? RegistrationNumber,
    string? TaxId,
    List<CreateClientAddressDto> Addresses,
    CreateInvestmentProfileDto? InvestmentProfile,
    byte[] RowVersion) : IRequest<ClientDto>;

public sealed class UpdateClientCommandValidator : AbstractValidator<UpdateClientCommand>
{
    public UpdateClientCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(200);
        RuleFor(x => x.RowVersion).NotEmpty();
        RuleFor(x => x.ExternalId).MaximumLength(64);
        RuleFor(x => x.Phone).MaximumLength(32);
        RuleFor(x => x.PreferredLanguage).MaximumLength(10);
        RuleFor(x => x.TimeZone).MaximumLength(64);
        RuleFor(x => x.Ssn).MaximumLength(20);
        RuleFor(x => x.PassportNumber).MaximumLength(30);
        RuleFor(x => x.DriverLicenseNumber).MaximumLength(30);

        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100)
            .When(x => x.ClientType == ClientType.Individual);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100)
            .When(x => x.ClientType == ClientType.Individual);
        RuleFor(x => x.MiddleName).MaximumLength(100);

        RuleFor(x => x.CompanyName).NotEmpty().MaximumLength(200)
            .When(x => x.ClientType == ClientType.Corporate);
        RuleFor(x => x.RegistrationNumber).MaximumLength(64);
        RuleFor(x => x.TaxId).MaximumLength(64);

        RuleFor(x => x.DateOfBirth)
            .LessThanOrEqualTo(DateOnly.FromDateTime(DateTime.UtcNow))
            .When(x => x.DateOfBirth.HasValue)
            .WithMessage("Date of birth cannot be in the future");

        RuleForEach(x => x.Addresses).ChildRules(a =>
        {
            a.RuleFor(x => x.Line1).NotEmpty().MaximumLength(200);
            a.RuleFor(x => x.City).NotEmpty().MaximumLength(100);
            a.RuleFor(x => x.CountryId).NotEmpty();
            a.RuleFor(x => x.Line2).MaximumLength(200);
            a.RuleFor(x => x.State).MaximumLength(100);
            a.RuleFor(x => x.PostalCode).MaximumLength(20);
        });

        When(x => x.InvestmentProfile is not null, () =>
        {
            RuleFor(x => x.InvestmentProfile!.Notes).MaximumLength(2000);
        });
    }
}

public sealed class UpdateClientCommandHandler(
    IAppDbContext db,
    IDateTimeProvider clock,
    ICurrentUser currentUser,
    IAuditContext audit) : IRequestHandler<UpdateClientCommand, ClientDto>
{
    public async Task<ClientDto> Handle(UpdateClientCommand request, CancellationToken ct)
    {
        var client = await db.Clients
            .Include(c => c.Addresses)
            .Include(c => c.InvestmentProfile)
            .FirstOrDefaultAsync(c => c.Id == request.Id, ct)
            ?? throw new KeyNotFoundException($"Client {request.Id} not found");

        if (await db.Clients.AnyAsync(c => c.Email == request.Email && c.Id != request.Id, ct))
            throw new InvalidOperationException($"Client with email '{request.Email}' already exists");

        var before = JsonSerializer.Serialize(new { client.Id, client.Email, client.ClientType, client.Status });
        db.Clients.Entry(client).Property(c => c.RowVersion).OriginalValue = request.RowVersion;

        client.ClientType = request.ClientType;
        client.ExternalId = request.ExternalId;
        client.Status = request.Status;
        client.Email = request.Email;
        client.Phone = request.Phone;
        client.PreferredLanguage = request.PreferredLanguage;
        client.TimeZone = request.TimeZone;
        client.ResidenceCountryId = request.ResidenceCountryId;
        client.CitizenshipCountryId = request.CitizenshipCountryId;
        client.PepStatus = request.PepStatus;
        client.RiskLevel = request.RiskLevel;
        client.KycStatus = request.KycStatus;
        client.KycReviewedAtUtc = request.KycReviewedAtUtc;
        client.FirstName = request.FirstName;
        client.LastName = request.LastName;
        client.MiddleName = request.MiddleName;
        client.DateOfBirth = request.DateOfBirth;
        client.Gender = request.Gender;
        client.MaritalStatus = request.MaritalStatus;
        client.Education = request.Education;
        client.Ssn = request.Ssn;
        client.PassportNumber = request.PassportNumber;
        client.DriverLicenseNumber = request.DriverLicenseNumber;
        client.CompanyName = request.CompanyName;
        client.RegistrationNumber = request.RegistrationNumber;
        client.TaxId = request.TaxId;
        client.UpdatedAt = clock.UtcNow;
        client.UpdatedBy = currentUser.UserName;

        // Two-step save within a transaction for atomicity:
        // Step 1 checks RowVersion concurrency; step 2 replaces addresses + investment profile.
        await using var tx = await db.Database.BeginTransactionAsync(ct);

        await db.SaveChangesAsync(ct);

        // Replace addresses
        db.ClientAddresses.RemoveRange(client.Addresses);
        foreach (var addr in request.Addresses)
        {
            db.ClientAddresses.Add(new ClientAddress
            {
                Id = Guid.NewGuid(),
                ClientId = client.Id,
                Type = addr.Type,
                Line1 = addr.Line1,
                Line2 = addr.Line2,
                City = addr.City,
                State = addr.State,
                PostalCode = addr.PostalCode,
                CountryId = addr.CountryId!.Value
            });
        }

        // Upsert investment profile
        if (request.InvestmentProfile is { } ip)
        {
            if (client.InvestmentProfile is not null)
            {
                client.InvestmentProfile.Objective = ip.Objective;
                client.InvestmentProfile.RiskTolerance = ip.RiskTolerance;
                client.InvestmentProfile.LiquidityNeeds = ip.LiquidityNeeds;
                client.InvestmentProfile.TimeHorizon = ip.TimeHorizon;
                client.InvestmentProfile.Knowledge = ip.Knowledge;
                client.InvestmentProfile.Experience = ip.Experience;
                client.InvestmentProfile.Notes = ip.Notes;
            }
            else
            {
                db.InvestmentProfiles.Add(new InvestmentProfile
                {
                    Id = Guid.NewGuid(),
                    ClientId = client.Id,
                    Objective = ip.Objective,
                    RiskTolerance = ip.RiskTolerance,
                    LiquidityNeeds = ip.LiquidityNeeds,
                    TimeHorizon = ip.TimeHorizon,
                    Knowledge = ip.Knowledge,
                    Experience = ip.Experience,
                    Notes = ip.Notes
                });
            }
        }
        else if (client.InvestmentProfile is not null)
        {
            db.InvestmentProfiles.Remove(client.InvestmentProfile);
        }

        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        // Reload for response
        var updated = await db.Clients
            .Include(c => c.ResidenceCountry)
            .Include(c => c.CitizenshipCountry)
            .Include(c => c.Addresses).ThenInclude(a => a.Country)
            .Include(c => c.InvestmentProfile)
            .FirstAsync(c => c.Id == client.Id, ct);

        audit.EntityType = "Client";
        audit.EntityId = client.Id.ToString();
        audit.BeforeJson = before;
        audit.AfterJson = JsonSerializer.Serialize(new { updated.Id, updated.Email, updated.ClientType, updated.Status });

        return GetClientByIdQueryHandler.ToDto(updated);
    }
}
