using Broker.Backoffice.Application.Abstractions;
using Broker.Backoffice.Application.Accounts;
using Broker.Backoffice.Domain.Accounts;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Broker.Backoffice.Application.Clients;

public sealed record ClientAccountInput(Guid AccountId, HolderRole Role, bool IsPrimary);

public sealed record SetClientAccountsCommand(
    Guid ClientId,
    List<ClientAccountInput> Accounts) : IRequest<IReadOnlyList<ClientAccountDto>>;

public sealed class SetClientAccountsCommandValidator : AbstractValidator<SetClientAccountsCommand>
{
    public SetClientAccountsCommandValidator()
    {
        RuleFor(x => x.ClientId).NotEmpty();
        RuleForEach(x => x.Accounts).ChildRules(a =>
        {
            a.RuleFor(x => x.AccountId).NotEmpty();
        });
    }
}

public sealed class SetClientAccountsCommandHandler(
    IAppDbContext db,
    IDateTimeProvider clock) : IRequestHandler<SetClientAccountsCommand, IReadOnlyList<ClientAccountDto>>
{
    public async Task<IReadOnlyList<ClientAccountDto>> Handle(SetClientAccountsCommand request, CancellationToken ct)
    {
        var client = await db.Clients
            .FirstOrDefaultAsync(c => c.Id == request.ClientId, ct)
            ?? throw new KeyNotFoundException($"Client {request.ClientId} not found");

        // Validate all accountIds exist
        var accountIds = request.Accounts.Select(a => a.AccountId).Distinct().ToList();
        var existingCount = await db.Accounts.CountAsync(a => accountIds.Contains(a.Id), ct);
        if (existingCount != accountIds.Count)
            throw new InvalidOperationException("One or more account IDs are invalid");

        // Remove existing holders for this client
        var existing = await db.AccountHolders
            .Where(h => h.ClientId == request.ClientId)
            .ToListAsync(ct);
        db.AccountHolders.RemoveRange(existing);

        // Add new
        var now = clock.UtcNow;
        foreach (var input in request.Accounts)
        {
            db.AccountHolders.Add(new AccountHolder
            {
                AccountId = input.AccountId,
                ClientId = client.Id,
                Role = input.Role,
                IsPrimary = input.IsPrimary,
                AddedAt = now
            });
        }

        await db.SaveChangesAsync(ct);

        return await db.AccountHolders
            .Where(h => h.ClientId == request.ClientId)
            .Include(h => h.Account)
            .Select(h => new ClientAccountDto(
                h.AccountId,
                h.Account!.Number,
                h.Account.Status,
                h.Role,
                h.IsPrimary,
                h.AddedAt))
            .ToListAsync(ct);
    }
}
