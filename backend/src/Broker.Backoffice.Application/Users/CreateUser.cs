using System.Text.Json;
using Broker.Backoffice.Application.Abstractions;
using Broker.Backoffice.Domain.Identity;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Broker.Backoffice.Application.Users;

public sealed record CreateUserCommand(
    string Username, string Email, string Password,
    string? FullName, bool IsActive, List<Guid> RoleIds) : IRequest<UserDto>;

public sealed class CreateUserCommandValidator : AbstractValidator<CreateUserCommand>
{
    public CreateUserCommandValidator()
    {
        RuleFor(x => x.Username).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.Password).NotEmpty().MinimumLength(6);
    }
}

public sealed class CreateUserCommandHandler(
    IAppDbContext db,
    IDateTimeProvider clock,
    ICurrentUser currentUser,
    IAuditContext audit,
    PasswordHasher<User> hasher) : IRequestHandler<CreateUserCommand, UserDto>
{
    public async Task<UserDto> Handle(CreateUserCommand request, CancellationToken ct)
    {
        if (await db.Users.AnyAsync(u => u.Username == request.Username, ct))
            throw new InvalidOperationException($"Username '{request.Username}' is already taken");

        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = request.Username,
            Email = request.Email,
            FullName = request.FullName,
            IsActive = request.IsActive,
            CreatedAt = clock.UtcNow,
            CreatedBy = currentUser.UserName
        };
        user.PasswordHash = hasher.HashPassword(user, request.Password);

        if (request.RoleIds.Count > 0)
        {
            var roles = await db.Roles.Where(r => request.RoleIds.Contains(r.Id)).ToListAsync(ct);
            foreach (var role in roles)
                user.UserRoles.Add(new UserRole
                {
                    Id = Guid.NewGuid(), UserId = user.Id, RoleId = role.Id,
                    CreatedAt = clock.UtcNow, CreatedBy = currentUser.UserName
                });
        }

        db.Users.Add(user);
        await db.SaveChangesAsync(ct);

        audit.EntityType = "User";
        audit.EntityId = user.Id.ToString();
        audit.AfterJson = Truncate(JsonSerializer.Serialize(new { user.Id, user.Username, user.Email, user.FullName, user.IsActive }));

        return new UserDto(user.Id, user.Username, user.Email, user.FullName, user.IsActive,
            user.UserRoles.Select(ur => ur.Role?.Name ?? "").ToList(), user.CreatedAt, user.RowVersion);
    }

    private static string Truncate(string json, int max = 16384) =>
        json.Length <= max ? json : json[..max];
}
