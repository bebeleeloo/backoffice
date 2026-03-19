using Broker.Auth.Application.Roles;
using FluentValidation.TestHelper;

namespace Broker.Auth.Tests.Unit.Validators;

public class UpdateRoleValidatorTests
{
    private readonly UpdateRoleCommandValidator _validator = new();

    [Fact]
    public void ValidCommand_ShouldPass()
    {
        var result = _validator.TestValidate(new UpdateRoleCommand(
            Guid.NewGuid(), "Editor", "Can edit content", 1u));
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void ValidCommand_NullDescription_ShouldPass()
    {
        var result = _validator.TestValidate(new UpdateRoleCommand(
            Guid.NewGuid(), "Editor", null, 1u));
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Name_Empty_ShouldFail(string? name)
    {
        var result = _validator.TestValidate(new UpdateRoleCommand(
            Guid.NewGuid(), name!, null, 1u));
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Name_TooLong_ShouldFail()
    {
        var result = _validator.TestValidate(new UpdateRoleCommand(
            Guid.NewGuid(), new string('a', 101), null, 1u));
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void RowVersion_Empty_ShouldFail()
    {
        var result = _validator.TestValidate(new UpdateRoleCommand(
            Guid.NewGuid(), "Editor", null, 0u));
        result.ShouldHaveValidationErrorFor(x => x.RowVersion);
    }
}
