using Broker.Backoffice.Domain.Common;
using Broker.Backoffice.Domain.Countries;

namespace Broker.Backoffice.Domain.Clients;

public sealed class Client : AuditableEntity
{
    public ClientType ClientType { get; set; }
    public string? ExternalId { get; set; }
    public ClientStatus Status { get; set; }
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? PreferredLanguage { get; set; }
    public string? TimeZone { get; set; }

    public Guid? ResidenceCountryId { get; set; }
    public Country? ResidenceCountry { get; set; }
    public Guid? CitizenshipCountryId { get; set; }
    public Country? CitizenshipCountry { get; set; }

    public bool PepStatus { get; set; }
    public RiskLevel? RiskLevel { get; set; }
    public KycStatus KycStatus { get; set; }
    public DateTime? KycReviewedAtUtc { get; set; }

    // Individual fields
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? MiddleName { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    public Gender? Gender { get; set; }
    public MaritalStatus? MaritalStatus { get; set; }
    public Education? Education { get; set; }

    // Identity documents
    public string? Ssn { get; set; }
    public string? PassportNumber { get; set; }
    public string? DriverLicenseNumber { get; set; }

    // Corporate fields
    public string? CompanyName { get; set; }
    public string? RegistrationNumber { get; set; }
    public string? TaxId { get; set; }

    public ICollection<ClientAddress> Addresses { get; set; } = [];
    public InvestmentProfile? InvestmentProfile { get; set; }
}
