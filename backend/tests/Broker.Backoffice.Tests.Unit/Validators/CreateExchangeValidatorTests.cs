using Broker.Backoffice.Application.Exchanges;
using FluentValidation.TestHelper;

namespace Broker.Backoffice.Tests.Unit.Validators;

public class CreateExchangeValidatorTests
{
    private readonly CreateExchangeCommandValidator _validator = new();

    private static CreateExchangeCommand ValidCommand() => new(
        Code: "NYSE",
        Name: "New York Stock Exchange",
        CountryId: null);

    [Fact]
    public void ValidCommand_ShouldPass()
    {
        var result = _validator.TestValidate(ValidCommand());
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Code_Empty_ShouldFail(string? code)
    {
        var result = _validator.TestValidate(ValidCommand() with { Code = code! });
        result.ShouldHaveValidationErrorFor(x => x.Code);
    }

    [Fact]
    public void Code_TooLong_ShouldFail()
    {
        var result = _validator.TestValidate(ValidCommand() with { Code = new string('A', 21) });
        result.ShouldHaveValidationErrorFor(x => x.Code);
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
        var result = _validator.TestValidate(ValidCommand() with { Name = new string('a', 201) });
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }
}
