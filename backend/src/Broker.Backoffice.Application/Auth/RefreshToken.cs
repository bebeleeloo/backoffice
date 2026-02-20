using Broker.Backoffice.Application.Abstractions;
using Broker.Backoffice.Domain.Identity;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Broker.Backoffice.Application.Auth;

public sealed record RefreshTokenCommand(string RefreshToken) : IRequest<AuthResponse>;

public sealed class RefreshTokenCommandHandler(
    IAppDbContext db,
    IJwtTokenService jwt,
    IDateTimeProvider clock) : IRequestHandler<RefreshTokenCommand, AuthResponse>
{
    public async Task<AuthResponse> Handle(RefreshTokenCommand request, CancellationToken ct)
    {
        var tokenHash = jwt.HashToken(request.RefreshToken);

        var stored = await db.UserRefreshTokens
            .Include(t => t.User)
                .ThenInclude(u => u.UserRoles).ThenInclude(ur => ur.Role).ThenInclude(r => r.RolePermissions).ThenInclude(rp => rp.Permission)
            .Include(t => t.User)
                .ThenInclude(u => u.PermissionOverrides).ThenInclude(po => po.Permission)
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash, ct)
            ?? throw new UnauthorizedAccessException("Invalid refresh token");

        if (!stored.IsActive)
        {
            // Reuse detected — revoke the whole family
            var family = await db.UserRefreshTokens
                .Where(t => t.UserId == stored.UserId && t.RevokedAt == null)
                .ToListAsync(ct);
            foreach (var t in family) t.RevokedAt = clock.UtcNow;
            await db.SaveChangesAsync(ct);
            throw new UnauthorizedAccessException("Token reuse detected");
        }

        if (!stored.User.IsActive)
            throw new UnauthorizedAccessException("Account is disabled");

        var permissions = LoginCommandHandler.GetEffectivePermissions(stored.User);
        var tokens = jwt.GenerateTokens(stored.User, permissions);

        // Rotate
        stored.RevokedAt = clock.UtcNow;
        stored.ReplacedByTokenHash = jwt.HashToken(tokens.RefreshToken);

        db.UserRefreshTokens.Add(new UserRefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = stored.UserId,
            TokenHash = jwt.HashToken(tokens.RefreshToken),
            ExpiresAt = clock.UtcNow.AddDays(7),
            CreatedAt = clock.UtcNow
        });

        await db.SaveChangesAsync(ct);

        return new AuthResponse(tokens.AccessToken, tokens.RefreshToken, tokens.AccessTokenExpires);
    }
}
