using Broker.Backoffice.Application.Abstractions;
using Broker.Backoffice.Domain.Accounts;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Broker.Backoffice.Application.Clearers;

public sealed record CreateClearerCommand(string Name, string? Description) : IRequest<ClearerDto>;

public sealed class CreateClearerCommandValidator : AbstractValidator<CreateClearerCommand>
{
    public CreateClearerCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(500);
    }
}

public sealed class CreateClearerCommandHandler(IAppDbContext db)
    : IRequestHandler<CreateClearerCommand, ClearerDto>
{
    public async Task<ClearerDto> Handle(CreateClearerCommand request, CancellationToken ct)
    {
        if (await db.Clearers.AnyAsync(c => c.Name == request.Name, ct))
            throw new InvalidOperationException($"Clearer '{request.Name}' already exists");

        var entity = new Clearer { Id = Guid.NewGuid(), Name = request.Name, Description = request.Description, IsActive = true };
        db.Clearers.Add(entity);
        await db.SaveChangesAsync(ct);
        return new ClearerDto(entity.Id, entity.Name, entity.Description, entity.IsActive);
    }
}
