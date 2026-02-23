using Broker.Backoffice.Application.Abstractions;
using Broker.Backoffice.Domain.Instruments;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Broker.Backoffice.Application.Currencies;

public sealed record CreateCurrencyCommand(string Code, string Name, string? Symbol) : IRequest<CurrencyDto>;

public sealed class CreateCurrencyCommandValidator : AbstractValidator<CreateCurrencyCommand>
{
    public CreateCurrencyCommandValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(10);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Symbol).MaximumLength(10);
    }
}

public sealed class CreateCurrencyCommandHandler(IAppDbContext db)
    : IRequestHandler<CreateCurrencyCommand, CurrencyDto>
{
    public async Task<CurrencyDto> Handle(CreateCurrencyCommand request, CancellationToken ct)
    {
        if (await db.Currencies.AnyAsync(c => c.Code == request.Code, ct))
            throw new InvalidOperationException($"Currency '{request.Code}' already exists");

        var entity = new Currency { Id = Guid.NewGuid(), Code = request.Code, Name = request.Name, Symbol = request.Symbol, IsActive = true };
        db.Currencies.Add(entity);
        await db.SaveChangesAsync(ct);
        return new CurrencyDto(entity.Id, entity.Code, entity.Name, entity.Symbol, entity.IsActive);
    }
}
