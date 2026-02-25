using Broker.Backoffice.Application.Clients;
using Broker.Backoffice.Domain.Accounts;
using FluentValidation.TestHelper;

namespace Broker.Backoffice.Tests.Unit.Validators;

public class SetClientAccountsValidatorTests
{
    private readonly SetClientAccountsCommandValidator _validator = new();

    private static SetClientAccountsCommand ValidCommand() => new(
        ClientId: Guid.NewGuid(),
        Accounts: [new ClientAccountInput(Guid.NewGuid(), HolderRole.Owner, true)]);

    [Fact]
    public void ValidCommand_ShouldPass()
    {
        var result = _validator.TestValidate(ValidCommand());
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void ClientId_Empty_ShouldFail()
    {
        var result = _validator.TestValidate(ValidCommand() with { ClientId = Guid.Empty });
        result.ShouldHaveValidationErrorFor(x => x.ClientId);
    }

    [Fact]
    public void Account_AccountId_Empty_ShouldFail()
    {
        var cmd = ValidCommand() with
        {
            Accounts = [new ClientAccountInput(Guid.Empty, HolderRole.Owner, true)]
        };
        var result = _validator.TestValidate(cmd);
        result.ShouldHaveAnyValidationError();
    }

    [Fact]
    public void EmptyAccounts_ShouldPass()
    {
        var result = _validator.TestValidate(ValidCommand() with { Accounts = [] });
        result.ShouldNotHaveAnyValidationErrors();
    }
}
