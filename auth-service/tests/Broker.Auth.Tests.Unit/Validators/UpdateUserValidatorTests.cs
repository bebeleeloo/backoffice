using Broker.Auth.Application.Users;
using FluentValidation.TestHelper;

namespace Broker.Auth.Tests.Unit.Validators;

public class UpdateUserValidatorTests
{
    private readonly UpdateUserCommandValidator _validator = new();

    [Fact]
    public void ValidCommand_ShouldPass()
    {
        var result = _validator.TestValidate(new UpdateUserCommand(
            Guid.NewGuid(), "user@test.com", "Full Name", true, [], 1u));
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Email_Empty_ShouldFail(string? email)
    {
        var result = _validator.TestValidate(new UpdateUserCommand(
            Guid.NewGuid(), email!, "Name", true, [], 1u));
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void Email_InvalidFormat_ShouldFail()
    {
        var result = _validator.TestValidate(new UpdateUserCommand(
            Guid.NewGuid(), "not-an-email", "Name", true, [], 1u));
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void Email_TooLong_ShouldFail()
    {
        var result = _validator.TestValidate(new UpdateUserCommand(
            Guid.NewGuid(), new string('a', 248) + "@test.com", "Name", true, [], 1u));
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void RowVersion_Empty_ShouldFail()
    {
        var result = _validator.TestValidate(new UpdateUserCommand(
            Guid.NewGuid(), "user@test.com", "Name", true, [], 0u));
        result.ShouldHaveValidationErrorFor(x => x.RowVersion);
    }
}
