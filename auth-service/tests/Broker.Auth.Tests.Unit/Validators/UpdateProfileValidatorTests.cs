using Broker.Auth.Application.Auth;
using FluentValidation.TestHelper;

namespace Broker.Auth.Tests.Unit.Validators;

public class UpdateProfileValidatorTests
{
    private readonly UpdateProfileCommandValidator _validator = new();

    [Fact]
    public void ValidCommand_ShouldPass()
    {
        var result = _validator.TestValidate(new UpdateProfileCommand(Guid.NewGuid(), "Full Name", "user@test.com"));
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void ValidCommand_NullFullName_ShouldPass()
    {
        var result = _validator.TestValidate(new UpdateProfileCommand(Guid.NewGuid(), null, "user@test.com"));
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Email_Empty_ShouldFail(string? email)
    {
        var result = _validator.TestValidate(new UpdateProfileCommand(Guid.NewGuid(), "Name", email!));
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void Email_InvalidFormat_ShouldFail()
    {
        var result = _validator.TestValidate(new UpdateProfileCommand(Guid.NewGuid(), "Name", "not-an-email"));
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void Email_TooLong_ShouldFail()
    {
        var result = _validator.TestValidate(new UpdateProfileCommand(Guid.NewGuid(), "Name", new string('a', 248) + "@test.com"));
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void FullName_TooLong_ShouldFail()
    {
        var result = _validator.TestValidate(new UpdateProfileCommand(Guid.NewGuid(), new string('a', 201), "user@test.com"));
        result.ShouldHaveValidationErrorFor(x => x.FullName);
    }
}
