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
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash, ct)
            ?? throw new UnauthorizedAccessException("Invalid refresh token");

        audit.EntityType = "RefreshToken";
        audit.EntityId = stored.UserId.ToString();

        if (stored.RevokedAt == null)
        {
            stored.RevokedAt = clock.UtcNow;
            await db.SaveChangesAsync(ct);
        }
    }
}
