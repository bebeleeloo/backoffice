using Broker.Backoffice.Application.Abstractions;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Broker.Backoffice.Application.Users;

public sealed record UploadUserPhotoCommand(Guid UserId, byte[] Photo, string ContentType) : IRequest;

public sealed class UploadUserPhotoCommandValidator : AbstractValidator<UploadUserPhotoCommand>
{
    private static readonly HashSet<string> AllowedContentTypes =
        ["image/jpeg", "image/png", "image/gif", "image/webp"];

    public UploadUserPhotoCommandValidator()
    {
        RuleFor(x => x.Photo).NotEmpty()
            .Must(p => p.Length <= 2 * 1024 * 1024).WithMessage("Photo must be 2 MB or less");
        RuleFor(x => x.ContentType).NotEmpty()
            .Must(ct => AllowedContentTypes.Contains(ct)).WithMessage("Unsupported image format");
    }
}

internal sealed class UploadUserPhotoCommandHandler(IAppDbContext db)
    : IRequestHandler<UploadUserPhotoCommand>
{
    public async Task Handle(UploadUserPhotoCommand request, CancellationToken ct)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == request.UserId, ct)
            ?? throw new KeyNotFoundException($"User {request.UserId} not found");

        user.Photo = request.Photo;
        user.PhotoContentType = request.ContentType;
        await db.SaveChangesAsync(ct);
    }
}
