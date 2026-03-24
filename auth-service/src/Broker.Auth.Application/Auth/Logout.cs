using Broker.Auth.Application.Abstractions;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Broker.Auth.Application.Auth;

public sealed record LogoutCommand(string RefreshToken) : IRequest;

public sealed class LogoutCommandValidator : AbstractValidator<LogoutCommand>
{
    public LogoutCommandValidator()
    {
        RuleFor(x => x.RefreshToken).NotEmpty();
    }
}

internal sealed class LogoutCommandHandler(
    IAuthDbContext db,
    IJwtTokenService jwt,
    IDateTimeProvider clock,
    IAuditContext audit) : IRequestHandler<LogoutCommand>
{
    public async Task Handle(LogoutCommand request, CancellationToken ct)
    {
        var tokenHash = jwt.HashToken(request.RefreshToken);

        var stored = await db.UserRefreshTokens
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash, ct);

        // Idempotent: return silently if token not found or already revoked
        if (stored == null)
            return;

        audit.EntityType = "RefreshToken";
        audit.EntityId = stored.UserId.ToString();

        if (stored.RevokedAt == null)
        {
            stored.RevokedAt = clock.UtcNow;
            await db.SaveChangesAsync(ct);
        }
    }
}
