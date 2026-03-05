namespace Broker.Auth.Application.Users;

public sealed record UserDto(
    Guid Id,
    string Username,
    string Email,
    string? FullName,
    bool IsActive,
    bool HasPhoto,
    IReadOnlyList<string> Roles,
    DateTime CreatedAt,
    byte[] RowVersion);
