using System.Text.Json;
using Broker.Backoffice.Application.Abstractions;
using Broker.Backoffice.Domain.Accounts;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Broker.Backoffice.Application.Accounts;

public sealed record CreateAccountCommand(
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
    string? ExternalId) : IRequest<AccountDto>;

public sealed class CreateAccountCommandValidator : AbstractValidator<CreateAccountCommand>
{
    public CreateAccountCommandValidator()
    {
        RuleFor(x => x.Number).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Comment).MaximumLength(500);
        RuleFor(x => x.ExternalId).MaximumLength(64);
    }
}

public sealed class CreateAccountCommandHandler(
    IAppDbContext db,
    IDateTimeProvider clock,
    ICurrentUser currentUser,
    IAuditContext audit) : IRequestHandler<CreateAccountCommand, AccountDto>
{
    public async Task<AccountDto> Handle(CreateAccountCommand request, CancellationToken ct)
    {
        if (await db.Accounts.AnyAsync(a => a.Number == request.Number, ct))
            throw new InvalidOperationException($"Account with number '{request.Number}' already exists");

        var account = new Account
        {
            Id = Guid.NewGuid(),
            Number = request.Number,
            ClearerId = request.ClearerId,
            TradePlatformId = request.TradePlatformId,
            Status = request.Status,
            AccountType = request.AccountType,
            MarginType = request.MarginType,
            OptionLevel = request.OptionLevel,
            Tariff = request.Tariff,
            DeliveryType = request.DeliveryType,
            OpenedAt = request.OpenedAt,
            ClosedAt = request.ClosedAt,
            Comment = request.Comment,
            ExternalId = request.ExternalId,
            CreatedAt = clock.UtcNow,
            CreatedBy = currentUser.UserName
        };

        db.Accounts.Add(account);
        await db.SaveChangesAsync(ct);

        await db.Accounts.Entry(account).Reference(a => a.Clearer).LoadAsync(ct);
        await db.Accounts.Entry(account).Reference(a => a.TradePlatform).LoadAsync(ct);

        audit.EntityType = "Account";
        audit.EntityId = account.Id.ToString();
        audit.AfterJson = JsonSerializer.Serialize(new { account.Id, account.Number, account.Status });

        return GetAccountByIdQueryHandler.ToDto(account);
    }
}
