using Broker.Backoffice.Application.Abstractions;
using Broker.Backoffice.Domain.Accounts;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Broker.Backoffice.Application.TradePlatforms;

public sealed record CreateTradePlatformCommand(string Name, string? Description) : IRequest<TradePlatformDto>;

public sealed class CreateTradePlatformCommandValidator : AbstractValidator<CreateTradePlatformCommand>
{
    public CreateTradePlatformCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(500);
    }
}

public sealed class CreateTradePlatformCommandHandler(IAppDbContext db)
    : IRequestHandler<CreateTradePlatformCommand, TradePlatformDto>
{
    public async Task<TradePlatformDto> Handle(CreateTradePlatformCommand request, CancellationToken ct)
    {
        if (await db.TradePlatforms.AnyAsync(t => t.Name == request.Name, ct))
            throw new InvalidOperationException($"Trade platform '{request.Name}' already exists");

        var entity = new TradePlatform { Id = Guid.NewGuid(), Name = request.Name, Description = request.Description, IsActive = true };
        db.TradePlatforms.Add(entity);
        await db.SaveChangesAsync(ct);
        return new TradePlatformDto(entity.Id, entity.Name, entity.Description, entity.IsActive);
    }
}
