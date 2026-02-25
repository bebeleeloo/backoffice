using Broker.Backoffice.Application.Roles;
using FluentValidation.TestHelper;

namespace Broker.Backoffice.Tests.Unit.Validators;

public class UpdateRoleValidatorTests
{
    private readonly UpdateRoleCommandValidator _validator = new();

    private static UpdateRoleCommand ValidCommand() => new(
        Id: Guid.NewGuid(),
        Name: "Trader",
        Description: "Trader role",
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
    public void RowVersion_Empty_ShouldFail()
    {
        var result = _validator.TestValidate(ValidCommand() with { RowVersion = [] });
        result.ShouldHaveValidationErrorFor(x => x.RowVersion);
    }
}
