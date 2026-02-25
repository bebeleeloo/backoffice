using Broker.Backoffice.Application.Accounts;
using Broker.Backoffice.Domain.Accounts;
using FluentValidation.TestHelper;

namespace Broker.Backoffice.Tests.Unit.Validators;

public class CreateAccountValidatorTests
{
    private readonly CreateAccountCommandValidator _validator = new();

    private static CreateAccountCommand ValidCommand() => new(
        Number: "ACC-001",
        ClearerId: null,
        TradePlatformId: null,
        Status: AccountStatus.Active,
        AccountType: AccountType.Individual,
        MarginType: MarginType.Cash,
        OptionLevel: OptionLevel.Level0,
        Tariff: Tariff.Basic,
        DeliveryType: null,
        OpenedAt: null,
        ClosedAt: null,
        Comment: null,
        ExternalId: null);

    [Fact]
    public void ValidCommand_ShouldPass()
    {
        var result = _validator.TestValidate(ValidCommand());
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Number_Empty_ShouldFail(string? number)
    {
        var result = _validator.TestValidate(ValidCommand() with { Number = number! });
        result.ShouldHaveValidationErrorFor(x => x.Number);
    }

    [Fact]
    public void Number_TooLong_ShouldFail()
    {
        var result = _validator.TestValidate(ValidCommand() with { Number = new string('A', 51) });
        result.ShouldHaveValidationErrorFor(x => x.Number);
    }

    [Fact]
    public void Comment_TooLong_ShouldFail()
    {
        var result = _validator.TestValidate(ValidCommand() with { Comment = new string('x', 501) });
        result.ShouldHaveValidationErrorFor(x => x.Comment);
    }

    [Fact]
    public void ExternalId_TooLong_ShouldFail()
    {
        var result = _validator.TestValidate(ValidCommand() with { ExternalId = new string('x', 65) });
        result.ShouldHaveValidationErrorFor(x => x.ExternalId);
    }

    [Fact]
    public void Comment_AtMaxLength_ShouldPass()
    {
        var result = _validator.TestValidate(ValidCommand() with { Comment = new string('x', 500) });
        result.ShouldNotHaveValidationErrorFor(x => x.Comment);
    }
}
