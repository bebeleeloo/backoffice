using System.Text.Json;
using Broker.Auth.Application.Abstractions;
using Broker.Auth.Domain.Identity;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Broker.Auth.Application.Users;

public sealed record UpdateUserCommand(
    Guid Id, string Email, string? FullName, bool IsActive,
    List<Guid> RoleIds, uint RowVersion) : IRequest<UserDto>;

public sealed class UpdateUserCommandValidator : AbstractValidator<UpdateUserCommand>
{
    public UpdateUserCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.RowVersion).NotEmpty();
    }
}

public sealed class UpdateUserCommandHandler(
    IAuthDbContext db,
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

        db.Users.Entry(user).Property(u => u.RowVersion).OriginalValue = request.RowVersion;

        if (await db.Users.AnyAsync(u => u.Email == request.Email && u.Id != request.Id, ct))
            throw new InvalidOperationException($"Email '{request.Email}' is already in use");

        var before = JsonSerializer.Serialize(new { user.Id, user.Email, user.FullName, user.IsActive });

        var wasActive = user.IsActive;

        user.Email = request.Email;
        user.FullName = request.FullName;
        user.IsActive = request.IsActive;
        user.UpdatedAt = clock.UtcNow;
        user.UpdatedBy = currentUser.UserName;

        // Revoke all refresh tokens when user is deactivated
        if (wasActive && !request.IsActive)
        {
            var tokens = await db.UserRefreshTokens
                .Where(t => t.UserId == user.Id && t.RevokedAt == null)
                .ToListAsync(ct);
            foreach (var t in tokens) t.RevokedAt = clock.UtcNow;
        }

        var currentRoleIds = user.UserRoles.Select(ur => ur.RoleId).ToHashSet();
        var toRemove = user.UserRoles.Where(ur => !request.RoleIds.Contains(ur.RoleId)).ToList();
        foreach (var r in toRemove) user.UserRoles.Remove(r);

        var newRoleIds = request.RoleIds.Where(id => !currentRoleIds.Contains(id)).ToList();
        if (newRoleIds.Count > 0)
        {
            var foundCount = await db.Roles.CountAsync(r => newRoleIds.Contains(r.Id), ct);
            if (foundCount != newRoleIds.Count)
                throw new KeyNotFoundException("One or more roles not found");
            await db.Roles.Where(r => newRoleIds.Contains(r.Id)).LoadAsync(ct);
        }

        foreach (var roleId in newRoleIds)
        {
            var ur = new UserRole
            {
                Id = Guid.NewGuid(),
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
            user.Photo != null,
            user.UserRoles.Select(ur => ur.Role.Name).ToList(), user.CreatedAt, user.RowVersion);
    }
}
