using Broker.Backoffice.Application.Abstractions;
using Broker.Backoffice.Domain.Instruments;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Broker.Backoffice.Application.Exchanges;

public sealed record CreateExchangeCommand(string Code, string Name, Guid? CountryId) : IRequest<ExchangeDto>;

public sealed class CreateExchangeCommandValidator : AbstractValidator<CreateExchangeCommand>
{
    public CreateExchangeCommandValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
    }
}

public sealed class CreateExchangeCommandHandler(IAppDbContext db)
    : IRequestHandler<CreateExchangeCommand, ExchangeDto>
{
    public async Task<ExchangeDto> Handle(CreateExchangeCommand request, CancellationToken ct)
    {
        if (await db.Exchanges.AnyAsync(e => e.Code == request.Code, ct))
            throw new InvalidOperationException($"Exchange '{request.Code}' already exists");

        var entity = new Exchange { Id = Guid.NewGuid(), Code = request.Code, Name = request.Name, CountryId = request.CountryId, IsActive = true };
        db.Exchanges.Add(entity);
        await db.SaveChangesAsync(ct);
        return new ExchangeDto(entity.Id, entity.Code, entity.Name, entity.CountryId, entity.IsActive);
    }
}
