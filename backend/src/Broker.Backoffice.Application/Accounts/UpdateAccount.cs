using System.Text.Json;
using Broker.Backoffice.Application.Abstractions;
using Broker.Backoffice.Domain.Accounts;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Broker.Backoffice.Application.Accounts;

public sealed record UpdateAccountCommand(
    Guid Id,
    string Number,
    Guid? ClearerId,
    Guid? TradePlatformId,
    AccountStatus Status,
    AccountType AccountType,
    MarginType MarginType,
    OptionLevel OptionLevel,
    Tariff Tariff,
    DeliveryType? DeliveryType,
    DateTime? OpenedAt,
    DateTime? ClosedAt,
    string? Comment,
    string? ExternalId,
    byte[] RowVersion) : IRequest<AccountDto>;

public sealed class UpdateAccountCommandValidator : AbstractValidator<UpdateAccountCommand>
{
    public UpdateAccountCommandValidator()
    {
        RuleFor(x => x.Number).NotEmpty().MaximumLength(50);
        RuleFor(x => x.RowVersion).NotEmpty();
        RuleFor(x => x.Comment).MaximumLength(500);
        RuleFor(x => x.ExternalId).MaximumLength(64);
    }
}

public sealed class UpdateAccountCommandHandler(
    IAppDbContext db,
    IDateTimeProvider clock,
    ICurrentUser currentUser,
    IAuditContext audit) : IRequestHandler<UpdateAccountCommand, AccountDto>
{
    public async Task<AccountDto> Handle(UpdateAccountCommand request, CancellationToken ct)
    {
        var account = await db.Accounts
            .FirstOrDefaultAsync(a => a.Id == request.Id, ct)
            ?? throw new KeyNotFoundException($"Account {request.Id} not found");

        if (await db.Accounts.AnyAsync(a => a.Number == request.Number && a.Id != request.Id, ct))
            throw new InvalidOperationException($"Account with number '{request.Number}' already exists");

        var before = JsonSerializer.Serialize(new { account.Id, account.Number, account.Status });
        db.Accounts.Entry(account).Property(a => a.RowVersion).OriginalValue = request.RowVersion;

        account.Number = request.Number;
        account.ClearerId = request.ClearerId;
        account.TradePlatformId = request.TradePlatformId;
        account.Status = request.Status;
        account.AccountType = request.AccountType;
        account.MarginType = request.MarginType;
        account.OptionLevel = request.OptionLevel;
        account.Tariff = request.Tariff;
        account.DeliveryType = request.DeliveryType;
        account.OpenedAt = request.OpenedAt;
        account.ClosedAt = request.ClosedAt;
        account.Comment = request.Comment;
        account.ExternalId = request.ExternalId;
        account.UpdatedAt = clock.UtcNow;
        account.UpdatedBy = currentUser.UserName;

        await db.SaveChangesAsync(ct);

        var updated = await db.Accounts
            .Include(a => a.Clearer)
            .Include(a => a.TradePlatform)
            .Include(a => a.Holders).ThenInclude(h => h.Client)
            .FirstAsync(a => a.Id == account.Id, ct);

        audit.EntityType = "Account";
        audit.EntityId = account.Id.ToString();
        audit.BeforeJson = before;
        audit.AfterJson = JsonSerializer.Serialize(new { updated.Id, updated.Number, updated.Status });

        return GetAccountByIdQueryHandler.ToDto(updated);
    }
}
