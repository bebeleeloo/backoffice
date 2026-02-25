using Broker.Backoffice.Application.Clients;
using Broker.Backoffice.Domain.Clients;
using FluentValidation.TestHelper;

namespace Broker.Backoffice.Tests.Unit.Validators;

public class UpdateClientValidatorTests
{
    private readonly UpdateClientCommandValidator _validator = new();

    private static UpdateClientCommand ValidCommand() => new(
        Id: Guid.NewGuid(),
        ClientType: ClientType.Individual,
        ExternalId: null,
        Status: ClientStatus.Active,
        Email: "test@example.com",
        Phone: null,
        PreferredLanguage: null,
        TimeZone: null,
        ResidenceCountryId: null,
        CitizenshipCountryId: null,
        PepStatus: false,
        RiskLevel: null,
        KycStatus: KycStatus.NotStarted,
        KycReviewedAtUtc: null,
        FirstName: "John",
        LastName: "Doe",
        MiddleName: null,
        DateOfBirth: null,
        Gender: null,
        MaritalStatus: null,
        Education: null,
        Ssn: null,
        PassportNumber: null,
        DriverLicenseNumber: null,
        CompanyName: null,
        RegistrationNumber: null,
        TaxId: null,
        Addresses: [new CreateClientAddressDto(AddressType.Legal, "123 Main St", null, "New York", null, null, Guid.NewGuid())],
        InvestmentProfile: null,
        RowVersion: [1, 2, 3]);

    [Fact]
    public void ValidCommand_ShouldPass()
    {
        var result = _validator.TestValidate(ValidCommand());
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Email_Empty_ShouldFail(string? email)
    {
        var result = _validator.TestValidate(ValidCommand() with { Email = email! });
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void Email_Invalid_ShouldFail()
    {
        var result = _validator.TestValidate(ValidCommand() with { Email = "not-an-email" });
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void Email_TooLong_ShouldFail()
    {
        var result = _validator.TestValidate(ValidCommand() with { Email = new string('a', 195) + "@test.com" });
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void RowVersion_Empty_ShouldFail()
    {
        var result = _validator.TestValidate(ValidCommand() with { RowVersion = [] });
        result.ShouldHaveValidationErrorFor(x => x.RowVersion);
    }

    [Fact]
    public void Individual_FirstName_Empty_ShouldFail()
    {
        var result = _validator.TestValidate(ValidCommand() with { FirstName = "" });
        result.ShouldHaveValidationErrorFor(x => x.FirstName);
    }

    [Fact]
    public void Individual_LastName_Empty_ShouldFail()
    {
        var result = _validator.TestValidate(ValidCommand() with { LastName = "" });
        result.ShouldHaveValidationErrorFor(x => x.LastName);
    }

    [Fact]
    public void Corporate_CompanyName_Empty_ShouldFail()
    {
        var cmd = ValidCommand() with
        {
            ClientType = ClientType.Corporate,
            CompanyName = "",
            FirstName = null,
            LastName = null,
        };
        var result = _validator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.CompanyName);
    }

    [Fact]
    public void Corporate_FirstName_Empty_ShouldNotFail()
    {
        var cmd = ValidCommand() with
        {
            ClientType = ClientType.Corporate,
            CompanyName = "Acme Corp",
            FirstName = null,
            LastName = null,
        };
        var result = _validator.TestValidate(cmd);
        result.ShouldNotHaveValidationErrorFor(x => x.FirstName);
    }

    [Fact]
    public void Address_Line1_Empty_ShouldFail()
    {
        var cmd = ValidCommand() with
        {
            Addresses = [new CreateClientAddressDto(AddressType.Legal, "", null, "City", null, null, Guid.NewGuid())]
        };
        var result = _validator.TestValidate(cmd);
        result.ShouldHaveAnyValidationError();
    }

    [Fact]
    public void InvestmentProfile_Notes_TooLong_ShouldFail()
    {
        var cmd = ValidCommand() with
        {
            InvestmentProfile = new CreateInvestmentProfileDto(null, null, null, null, null, null, new string('x', 2001))
        };
        var result = _validator.TestValidate(cmd);
        result.ShouldHaveAnyValidationError();
    }

    [Fact]
    public void InvestmentProfile_Notes_AtMaxLength_ShouldPass()
    {
        var cmd = ValidCommand() with
        {
            InvestmentProfile = new CreateInvestmentProfileDto(null, null, null, null, null, null, new string('x', 2000))
        };
        var result = _validator.TestValidate(cmd);
        result.ShouldNotHaveAnyValidationErrors();
    }
}
