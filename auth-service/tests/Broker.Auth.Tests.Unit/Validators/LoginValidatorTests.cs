using Broker.Auth.Application.Auth;
using FluentValidation.TestHelper;

namespace Broker.Auth.Tests.Unit.Validators;

public class LoginValidatorTests
{
    private readonly LoginCommandValidator _validator = new();

    [Fact]
    public void ValidCommand_ShouldPass()
    {
        var result = _validator.TestValidate(new LoginCommand("admin", "password"));
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Username_Empty_ShouldFail(string? username)
    {
        var result = _validator.TestValidate(new LoginCommand(username!, "password"));
        result.ShouldHaveValidationErrorFor(x => x.Username);
    }

    [Fact]
    public void Username_TooLong_ShouldFail()
    {
        var result = _validator.TestValidate(new LoginCommand(new string('a', 101), "password"));
        result.ShouldHaveValidationErrorFor(x => x.Username);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Password_Empty_ShouldFail(string? password)
    {
        var result = _validator.TestValidate(new LoginCommand("admin", password!));
        result.ShouldHaveValidationErrorFor(x => x.Password);
    }
}
