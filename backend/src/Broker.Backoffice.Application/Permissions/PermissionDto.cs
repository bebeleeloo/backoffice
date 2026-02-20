namespace Broker.Backoffice.Application.Permissions;

public sealed record PermissionDto(Guid Id, string Code, string Name, string? Description, string Group);
