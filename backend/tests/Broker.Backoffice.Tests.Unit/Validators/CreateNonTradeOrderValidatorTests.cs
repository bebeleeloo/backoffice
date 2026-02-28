using Broker.Backoffice.Application.Orders.NonTradeOrders;
using Broker.Backoffice.Domain.Orders;
using FluentValidation.TestHelper;

namespace Broker.Backoffice.Tests.Unit.Validators;

public class CreateNonTradeOrderValidatorTests
{
    private readonly CreateNonTradeOrderCommandValidator _validator = new();

    private static CreateNonTradeOrderCommand ValidCommand() => new(
        AccountId: Guid.NewGuid(),
        OrderDate: DateTime.UtcNow,
        NonTradeType: NonTradeOrderType.Deposit,
        Amount: 1000.00m,
        CurrencyId: Guid.NewGuid(),
        InstrumentId: null,
        ReferenceNumber: "REF-001",
        Description: "Test deposit",
        Comment: "Test order",
        ExternalId: "EXT-001");

    [Fact]
    public void ValidCommand_ShouldPass()
    {
        var result = _validator.TestValidate(ValidCommand());
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void AccountId_Empty_ShouldFail()
    {
        var result = _validator.TestValidate(ValidCommand() with { AccountId = Guid.Empty });
        result.ShouldHaveValidationErrorFor(x => x.AccountId);
    }

    [Fact]
    public void CurrencyId_Empty_ShouldFail()
    {
        var result = _validator.TestValidate(ValidCommand() with { CurrencyId = Guid.Empty });
        result.ShouldHaveValidationErrorFor(x => x.CurrencyId);
    }

    [Fact]
    public void Amount_Zero_ShouldFail()
    {
        var result = _validator.TestValidate(ValidCommand() with { Amount = 0 });
        result.ShouldHaveValidationErrorFor(x => x.Amount);
    }

    [Fact]
    public void Amount_Positive_ShouldPass()
    {
        var result = _validator.TestValidate(ValidCommand() with { Amount = 500.00m });
        result.ShouldNotHaveValidationErrorFor(x => x.Amount);
    }

    [Fact]
    public void Amount_Negative_ShouldPass()
    {
        var result = _validator.TestValidate(ValidCommand() with { Amount = -500.00m });
        result.ShouldNotHaveValidationErrorFor(x => x.Amount);
    }

    [Fact]
    public void ReferenceNumber_TooLong_ShouldFail()
    {
        var result = _validator.TestValidate(ValidCommand() with { ReferenceNumber = new string('x', 101) });
        result.ShouldHaveValidationErrorFor(x => x.ReferenceNumber);
    }

    [Fact]
    public void ReferenceNumber_AtMaxLength_ShouldPass()
    {
        var result = _validator.TestValidate(ValidCommand() with { ReferenceNumber = new string('x', 100) });
        result.ShouldNotHaveValidationErrorFor(x => x.ReferenceNumber);
    }

    [Fact]
    public void Description_TooLong_ShouldFail()
    {
        var result = _validator.TestValidate(ValidCommand() with { Description = new string('x', 501) });
        result.ShouldHaveValidationErrorFor(x => x.Description);
    }

    [Fact]
    public void Description_AtMaxLength_ShouldPass()
    {
        var result = _validator.TestValidate(ValidCommand() with { Description = new string('x', 500) });
        result.ShouldNotHaveValidationErrorFor(x => x.Description);
    }

    [Fact]
    public void Comment_TooLong_ShouldFail()
    {
        var result = _validator.TestValidate(ValidCommand() with { Comment = new string('x', 501) });
        result.ShouldHaveValidationErrorFor(x => x.Comment);
    }

    [Fact]
    public void Comment_AtMaxLength_ShouldPass()
    {
        var result = _validator.TestValidate(ValidCommand() with { Comment = new string('x', 500) });
        result.ShouldNotHaveValidationErrorFor(x => x.Comment);
    }

    [Fact]
    public void ExternalId_TooLong_ShouldFail()
    {
        var result = _validator.TestValidate(ValidCommand() with { ExternalId = new string('x', 65) });
        result.ShouldHaveValidationErrorFor(x => x.ExternalId);
    }

    [Fact]
    public void ExternalId_AtMaxLength_ShouldPass()
    {
        var result = _validator.TestValidate(ValidCommand() with { ExternalId = new string('x', 64) });
        result.ShouldNotHaveValidationErrorFor(x => x.ExternalId);
    }
}
