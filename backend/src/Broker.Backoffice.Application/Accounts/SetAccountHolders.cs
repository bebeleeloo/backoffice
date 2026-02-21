using Broker.Backoffice.Application.Abstractions;
using Broker.Backoffice.Domain.Accounts;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Broker.Backoffice.Application.Accounts;

public sealed record AccountHolderInput(Guid ClientId, HolderRole Role, bool IsPrimary);

public sealed record SetAccountHoldersCommand(
    Guid AccountId,
    List<AccountHolderInput> Holders) : IRequest<AccountDto>;

public sealed class SetAccountHoldersCommandValidator : AbstractValidator<SetAccountHoldersCommand>
{
    public SetAccountHoldersCommandValidator()
    {
        RuleFor(x => x.AccountId).NotEmpty();
        RuleForEach(x => x.Holders).ChildRules(h =>
        {
            h.RuleFor(x => x.ClientId).NotEmpty();
        });
    }
}

public sealed class SetAccountHoldersCommandHandler(
    IAppDbContext db,
    IDateTimeProvider clock) : IRequestHandler<SetAccountHoldersCommand, AccountDto>
{
    public async Task<AccountDto> Handle(SetAccountHoldersCommand request, CancellationToken ct)
    {
        var account = await db.Accounts
            .Include(a => a.Holders)
            .Include(a => a.Clearer)
            .Include(a => a.TradePlatform)
            .FirstOrDefaultAsync(a => a.Id == request.AccountId, ct)
            ?? throw new KeyNotFoundException($"Account {request.AccountId} not found");

        // Validate all clientIds exist
        var clientIds = request.Holders.Select(h => h.ClientId).Distinct().ToList();
        var existingCount = await db.Clients.CountAsync(c => clientIds.Contains(c.Id), ct);
        if (existingCount != clientIds.Count)
            throw new InvalidOperationException("One or more client IDs are invalid");

        // Replace semantics: remove all existing, add new
        account.Holders.Clear();

        var now = clock.UtcNow;
        foreach (var input in request.Holders)
        {
            account.Holders.Add(new AccountHolder
            {
                AccountId = account.Id,
                ClientId = input.ClientId,
                Role = input.Role,
                IsPrimary = input.IsPrimary,
                AddedAt = now
            });
        }

        await db.SaveChangesAsync(ct);

        // Reload with client navigation
        var result = await db.Accounts
            .Include(a => a.Clearer)
            .Include(a => a.TradePlatform)
            .Include(a => a.Holders).ThenInclude(h => h.Client)
            .FirstAsync(a => a.Id == account.Id, ct);

        return GetAccountByIdQueryHandler.ToDto(result);
    }
}
