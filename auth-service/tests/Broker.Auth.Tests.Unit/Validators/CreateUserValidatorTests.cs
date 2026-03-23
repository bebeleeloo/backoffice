using Broker.Auth.Application.Users;
using FluentValidation.TestHelper;

namespace Broker.Auth.Tests.Unit.Validators;

public class CreateUserValidatorTests
{
    private readonly CreateUserCommandValidator _validator = new();

    [Fact]
    public void ValidCommand_ShouldPass()
    {
        var result = _validator.TestValidate(new CreateUserCommand("user1", "user@test.com", "Pass123!", "Full Name", true, []));
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Username_Empty_ShouldFail(string? username)
    {
        var result = _validator.TestValidate(new CreateUserCommand(username!, "user@test.com", "Pass123!", null, true, []));
        result.ShouldHaveValidationErrorFor(x => x.Username);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not-an-email")]
    public void Email_Invalid_ShouldFail(string? email)
    {
        var result = _validator.TestValidate(new CreateUserCommand("user1", email!, "Pass123!", null, true, []));
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Password_Empty_ShouldFail(string? password)
    {
        var result = _validator.TestValidate(new CreateUserCommand("user1", "user@test.com", password!, null, true, []));
        result.ShouldHaveValidationErrorFor(x => x.Password);
    }

    [Fact]
    public void Password_TooShort_ShouldFail()
    {
        var result = _validator.TestValidate(new CreateUserCommand("user1", "user@test.com", "12345", null, true, []));
        result.ShouldHaveValidationErrorFor(x => x.Password);
    }
}
