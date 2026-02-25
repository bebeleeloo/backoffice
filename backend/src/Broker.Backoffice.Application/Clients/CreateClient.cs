using System.Text.Json;
using Broker.Backoffice.Application.Abstractions;
using Broker.Backoffice.Domain.Clients;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Broker.Backoffice.Application.Clients;

public sealed record CreateClientCommand(
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
    CreateInvestmentProfileDto? InvestmentProfile) : IRequest<ClientDto>;

public sealed record CreateClientAddressDto(
    AddressType Type,
    string Line1,
    string? Line2,
    string City,
    string? State,
    string? PostalCode,
    Guid? CountryId);

public sealed class CreateClientCommandValidator : AbstractValidator<CreateClientCommand>
{
    public CreateClientCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(200);
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

public sealed class CreateClientCommandHandler(
    IAppDbContext db,
    IDateTimeProvider clock,
    ICurrentUser currentUser,
    IAuditContext audit) : IRequestHandler<CreateClientCommand, ClientDto>
{
    public async Task<ClientDto> Handle(CreateClientCommand request, CancellationToken ct)
    {
        if (await db.Clients.AnyAsync(c => c.Email == request.Email, ct))
            throw new InvalidOperationException($"Client with email '{request.Email}' already exists");

        var client = new Client
        {
            Id = Guid.NewGuid(),
            ClientType = request.ClientType,
            ExternalId = request.ExternalId,
            Status = request.Status,
            Email = request.Email,
            Phone = request.Phone,
            PreferredLanguage = request.PreferredLanguage,
            TimeZone = request.TimeZone,
            ResidenceCountryId = request.ResidenceCountryId,
            CitizenshipCountryId = request.CitizenshipCountryId,
            PepStatus = request.PepStatus,
            RiskLevel = request.RiskLevel,
            KycStatus = request.KycStatus,
            FirstName = request.FirstName,
            LastName = request.LastName,
            MiddleName = request.MiddleName,
            DateOfBirth = request.DateOfBirth,
            Gender = request.Gender,
            MaritalStatus = request.MaritalStatus,
            Education = request.Education,
            Ssn = request.Ssn,
            PassportNumber = request.PassportNumber,
            DriverLicenseNumber = request.DriverLicenseNumber,
            CompanyName = request.CompanyName,
            RegistrationNumber = request.RegistrationNumber,
            TaxId = request.TaxId,
            CreatedAt = clock.UtcNow,
            CreatedBy = currentUser.UserName
        };

        foreach (var addr in request.Addresses)
        {
            client.Addresses.Add(new ClientAddress
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

        if (request.InvestmentProfile is { } ip)
        {
            client.InvestmentProfile = new InvestmentProfile
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
            };
        }

        db.Clients.Add(client);
        await db.SaveChangesAsync(ct);

        // Reload navigations for response
        await db.Clients.Entry(client).Reference(c => c.ResidenceCountry).LoadAsync(ct);
        await db.Clients.Entry(client).Reference(c => c.CitizenshipCountry).LoadAsync(ct);
        foreach (var addr in client.Addresses)
            await db.ClientAddresses.Entry(addr).Reference(a => a.Country).LoadAsync(ct);

        audit.EntityType = "Client";
        audit.EntityId = client.Id.ToString();
        audit.AfterJson = JsonSerializer.Serialize(new { client.Id, client.Email, client.ClientType });

        return GetClientByIdQueryHandler.ToDto(client);
    }
}
