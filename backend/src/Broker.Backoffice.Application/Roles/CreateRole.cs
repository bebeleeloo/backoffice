using System.Text.Json;
using Broker.Backoffice.Application.Abstractions;
using Broker.Backoffice.Domain.Identity;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Broker.Backoffice.Application.Roles;

public sealed record CreateRoleCommand(string Name, string? Description) : IRequest<RoleDto>;

public sealed class CreateRoleCommandValidator : AbstractValidator<CreateRoleCommand>
{
    public CreateRoleCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
    }
}

public sealed class CreateRoleCommandHandler(
    IAppDbContext db, IDateTimeProvider clock, ICurrentUser currentUser, IAuditContext audit)
    : IRequestHandler<CreateRoleCommand, RoleDto>
{
    public async Task<RoleDto> Handle(CreateRoleCommand request, CancellationToken ct)
    {
        if (await db.Roles.AnyAsync(r => r.Name == request.Name, ct))
            throw new InvalidOperationException($"Role '{request.Name}' already exists");

        var role = new Role
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Description = request.Description,
            CreatedAt = clock.UtcNow,
            CreatedBy = currentUser.UserName
        };
        db.Roles.Add(role);
        await db.SaveChangesAsync(ct);

        audit.EntityType = "Role";
        audit.EntityId = role.Id.ToString();
        audit.AfterJson = JsonSerializer.Serialize(new { role.Id, role.Name });

        return new RoleDto(role.Id, role.Name, role.Description, role.IsSystem, [], role.CreatedAt, role.RowVersion);
    }
}
