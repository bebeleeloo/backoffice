namespace Broker.Auth.Application.Roles;

public sealed record RoleDto(
    Guid Id, string Name, string? Description, bool IsSystem,
    IReadOnlyList<string> Permissions, DateTime CreatedAt, uint RowVersion);
