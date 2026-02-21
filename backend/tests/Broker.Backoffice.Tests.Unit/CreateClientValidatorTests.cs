using Broker.Backoffice.Application.Clients;
using Broker.Backoffice.Domain.Clients;
using FluentAssertions;
using FluentValidation.TestHelper;

namespace Broker.Backoffice.Tests.Unit;

public class CreateClientValidatorTests
{
    private readonly CreateClientCommandValidator _validator = new();

    private static CreateClientCommand ValidCommand() => new(
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
        InvestmentProfile: null);

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
    public void Phone_TooLong_ShouldFail()
    {
        var result = _validator.TestValidate(ValidCommand() with { Phone = new string('1', 33) });
        result.ShouldHaveValidationErrorFor(x => x.Phone);
    }

    [Fact]
    public void ExternalId_TooLong_ShouldFail()
    {
        var result = _validator.TestValidate(ValidCommand() with { ExternalId = new string('x', 65) });
        result.ShouldHaveValidationErrorFor(x => x.ExternalId);
    }

    [Fact]
    public void Ssn_TooLong_ShouldFail()
    {
        var result = _validator.TestValidate(ValidCommand() with { Ssn = new string('1', 21) });
        result.ShouldHaveValidationErrorFor(x => x.Ssn);
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
}
