using Broker.Backoffice.Application.Roles;
using FluentValidation.TestHelper;

namespace Broker.Backoffice.Tests.Unit.Validators;

public class CreateRoleValidatorTests
{
    private readonly CreateRoleCommandValidator _validator = new();

    private static CreateRoleCommand ValidCommand() => new(
        Name: "Trader",
        Description: "Trader role");

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
        var result = _validator.TestValidate(ValidCommand() with { Name = new string('a', 101) });
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Name_AtMaxLength_ShouldPass()
    {
        var result = _validator.TestValidate(ValidCommand() with { Name = new string('a', 100) });
        result.ShouldNotHaveValidationErrorFor(x => x.Name);
    }
}
