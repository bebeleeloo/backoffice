using System.Text.Json;
using Broker.Auth.Application.Abstractions;
using Broker.Auth.Application.Common;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Broker.Auth.Application.Auth;

public sealed record UpdateProfileCommand(Guid UserId, string? FullName, string Email) : IRequest<UserProfileResponse>;

public sealed class UpdateProfileCommandValidator : AbstractValidator<UpdateProfileCommand>
{
    public UpdateProfileCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.FullName).MaximumLength(200);
    }
}

public sealed class UpdateProfileCommandHandler(
    IAuthDbContext db,
    IAuditContext audit,
    IDateTimeProvider clock,
    ICurrentUser currentUser)
    : IRequestHandler<UpdateProfileCommand, UserProfileResponse>
{
    public async Task<UserProfileResponse> Handle(UpdateProfileCommand request, CancellationToken ct)
    {
        var user = await db.Users
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
                .ThenInclude(r => r.RolePermissions).ThenInclude(rp => rp.Permission)
            .Include(u => u.PermissionOverrides).ThenInclude(po => po.Permission)
            .Include(u => u.DataScopes)
            .FirstOrDefaultAsync(u => u.Id == request.UserId, ct)
            ?? throw new KeyNotFoundException("User not found");

        if (await db.Users.AnyAsync(u => u.Email == request.Email && u.Id != request.UserId, ct))
            throw new InvalidOperationException("Email is already in use");

        audit.EntityType = "User";
        audit.EntityId = user.Id.ToString();
        audit.BeforeJson = JsonSerializer.Serialize(new { user.Id, user.Username, user.Email, user.FullName });

        user.FullName = request.FullName;
        user.Email = request.Email;
        user.UpdatedAt = clock.UtcNow;
        user.UpdatedBy = currentUser.UserId?.ToString();
        await db.SaveChangesAsync(ct);

        audit.AfterJson = JsonSerializer.Serialize(new { user.Id, user.Username, user.Email, user.FullName });

        var permissions = EffectivePermissionsResolver.GetEffectivePermissions(user);
        return new UserProfileResponse(
            user.Id, user.Username, user.Email, user.FullName,
            user.Photo != null,
            user.UserRoles.Select(ur => ur.Role.Name).ToList(),
            permissions,
            user.DataScopes.Select(ds => new DataScopeDto(ds.ScopeType, ds.ScopeValue)).ToList());
    }
}
