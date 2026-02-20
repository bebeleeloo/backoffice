using Broker.Backoffice.Domain.Clients;

namespace Broker.Backoffice.Application.Clients;

public sealed record ClientDto(
    Guid Id,
    ClientType ClientType,
    string? ExternalId,
    ClientStatus Status,
    string Email,
    string? Phone,
    string? PreferredLanguage,
    string? TimeZone,
    Guid? ResidenceCountryId,
    string? ResidenceCountryIso2,
    string? ResidenceCountryName,
    string? ResidenceCountryFlagEmoji,
    Guid? CitizenshipCountryId,
    string? CitizenshipCountryIso2,
    string? CitizenshipCountryName,
    string? CitizenshipCountryFlagEmoji,
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
    DateTime CreatedAt,
    byte[] RowVersion,
    IReadOnlyList<ClientAddressDto> Addresses,
    InvestmentProfileDto? InvestmentProfile);

public sealed record ClientAddressDto(
    Guid Id,
    AddressType Type,
    string Line1,
    string? Line2,
    string City,
    string? State,
    string? PostalCode,
    Guid CountryId,
    string CountryIso2,
    string CountryName,
    string CountryFlagEmoji);

public sealed record ClientListItemDto(
    Guid Id,
    ClientType ClientType,
    string DisplayName,
    string Email,
    ClientStatus Status,
    KycStatus KycStatus,
    string? ResidenceCountryIso2,
    string? ResidenceCountryFlagEmoji,
    DateTime CreatedAt,
    byte[] RowVersion,
    string? Phone,
    string? ExternalId,
    bool PepStatus,
    RiskLevel? RiskLevel,
    string? ResidenceCountryName,
    string? CitizenshipCountryIso2,
    string? CitizenshipCountryFlagEmoji,
    string? CitizenshipCountryName);

public sealed record InvestmentProfileDto(
    Guid Id,
    InvestmentObjective? Objective,
    InvestmentRiskTolerance? RiskTolerance,
    LiquidityNeeds? LiquidityNeeds,
    InvestmentTimeHorizon? TimeHorizon,
    InvestmentKnowledge? Knowledge,
    InvestmentExperience? Experience,
    string? Notes);

public sealed record CreateInvestmentProfileDto(
    InvestmentObjective? Objective,
    InvestmentRiskTolerance? RiskTolerance,
    LiquidityNeeds? LiquidityNeeds,
    InvestmentTimeHorizon? TimeHorizon,
    InvestmentKnowledge? Knowledge,
    InvestmentExperience? Experience,
    string? Notes);
