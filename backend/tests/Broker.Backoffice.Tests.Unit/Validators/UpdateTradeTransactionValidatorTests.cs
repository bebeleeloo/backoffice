using Broker.Backoffice.Application.Transactions.TradeTransactions;
using Broker.Backoffice.Domain.Orders;
using Broker.Backoffice.Domain.Transactions;
using FluentValidation.TestHelper;

namespace Broker.Backoffice.Tests.Unit.Validators;

public class UpdateTradeTransactionValidatorTests
{
    private readonly UpdateTradeTransactionCommandValidator _validator = new();

    private static UpdateTradeTransactionCommand ValidCommand() => new(
        Id: Guid.NewGuid(),
        OrderId: Guid.NewGuid(),
        InstrumentId: Guid.NewGuid(),
        TransactionDate: DateTime.UtcNow,
        Status: TransactionStatus.Pending,
        Side: TradeSide.Buy,
        Quantity: 100,
        Price: 50.25m,
        Commission: 9.99m,
        SettlementDate: DateTime.UtcNow.AddDays(2),
        Venue: "NYSE",
        Comment: "Test trade",
        ExternalId: "EXT-001",
        RowVersion: [1, 2, 3]);

    [Fact]
    public void ValidCommand_ShouldPass()
    {
        var result = _validator.TestValidate(ValidCommand());
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void OrderId_Null_ShouldPass()
    {
        var result = _validator.TestValidate(ValidCommand() with { OrderId = null });
        result.ShouldNotHaveValidationErrorFor(x => x.OrderId);
    }

    [Fact]
    public void InstrumentId_Empty_ShouldFail()
    {
        var result = _validator.TestValidate(ValidCommand() with { InstrumentId = Guid.Empty });
        result.ShouldHaveValidationErrorFor(x => x.InstrumentId);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100.5)]
    public void Quantity_ZeroOrNegative_ShouldFail(decimal quantity)
    {
        var result = _validator.TestValidate(ValidCommand() with { Quantity = quantity });
        result.ShouldHaveValidationErrorFor(x => x.Quantity);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-50.25)]
    public void Price_ZeroOrNegative_ShouldFail(decimal price)
    {
        var result = _validator.TestValidate(ValidCommand() with { Price = price });
        result.ShouldHaveValidationErrorFor(x => x.Price);
    }

    [Fact]
    public void Commission_Negative_ShouldFail()
    {
        var result = _validator.TestValidate(ValidCommand() with { Commission = -1 });
        result.ShouldHaveValidationErrorFor(x => x.Commission);
    }

    [Fact]
    public void Commission_Zero_ShouldPass()
    {
        var result = _validator.TestValidate(ValidCommand() with { Commission = 0 });
        result.ShouldNotHaveValidationErrorFor(x => x.Commission);
    }

    [Fact]
    public void Commission_Null_ShouldPass()
    {
        var result = _validator.TestValidate(ValidCommand() with { Commission = null });
        result.ShouldNotHaveValidationErrorFor(x => x.Commission);
    }

    [Fact]
    public void Venue_TooLong_ShouldFail()
    {
        var result = _validator.TestValidate(ValidCommand() with { Venue = new string('x', 101) });
        result.ShouldHaveValidationErrorFor(x => x.Venue);
    }

    [Fact]
    public void Venue_AtMaxLength_ShouldPass()
    {
        var result = _validator.TestValidate(ValidCommand() with { Venue = new string('x', 100) });
        result.ShouldNotHaveValidationErrorFor(x => x.Venue);
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

    [Fact]
    public void RowVersion_Empty_ShouldFail()
    {
        var result = _validator.TestValidate(ValidCommand() with { RowVersion = [] });
        result.ShouldHaveValidationErrorFor(x => x.RowVersion);
    }
}
