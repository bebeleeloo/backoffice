using Broker.Backoffice.Application.Auth;
using FluentValidation.TestHelper;

namespace Broker.Backoffice.Tests.Unit.Validators;

public class ChangePasswordValidatorTests
{
    private readonly ChangePasswordCommandValidator _validator = new();

    private static ChangePasswordCommand ValidCommand() => new(
        UserId: Guid.NewGuid(),
        CurrentPassword: "OldPass123!",
        NewPassword: "NewPass123!");

    [Fact]
    public void ValidCommand_ShouldPass()
    {
        var result = _validator.TestValidate(ValidCommand());
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void CurrentPassword_Empty_ShouldFail(string? currentPassword)
    {
        var result = _validator.TestValidate(ValidCommand() with { CurrentPassword = currentPassword! });
        result.ShouldHaveValidationErrorFor(x => x.CurrentPassword);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void NewPassword_Empty_ShouldFail(string? newPassword)
    {
        var result = _validator.TestValidate(ValidCommand() with { NewPassword = newPassword! });
        result.ShouldHaveValidationErrorFor(x => x.NewPassword);
    }

    [Fact]
    public void NewPassword_TooShort_ShouldFail()
    {
        var result = _validator.TestValidate(ValidCommand() with { NewPassword = "12345" });
        result.ShouldHaveValidationErrorFor(x => x.NewPassword);
    }

    [Fact]
    public void NewPassword_AtMinLength_ShouldPass()
    {
        var result = _validator.TestValidate(ValidCommand() with { NewPassword = "123456" });
        result.ShouldNotHaveValidationErrorFor(x => x.NewPassword);
    }
}
