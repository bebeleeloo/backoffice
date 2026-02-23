using Broker.Backoffice.Application.Abstractions;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Broker.Backoffice.Application.TradePlatforms;

public sealed record UpdateTradePlatformCommand(Guid Id, string Name, string? Description, bool IsActive) : IRequest<TradePlatformDto>;

public sealed class UpdateTradePlatformCommandValidator : AbstractValidator<UpdateTradePlatformCommand>
{
    public UpdateTradePlatformCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(500);
    }
}

public sealed class UpdateTradePlatformCommandHandler(IAppDbContext db)
    : IRequestHandler<UpdateTradePlatformCommand, TradePlatformDto>
{
    public async Task<TradePlatformDto> Handle(UpdateTradePlatformCommand request, CancellationToken ct)
    {
        var entity = await db.TradePlatforms.FirstOrDefaultAsync(t => t.Id == request.Id, ct)
            ?? throw new KeyNotFoundException($"Trade platform {request.Id} not found");

        if (await db.TradePlatforms.AnyAsync(t => t.Name == request.Name && t.Id != request.Id, ct))
            throw new InvalidOperationException($"Trade platform '{request.Name}' already exists");

        entity.Name = request.Name;
        entity.Description = request.Description;
        entity.IsActive = request.IsActive;
        await db.SaveChangesAsync(ct);
        return new TradePlatformDto(entity.Id, entity.Name, entity.Description, entity.IsActive);
    }
}
