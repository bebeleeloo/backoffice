using Broker.Backoffice.Application.Abstractions;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Broker.Backoffice.Application.Currencies;

public sealed record UpdateCurrencyCommand(Guid Id, string Code, string Name, string? Symbol, bool IsActive) : IRequest<CurrencyDto>;

public sealed class UpdateCurrencyCommandValidator : AbstractValidator<UpdateCurrencyCommand>
{
    public UpdateCurrencyCommandValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(10);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Symbol).MaximumLength(10);
    }
}

public sealed class UpdateCurrencyCommandHandler(IAppDbContext db)
    : IRequestHandler<UpdateCurrencyCommand, CurrencyDto>
{
    public async Task<CurrencyDto> Handle(UpdateCurrencyCommand request, CancellationToken ct)
    {
        var entity = await db.Currencies.FirstOrDefaultAsync(c => c.Id == request.Id, ct)
            ?? throw new KeyNotFoundException($"Currency {request.Id} not found");

        if (await db.Currencies.AnyAsync(c => c.Code == request.Code && c.Id != request.Id, ct))
            throw new InvalidOperationException($"Currency '{request.Code}' already exists");

        entity.Code = request.Code;
        entity.Name = request.Name;
        entity.Symbol = request.Symbol;
        entity.IsActive = request.IsActive;
        await db.SaveChangesAsync(ct);
        return new CurrencyDto(entity.Id, entity.Code, entity.Name, entity.Symbol, entity.IsActive);
    }
}
