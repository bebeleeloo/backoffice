using Broker.Backoffice.Application.Auth;
using FluentValidation.TestHelper;

namespace Broker.Backoffice.Tests.Unit.Validators;

public class UpdateProfileValidatorTests
{
    private readonly UpdateProfileCommandValidator _validator = new();

    private static UpdateProfileCommand ValidCommand() => new(
        UserId: Guid.NewGuid(),
        FullName: "John Doe",
        Email: "john@example.com");

    [Fact]
    public void ValidCommand_ShouldPass()
    {
        var result = _validator.TestValidate(ValidCommand());
        result.ShouldNotHaveAnyValidationErrors();
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

    [Fact]
    public void FullName_TooLong_ShouldFail()
    {
        var result = _validator.TestValidate(ValidCommand() with { FullName = new string('a', 201) });
        result.ShouldHaveValidationErrorFor(x => x.FullName);
    }

    [Fact]
    public void FullName_AtMaxLength_ShouldPass()
    {
        var result = _validator.TestValidate(ValidCommand() with { FullName = new string('a', 200) });
        result.ShouldNotHaveValidationErrorFor(x => x.FullName);
    }

    [Fact]
    public void FullName_Null_ShouldPass()
    {
        var result = _validator.TestValidate(ValidCommand() with { FullName = null });
        result.ShouldNotHaveValidationErrorFor(x => x.FullName);
    }
}
