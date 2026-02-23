using System.Text.Json;
using Broker.Backoffice.Application.Abstractions;
using Broker.Backoffice.Domain.Identity;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Broker.Backoffice.Application.Users;

public sealed record UpdateUserCommand(
    Guid Id, string Email, string? FullName, bool IsActive,
    List<Guid> RoleIds, byte[] RowVersion) : IRequest<UserDto>;

public sealed class UpdateUserCommandValidator : AbstractValidator<UpdateUserCommand>
{
    public UpdateUserCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.RowVersion).NotEmpty();
    }
}

public sealed class UpdateUserCommandHandler(
    IAppDbContext db,
    IDateTimeProvider clock,
    ICurrentUser currentUser,
    IAuditContext audit) : IRequestHandler<UpdateUserCommand, UserDto>
{
    public async Task<UserDto> Handle(UpdateUserCommand request, CancellationToken ct)
    {
        var user = await db.Users
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Id == request.Id, ct)
            ?? throw new KeyNotFoundException($"User {request.Id} not found");

        var before = JsonSerializer.Serialize(new { user.Id, user.Email, user.FullName, user.IsActive });

        // Set concurrency token
        db.Users.Entry(user).Property(u => u.RowVersion).OriginalValue = request.RowVersion;

        user.Email = request.Email;
        user.FullName = request.FullName;
        user.IsActive = request.IsActive;
        user.UpdatedAt = clock.UtcNow;
        user.UpdatedBy = currentUser.UserName;

        // Sync roles
        var currentRoleIds = user.UserRoles.Select(ur => ur.RoleId).ToHashSet();
        var toRemove = user.UserRoles.Where(ur => !request.RoleIds.Contains(ur.RoleId)).ToList();
        foreach (var r in toRemove) user.UserRoles.Remove(r);

        var newRoleIds = request.RoleIds.Where(id => !currentRoleIds.Contains(id)).ToList();
        if (newRoleIds.Count > 0)
        {
            // Load roles into context so EF fixup populates ur.Role navigation
            await db.Roles.Where(r => newRoleIds.Contains(r.Id)).LoadAsync(ct);
        }

        foreach (var roleId in newRoleIds)
        {
            var ur = new UserRole
            {
                UserId = user.Id, RoleId = roleId,
                CreatedAt = clock.UtcNow, CreatedBy = currentUser.UserName
            };
            user.UserRoles.Add(ur);
            db.UserRoles.Add(ur);
        }

        await db.SaveChangesAsync(ct);

        audit.EntityType = "User";
        audit.EntityId = user.Id.ToString();
        audit.BeforeJson = before;
        audit.AfterJson = JsonSerializer.Serialize(new { user.Id, user.Email, user.FullName, user.IsActive });

        return new UserDto(user.Id, user.Username, user.Email, user.FullName, user.IsActive,
            user.UserRoles.Select(ur => ur.Role.Name).ToList(), user.CreatedAt, user.RowVersion);
    }
}
