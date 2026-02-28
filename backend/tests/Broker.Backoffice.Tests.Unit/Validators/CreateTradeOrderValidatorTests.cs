using Broker.Backoffice.Application.Orders.TradeOrders;
using Broker.Backoffice.Domain.Orders;
using FluentValidation.TestHelper;

namespace Broker.Backoffice.Tests.Unit.Validators;

public class CreateTradeOrderValidatorTests
{
    private readonly CreateTradeOrderCommandValidator _validator = new();

    private static CreateTradeOrderCommand ValidCommand() => new(
        AccountId: Guid.NewGuid(),
        InstrumentId: Guid.NewGuid(),
        OrderDate: DateTime.UtcNow,
        Side: TradeSide.Buy,
        OrderType: TradeOrderType.Market,
        TimeInForce: TimeInForce.Day,
        Quantity: 100,
        Price: 50.25m,
        StopPrice: null,
        Commission: 9.99m,
        ExpirationDate: null,
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
    public void Price_Null_ShouldPass()
    {
        var result = _validator.TestValidate(ValidCommand() with { Price = null });
        result.ShouldNotHaveValidationErrorFor(x => x.Price);
    }

    [Fact]
    public void Price_RequiredForLimitOrder_ShouldFail()
    {
        var result = _validator.TestValidate(ValidCommand() with { OrderType = TradeOrderType.Limit, Price = null });
        result.ShouldHaveValidationErrorFor(x => x.Price);
    }

    [Fact]
    public void Price_RequiredForStopLimitOrder_ShouldFail()
    {
        var result = _validator.TestValidate(ValidCommand() with { OrderType = TradeOrderType.StopLimit, Price = null });
        result.ShouldHaveValidationErrorFor(x => x.Price);
    }

    [Fact]
    public void StopPrice_RequiredForStopOrder_ShouldFail()
    {
        var result = _validator.TestValidate(ValidCommand() with { OrderType = TradeOrderType.Stop, StopPrice = null });
        result.ShouldHaveValidationErrorFor(x => x.StopPrice);
    }

    [Fact]
    public void StopPrice_RequiredForStopLimitOrder_ShouldFail()
    {
        var result = _validator.TestValidate(ValidCommand() with { OrderType = TradeOrderType.StopLimit, StopPrice = null, Price = 50m });
        result.ShouldHaveValidationErrorFor(x => x.StopPrice);
    }

    [Fact]
    public void StopPrice_ZeroOrNegative_ShouldFail()
    {
        var result = _validator.TestValidate(ValidCommand() with { StopPrice = -1 });
        result.ShouldHaveValidationErrorFor(x => x.StopPrice);
    }

    [Fact]
    public void ExpirationDate_RequiredForGTD_ShouldFail()
    {
        var result = _validator.TestValidate(ValidCommand() with { TimeInForce = TimeInForce.GTD, ExpirationDate = null });
        result.ShouldHaveValidationErrorFor(x => x.ExpirationDate);
    }

    [Fact]
    public void ExpirationDate_NotRequiredForDay_ShouldPass()
    {
        var result = _validator.TestValidate(ValidCommand() with { TimeInForce = TimeInForce.Day, ExpirationDate = null });
        result.ShouldNotHaveValidationErrorFor(x => x.ExpirationDate);
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
