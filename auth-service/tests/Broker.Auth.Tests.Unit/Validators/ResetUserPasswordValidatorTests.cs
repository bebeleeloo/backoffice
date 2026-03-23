using Broker.Auth.Application.Users;
using FluentValidation.TestHelper;

namespace Broker.Auth.Tests.Unit.Validators;

public class ResetUserPasswordValidatorTests
{
    private readonly ResetUserPasswordCommandValidator _validator = new();

    [Fact]
    public void ValidCommand_ShouldPass()
    {
        var result = _validator.TestValidate(new ResetUserPasswordCommand(Guid.NewGuid(), "NewPass123!"));
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void UserId_Empty_ShouldFail()
    {
        var result = _validator.TestValidate(new ResetUserPasswordCommand(Guid.Empty, "NewPass123!"));
        result.ShouldHaveValidationErrorFor(x => x.UserId);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void NewPassword_Empty_ShouldFail(string? newPass)
    {
        var result = _validator.TestValidate(new ResetUserPasswordCommand(Guid.NewGuid(), newPass!));
        result.ShouldHaveValidationErrorFor(x => x.NewPassword);
    }

    [Fact]
    public void NewPassword_TooShort_ShouldFail()
    {
        var result = _validator.TestValidate(new ResetUserPasswordCommand(Guid.NewGuid(), "12345"));
        result.ShouldHaveValidationErrorFor(x => x.NewPassword);
    }
}
