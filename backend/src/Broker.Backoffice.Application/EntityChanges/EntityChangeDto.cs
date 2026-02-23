namespace Broker.Backoffice.Application.EntityChanges;

public sealed record FieldChangeDto(
    string FieldName,
    string ChangeType,
    string? OldValue,
    string? NewValue);

public sealed record EntityChangeGroupDto(
    string? RelatedEntityType,
    string? RelatedEntityId,
    string? RelatedEntityDisplayName,
    string ChangeType,
    IReadOnlyList<FieldChangeDto> Fields);

public sealed record OperationDto(
    Guid OperationId,
    DateTime Timestamp,
    string? UserId,
    string? UserName,
    string? EntityDisplayName,
    string ChangeType,
    IReadOnlyList<EntityChangeGroupDto> Changes);
