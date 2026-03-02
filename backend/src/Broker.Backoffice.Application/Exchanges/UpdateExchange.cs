using System.Text.Json;
using Broker.Backoffice.Application.Abstractions;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Broker.Backoffice.Application.Exchanges;

public sealed record UpdateExchangeCommand(Guid Id, string Code, string Name, Guid? CountryId, bool IsActive) : IRequest<ExchangeDto>;

public sealed class UpdateExchangeCommandValidator : AbstractValidator<UpdateExchangeCommand>
{
    public UpdateExchangeCommandValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
    }
}

public sealed class UpdateExchangeCommandHandler(IAppDbContext db, IAuditContext audit)
    : IRequestHandler<UpdateExchangeCommand, ExchangeDto>
{
    public async Task<ExchangeDto> Handle(UpdateExchangeCommand request, CancellationToken ct)
    {
        var entity = await db.Exchanges.FirstOrDefaultAsync(e => e.Id == request.Id, ct)
            ?? throw new KeyNotFoundException($"Exchange {request.Id} not found");

        if (await db.Exchanges.AnyAsync(e => e.Code == request.Code && e.Id != request.Id, ct))
            throw new InvalidOperationException($"Exchange '{request.Code}' already exists");

        audit.EntityType = "Exchange";
        audit.EntityId = entity.Id.ToString();
        audit.BeforeJson = JsonSerializer.Serialize(new { entity.Id, entity.Code, entity.Name, entity.CountryId, entity.IsActive });

        entity.Code = request.Code;
        entity.Name = request.Name;
        entity.CountryId = request.CountryId;
        entity.IsActive = request.IsActive;
        await db.SaveChangesAsync(ct);

        audit.AfterJson = JsonSerializer.Serialize(new { entity.Id, entity.Code, entity.Name, entity.CountryId, entity.IsActive });

        return new ExchangeDto(entity.Id, entity.Code, entity.Name, entity.CountryId, entity.IsActive);
    }
}
