namespace Broker.Backoffice.Application.Roles;

public sealed record RoleDto(
    Guid Id, string Name, string? Description, bool IsSystem,
    IReadOnlyList<string> Permissions, DateTime CreatedAt, byte[] RowVersion);
