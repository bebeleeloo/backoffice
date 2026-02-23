using Broker.Backoffice.Application.Abstractions;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Broker.Backoffice.Application.Clearers;

public sealed record UpdateClearerCommand(Guid Id, string Name, string? Description, bool IsActive) : IRequest<ClearerDto>;

public sealed class UpdateClearerCommandValidator : AbstractValidator<UpdateClearerCommand>
{
    public UpdateClearerCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(500);
    }
}

public sealed class UpdateClearerCommandHandler(IAppDbContext db)
    : IRequestHandler<UpdateClearerCommand, ClearerDto>
{
    public async Task<ClearerDto> Handle(UpdateClearerCommand request, CancellationToken ct)
    {
        var entity = await db.Clearers.FirstOrDefaultAsync(c => c.Id == request.Id, ct)
            ?? throw new KeyNotFoundException($"Clearer {request.Id} not found");

        if (await db.Clearers.AnyAsync(c => c.Name == request.Name && c.Id != request.Id, ct))
            throw new InvalidOperationException($"Clearer '{request.Name}' already exists");

        entity.Name = request.Name;
        entity.Description = request.Description;
        entity.IsActive = request.IsActive;
        await db.SaveChangesAsync(ct);
        return new ClearerDto(entity.Id, entity.Name, entity.Description, entity.IsActive);
    }
}
