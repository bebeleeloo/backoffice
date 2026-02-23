using Broker.Backoffice.Application.Abstractions;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Broker.Backoffice.Application.Auth;

public sealed record UpdateProfileCommand(Guid UserId, string? FullName, string Email) : IRequest<UserProfileResponse>;

public sealed class UpdateProfileCommandValidator : AbstractValidator<UpdateProfileCommand>
{
    public UpdateProfileCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.FullName).MaximumLength(200);
    }
}

public sealed class UpdateProfileCommandHandler(IAppDbContext db)
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

        user.FullName = request.FullName;
        user.Email = request.Email;
        await db.SaveChangesAsync(ct);

        var permissions = LoginCommandHandler.GetEffectivePermissions(user);
        return new UserProfileResponse(
            user.Id, user.Username, user.Email, user.FullName,
            user.UserRoles.Select(ur => ur.Role.Name).ToList(),
            permissions,
            user.DataScopes.Select(ds => new DataScopeDto(ds.ScopeType, ds.ScopeValue)).ToList());
    }
}
