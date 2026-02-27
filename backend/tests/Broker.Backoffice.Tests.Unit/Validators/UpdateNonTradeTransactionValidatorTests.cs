using Broker.Backoffice.Application.Transactions.NonTradeTransactions;
using Broker.Backoffice.Domain.Transactions;
using FluentValidation.TestHelper;

namespace Broker.Backoffice.Tests.Unit.Validators;

public class UpdateNonTradeTransactionValidatorTests
{
    private readonly UpdateNonTradeTransactionCommandValidator _validator = new();

    private static UpdateNonTradeTransactionCommand ValidCommand() => new(
        Id: Guid.NewGuid(),
        OrderId: Guid.NewGuid(),
        TransactionDate: DateTime.UtcNow,
        Status: TransactionStatus.Pending,
        Amount: 1000.50m,
        CurrencyId: Guid.NewGuid(),
        InstrumentId: null,
        ReferenceNumber: "REF-001",
        Description: "Test deposit",
        ProcessedAt: null,
        Comment: "Test comment",
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
        var result = _validator.TestValidate(ValidCommand() with { Amount = 500 });
        result.ShouldNotHaveValidationErrorFor(x => x.Amount);
    }

    [Fact]
    public void Amount_Negative_ShouldPass()
    {
        var result = _validator.TestValidate(ValidCommand() with { Amount = -500 });
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

    [Fact]
    public void RowVersion_Empty_ShouldFail()
    {
        var result = _validator.TestValidate(ValidCommand() with { RowVersion = [] });
        result.ShouldHaveValidationErrorFor(x => x.RowVersion);
    }
}
