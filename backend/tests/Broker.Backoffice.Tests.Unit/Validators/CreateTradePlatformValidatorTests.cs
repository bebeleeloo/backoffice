using Broker.Backoffice.Application.TradePlatforms;
using FluentValidation.TestHelper;

namespace Broker.Backoffice.Tests.Unit.Validators;

public class CreateTradePlatformValidatorTests
{
    private readonly CreateTradePlatformCommandValidator _validator = new();

    private static CreateTradePlatformCommand ValidCommand() => new(
        Name: "MetaTrader 5",
        Description: "Trading platform");

    [Fact]
    public void ValidCommand_ShouldPass()
    {
        var result = _validator.TestValidate(ValidCommand());
        result.ShouldNotHaveAnyValidationErrors();
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

    [Fact]
    public void Description_TooLong_ShouldFail()
    {
        var result = _validator.TestValidate(ValidCommand() with { Description = new string('a', 501) });
        result.ShouldHaveValidationErrorFor(x => x.Description);
    }

    [Fact]
    public void Description_AtMaxLength_ShouldPass()
    {
        var result = _validator.TestValidate(ValidCommand() with { Description = new string('a', 500) });
        result.ShouldNotHaveValidationErrorFor(x => x.Description);
    }
}
