using Broker.Backoffice.Application.Users;
using FluentValidation.TestHelper;

namespace Broker.Backoffice.Tests.Unit.Validators;

public class CreateUserValidatorTests
{
    private readonly CreateUserCommandValidator _validator = new();

    private static CreateUserCommand ValidCommand() => new(
        Username: "johndoe",
        Email: "john@example.com",
        Password: "Pass123!",
        FullName: "John Doe",
        IsActive: true,
        RoleIds: []);

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

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Email_Empty_ShouldFail(string? email)
    {
        var result = _validator.TestValidate(ValidCommand() with { Email = email! });
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void Email_Invalid_ShouldFail()
    {
        var result = _validator.TestValidate(ValidCommand() with { Email = "not-an-email" });
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void Email_TooLong_ShouldFail()
    {
        var result = _validator.TestValidate(ValidCommand() with { Email = new string('a', 250) + "@test.com" });
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Password_Empty_ShouldFail(string? password)
    {
        var result = _validator.TestValidate(ValidCommand() with { Password = password! });
        result.ShouldHaveValidationErrorFor(x => x.Password);
    }

    [Fact]
    public void Password_TooShort_ShouldFail()
    {
        var result = _validator.TestValidate(ValidCommand() with { Password = "12345" });
        result.ShouldHaveValidationErrorFor(x => x.Password);
    }

    [Fact]
    public void Password_AtMinLength_ShouldPass()
    {
        var result = _validator.TestValidate(ValidCommand() with { Password = "123456" });
        result.ShouldNotHaveValidationErrorFor(x => x.Password);
    }
}
