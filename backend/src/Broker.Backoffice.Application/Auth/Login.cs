using Broker.Backoffice.Application.Abstractions;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Broker.Backoffice.Domain.Identity;

namespace Broker.Backoffice.Application.Auth;

public sealed record LoginCommand(string Username, string Password) : IRequest<AuthResponse>;

public sealed class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.Username).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Password).NotEmpty();
    }
}

public sealed class LoginCommandHandler(
    IAppDbContext db,
    IJwtTokenService jwt,
    IDateTimeProvider clock,
    PasswordHasher<User> hasher) : IRequestHandler<LoginCommand, AuthResponse>
{
    public async Task<AuthResponse> Handle(LoginCommand request, CancellationToken ct)
    {
        var user = await db.Users
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role).ThenInclude(r => r.RolePermissions).ThenInclude(rp => rp.Permission)
            .Include(u => u.PermissionOverrides).ThenInclude(po => po.Permission)
            .FirstOrDefaultAsync(u => u.Username == request.Username, ct)
            ?? throw new UnauthorizedAccessException("Invalid credentials");

        var result = hasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
        if (result == PasswordVerificationResult.Failed)
            throw new UnauthorizedAccessException("Invalid credentials");

        if (!user.IsActive)
            throw new UnauthorizedAccessException("Account is disabled");

        var permissions = GetEffectivePermissions(user);
        var tokens = jwt.GenerateTokens(user, permissions);

        db.UserRefreshTokens.Add(new UserRefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = jwt.HashToken(tokens.RefreshToken),
            ExpiresAt = clock.UtcNow.AddDays(7),
            CreatedAt = clock.UtcNow
        });

        await db.SaveChangesAsync(ct);

        return new AuthResponse(tokens.AccessToken, tokens.RefreshToken, tokens.AccessTokenExpires);
    }

    public static List<string> GetEffectivePermissions(User user)
    {
        var rolePerms = user.UserRoles
            .SelectMany(ur => ur.Role.RolePermissions)
            .Select(rp => rp.Permission.Code)
            .ToHashSet();

        foreach (var ov in user.PermissionOverrides)
        {
            if (ov.IsAllowed) rolePerms.Add(ov.Permission.Code);
            else rolePerms.Remove(ov.Permission.Code);
        }

        return rolePerms.ToList();
    }
}
