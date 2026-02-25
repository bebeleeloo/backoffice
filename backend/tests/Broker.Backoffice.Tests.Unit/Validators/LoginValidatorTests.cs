using Broker.Backoffice.Application.Auth;
using FluentValidation.TestHelper;

namespace Broker.Backoffice.Tests.Unit.Validators;

public class LoginValidatorTests
{
    private readonly LoginCommandValidator _validator = new();

    private static LoginCommand ValidCommand() => new(
        Username: "admin",
        Password: "Admin123!");

    [Fact]
    public void ValidCommand_ShouldPass()
    {
        var result = _validator.TestValidate(ValidCommand());
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Username_Empty_ShouldFail(string? username)
    {
        var result = _validator.TestValidate(ValidCommand() with { Username = username! });
        result.ShouldHaveValidationErrorFor(x => x.Username);
    }

    [Fact]
    public void Username_TooLong_ShouldFail()
    {
        var result = _validator.TestValidate(ValidCommand() with { Username = new string('a', 101) });
        result.ShouldHaveValidationErrorFor(x => x.Username);
    }

    [Fact]
    public void Username_AtMaxLength_ShouldPass()
    {
        var result = _validator.TestValidate(ValidCommand() with { Username = new string('a', 100) });
        result.ShouldNotHaveValidationErrorFor(x => x.Username);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Password_Empty_ShouldFail(string? password)
    {
        var result = _validator.TestValidate(ValidCommand() with { Password = password! });
        result.ShouldHaveValidationErrorFor(x => x.Password);
    }
}
