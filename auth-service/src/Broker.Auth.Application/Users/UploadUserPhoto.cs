using Broker.Auth.Application.Abstractions;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Broker.Auth.Application.Users;

public sealed record UploadUserPhotoCommand(Guid UserId, byte[] Photo, string ContentType) : IRequest;

public sealed class UploadUserPhotoCommandValidator : AbstractValidator<UploadUserPhotoCommand>
{
    private static readonly HashSet<string> AllowedContentTypes =
        ["image/jpeg", "image/png", "image/gif", "image/webp"];

    // Magic byte signatures for image formats
    private static readonly byte[] JpegMagic = [0xFF, 0xD8, 0xFF];
    private static readonly byte[] PngMagic = [0x89, 0x50, 0x4E, 0x47];
    private static readonly byte[] GifMagic87 = "GIF87a"u8.ToArray();
    private static readonly byte[] GifMagic89 = "GIF89a"u8.ToArray();
    private static readonly byte[] WebpRiff = "RIFF"u8.ToArray();
    private static readonly byte[] WebpWebp = "WEBP"u8.ToArray();

    public UploadUserPhotoCommandValidator()
    {
        RuleFor(x => x.Photo).NotEmpty()
            .Must(p => p.Length >= 100).WithMessage("Photo must be at least 100 bytes")
            .Must(p => p.Length <= 2 * 1024 * 1024).WithMessage("Photo must be 2 MB or less");
        RuleFor(x => x.ContentType).NotEmpty()
            .Must(ct => AllowedContentTypes.Contains(ct)).WithMessage("Unsupported image format");
        RuleFor(x => x).Must(x => HasValidMagicBytes(x.Photo, x.ContentType))
            .WithMessage("File content does not match declared content type");
    }

    private static bool HasValidMagicBytes(byte[]? photo, string? contentType)
    {
        if (photo == null || photo.Length < 4 || string.IsNullOrEmpty(contentType)) return false;

        return contentType switch
        {
            "image/jpeg" => photo.AsSpan().StartsWith(JpegMagic),
            "image/png" => photo.AsSpan().StartsWith(PngMagic),
            "image/gif" => photo.AsSpan().StartsWith(GifMagic87) || photo.AsSpan().StartsWith(GifMagic89),
            "image/webp" => photo.Length >= 12 && photo.AsSpan(0, 4).SequenceEqual(WebpRiff) && photo.AsSpan(8, 4).SequenceEqual(WebpWebp),
            _ => false,
        };
    }
}

internal sealed class UploadUserPhotoCommandHandler(
    IAuthDbContext db,
    IAuditContext audit,
    IDateTimeProvider clock,
    ICurrentUser currentUser)
    : IRequestHandler<UploadUserPhotoCommand>
{
    public async Task Handle(UploadUserPhotoCommand request, CancellationToken ct)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == request.UserId, ct)
            ?? throw new KeyNotFoundException($"User {request.UserId} not found");

        audit.EntityType = "User";
        audit.EntityId = user.Id.ToString();

        user.Photo = request.Photo;
        user.PhotoContentType = request.ContentType;
        user.UpdatedAt = clock.UtcNow;
        user.UpdatedBy = currentUser.UserName;
        await db.SaveChangesAsync(ct);
    }
}
