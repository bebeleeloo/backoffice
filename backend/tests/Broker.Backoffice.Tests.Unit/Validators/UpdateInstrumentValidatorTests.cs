using Broker.Backoffice.Application.Instruments;
using Broker.Backoffice.Domain.Instruments;
using FluentValidation.TestHelper;

namespace Broker.Backoffice.Tests.Unit.Validators;

public class UpdateInstrumentValidatorTests
{
    private readonly UpdateInstrumentCommandValidator _validator = new();

    private static UpdateInstrumentCommand ValidCommand() => new(
        Id: Guid.NewGuid(),
        Symbol: "AAPL",
        Name: "Apple Inc.",
        ISIN: null,
        CUSIP: null,
        Type: InstrumentType.Stock,
        AssetClass: AssetClass.Equities,
        Status: InstrumentStatus.Active,
        ExchangeId: null,
        CurrencyId: null,
        CountryId: null,
        Sector: null,
        LotSize: 1,
        TickSize: null,
        MarginRequirement: null,
        IsMarginEligible: false,
        ListingDate: null,
        DelistingDate: null,
        ExpirationDate: null,
        IssuerName: null,
        Description: null,
        ExternalId: null,
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
    public void Symbol_Empty_ShouldFail(string? symbol)
    {
        var result = _validator.TestValidate(ValidCommand() with { Symbol = symbol! });
        result.ShouldHaveValidationErrorFor(x => x.Symbol);
    }

    [Fact]
    public void Symbol_TooLong_ShouldFail()
    {
        var result = _validator.TestValidate(ValidCommand() with { Symbol = new string('A', 21) });
        result.ShouldHaveValidationErrorFor(x => x.Symbol);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Name_Empty_ShouldFail(string? name)
    {
        var result = _validator.TestValidate(ValidCommand() with { Name = name! });
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Name_TooLong_ShouldFail()
    {
        var result = _validator.TestValidate(ValidCommand() with { Name = new string('a', 256) });
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void ISIN_TooLong_ShouldFail()
    {
        var result = _validator.TestValidate(ValidCommand() with { ISIN = new string('A', 13) });
        result.ShouldHaveValidationErrorFor(x => x.ISIN);
    }

    [Fact]
    public void CUSIP_TooLong_ShouldFail()
    {
        var result = _validator.TestValidate(ValidCommand() with { CUSIP = new string('A', 10) });
        result.ShouldHaveValidationErrorFor(x => x.CUSIP);
    }

    [Fact]
    public void IssuerName_TooLong_ShouldFail()
    {
        var result = _validator.TestValidate(ValidCommand() with { IssuerName = new string('a', 256) });
        result.ShouldHaveValidationErrorFor(x => x.IssuerName);
    }

    [Fact]
    public void Description_TooLong_ShouldFail()
    {
        var result = _validator.TestValidate(ValidCommand() with { Description = new string('a', 1001) });
        result.ShouldHaveValidationErrorFor(x => x.Description);
    }

    [Fact]
    public void ExternalId_TooLong_ShouldFail()
    {
        var result = _validator.TestValidate(ValidCommand() with { ExternalId = new string('x', 65) });
        result.ShouldHaveValidationErrorFor(x => x.ExternalId);
    }

    [Fact]
    public void RowVersion_Empty_ShouldFail()
    {
        var result = _validator.TestValidate(ValidCommand() with { RowVersion = [] });
        result.ShouldHaveValidationErrorFor(x => x.RowVersion);
    }
}
