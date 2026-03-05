using Broker.Auth.Application.Roles;
using FluentValidation.TestHelper;

namespace Broker.Auth.Tests.Unit.Validators;

public class CreateRoleValidatorTests
{
    private readonly CreateRoleCommandValidator _validator = new();

    [Fact]
    public void ValidCommand_ShouldPass()
    {
        var result = _validator.TestValidate(new CreateRoleCommand("Admin", "Full access"));
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Name_Empty_ShouldFail(string? name)
    {
        var result = _validator.TestValidate(new CreateRoleCommand(name!, null));
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Name_TooLong_ShouldFail()
    {
        var result = _validator.TestValidate(new CreateRoleCommand(new string('a', 101), null));
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }
}
