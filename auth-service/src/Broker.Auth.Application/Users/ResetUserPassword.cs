using Broker.Auth.Application.Abstractions;
using Broker.Auth.Domain.Identity;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Broker.Auth.Application.Users;

public sealed record ResetUserPasswordCommand(Guid UserId, string NewPassword) : IRequest;

public sealed class ResetUserPasswordCommandValidator : AbstractValidator<ResetUserPasswordCommand>
{
    public ResetUserPasswordCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.NewPassword).NotEmpty().MinimumLength(8)
            .Matches("[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
            .Matches("[a-z]").WithMessage("Password must contain at least one lowercase letter.")
            .Matches("[0-9]").WithMessage("Password must contain at least one digit.")
            .Matches("[^a-zA-Z0-9]").WithMessage("Password must contain at least one special character.");
    }
}

internal sealed class ResetUserPasswordCommandHandler(
    IAuthDbContext db,
    PasswordHasher<User> hasher,
    IDateTimeProvider clock,
    ICurrentUser currentUser,
    IAuditContext audit) : IRequestHandler<ResetUserPasswordCommand>
{
    public async Task Handle(ResetUserPasswordCommand request, CancellationToken ct)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == request.UserId, ct)
            ?? throw new KeyNotFoundException("User not found");

        user.PasswordHash = hasher.HashPassword(user, request.NewPassword);
        user.UpdatedAt = clock.UtcNow;
        user.UpdatedBy = currentUser.UserName;

        // Revoke all refresh tokens on password reset
        var tokens = await db.UserRefreshTokens
            .Where(t => t.UserId == user.Id && t.RevokedAt == null)
            .ToListAsync(ct);
        foreach (var t in tokens) t.RevokedAt = clock.UtcNow;

        await db.SaveChangesAsync(ct);

        audit.EntityType = "User";
        audit.EntityId = user.Id.ToString();
    }
}
