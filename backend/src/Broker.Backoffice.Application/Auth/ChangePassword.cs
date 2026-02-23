using Broker.Backoffice.Application.Abstractions;
using Broker.Backoffice.Domain.Identity;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Broker.Backoffice.Application.Auth;

public sealed record ChangePasswordCommand(Guid UserId, string CurrentPassword, string NewPassword) : IRequest;

public sealed class ChangePasswordCommandValidator : AbstractValidator<ChangePasswordCommand>
{
    public ChangePasswordCommandValidator()
    {
        RuleFor(x => x.CurrentPassword).NotEmpty();
        RuleFor(x => x.NewPassword).NotEmpty().MinimumLength(6);
    }
}

public sealed class ChangePasswordCommandHandler(
    IAppDbContext db,
    PasswordHasher<User> hasher) : IRequestHandler<ChangePasswordCommand>
{
    public async Task Handle(ChangePasswordCommand request, CancellationToken ct)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == request.UserId, ct)
            ?? throw new KeyNotFoundException("User not found");

        var result = hasher.VerifyHashedPassword(user, user.PasswordHash, request.CurrentPassword);
        if (result == PasswordVerificationResult.Failed)
            throw new UnauthorizedAccessException("Current password is incorrect");

        user.PasswordHash = hasher.HashPassword(user, request.NewPassword);
        await db.SaveChangesAsync(ct);
    }
}
