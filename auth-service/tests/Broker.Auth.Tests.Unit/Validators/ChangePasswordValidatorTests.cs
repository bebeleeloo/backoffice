using Broker.Auth.Application.Auth;
using FluentValidation.TestHelper;

namespace Broker.Auth.Tests.Unit.Validators;

public class ChangePasswordValidatorTests
{
    private readonly ChangePasswordCommandValidator _validator = new();

    [Fact]
    public void ValidCommand_ShouldPass()
    {
        var result = _validator.TestValidate(new ChangePasswordCommand(Guid.NewGuid(), "current", "NewPass123!"));
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void CurrentPassword_Empty_ShouldFail(string? current)
    {
        var result = _validator.TestValidate(new ChangePasswordCommand(Guid.NewGuid(), current!, "newPass"));
        result.ShouldHaveValidationErrorFor(x => x.CurrentPassword);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void NewPassword_Empty_ShouldFail(string? newPass)
    {
        var result = _validator.TestValidate(new ChangePasswordCommand(Guid.NewGuid(), "current", newPass!));
        result.ShouldHaveValidationErrorFor(x => x.NewPassword);
    }

    [Fact]
    public void NewPassword_TooShort_ShouldFail()
    {
        var result = _validator.TestValidate(new ChangePasswordCommand(Guid.NewGuid(), "current", "12345"));
        result.ShouldHaveValidationErrorFor(x => x.NewPassword);
    }
}
