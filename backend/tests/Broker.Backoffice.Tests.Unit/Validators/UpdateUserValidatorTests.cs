using Broker.Backoffice.Application.Users;
using FluentValidation.TestHelper;

namespace Broker.Backoffice.Tests.Unit.Validators;

public class UpdateUserValidatorTests
{
    private readonly UpdateUserCommandValidator _validator = new();

    private static UpdateUserCommand ValidCommand() => new(
        Id: Guid.NewGuid(),
        Email: "john@example.com",
        FullName: "John Doe",
        IsActive: true,
        RoleIds: [],
        RowVersion: [1, 2, 3]);

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
    public void RowVersion_Empty_ShouldFail()
    {
        var result = _validator.TestValidate(ValidCommand() with { RowVersion = [] });
        result.ShouldHaveValidationErrorFor(x => x.RowVersion);
    }
}
