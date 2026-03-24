using Broker.Auth.Application.Abstractions;
using Broker.Auth.Application.Common;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Broker.Auth.Domain.Identity;

namespace Broker.Auth.Application.Auth;

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
    IAuthDbContext db,
    IJwtTokenService jwt,
    IDateTimeProvider clock,
    PasswordHasher<User> hasher) : IRequestHandler<LoginCommand, AuthResponse>
{
    private const string DummyHash = "AQAAAAIAAYagAAAAEDummyHashForTimingProtection==";

    public async Task<AuthResponse> Handle(LoginCommand request, CancellationToken ct)
    {
        // Minimal query first — only fields needed for auth check
        var user = await db.Users
            .FirstOrDefaultAsync(u => u.Username == request.Username, ct);

        // Always verify password to prevent timing oracle (user enumeration)
        var hashToVerify = user?.PasswordHash ?? DummyHash;
        var verifyResult = hasher.VerifyHashedPassword(user ?? new User(), hashToVerify, request.Password);

        if (user == null || !user.IsActive || verifyResult == PasswordVerificationResult.Failed)
            throw new UnauthorizedAccessException("Invalid credentials");

        if (verifyResult == PasswordVerificationResult.SuccessRehashNeeded)
        {
            user.PasswordHash = hasher.HashPassword(user, request.Password);
        }

        // Load full graph only after successful authentication
        await db.Users.Entry(user)
            .Collection(u => u.UserRoles).Query()
            .Include(ur => ur.Role).ThenInclude(r => r.RolePermissions).ThenInclude(rp => rp.Permission)
            .LoadAsync(ct);
        await db.Users.Entry(user)
            .Collection(u => u.PermissionOverrides).Query()
            .Include(po => po.Permission)
            .LoadAsync(ct);

        var permissions = EffectivePermissionsResolver.GetEffectivePermissions(user);
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

}
