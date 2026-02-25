using Broker.Backoffice.Application.Accounts;
using Broker.Backoffice.Domain.Accounts;
using FluentValidation.TestHelper;

namespace Broker.Backoffice.Tests.Unit.Validators;

public class SetAccountHoldersValidatorTests
{
    private readonly SetAccountHoldersCommandValidator _validator = new();

    private static SetAccountHoldersCommand ValidCommand() => new(
        AccountId: Guid.NewGuid(),
        Holders: [new AccountHolderInput(Guid.NewGuid(), HolderRole.Owner, true)]);

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
    public void Holder_ClientId_Empty_ShouldFail()
    {
        var cmd = ValidCommand() with
        {
            Holders = [new AccountHolderInput(Guid.Empty, HolderRole.Owner, true)]
        };
        var result = _validator.TestValidate(cmd);
        result.ShouldHaveAnyValidationError();
    }

    [Fact]
    public void EmptyHolders_ShouldPass()
    {
        var result = _validator.TestValidate(ValidCommand() with { Holders = [] });
        result.ShouldNotHaveAnyValidationErrors();
    }
}
