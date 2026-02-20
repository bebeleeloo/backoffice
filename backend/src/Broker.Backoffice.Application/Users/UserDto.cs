namespace Broker.Backoffice.Application.Users;

public sealed record UserDto(
    Guid Id,
    string Username,
    string Email,
    string? FullName,
    bool IsActive,
    IReadOnlyList<string> Roles,
    DateTime CreatedAt,
    byte[] RowVersion);
