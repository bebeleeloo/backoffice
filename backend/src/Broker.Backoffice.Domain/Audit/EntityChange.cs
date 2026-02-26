using Broker.Backoffice.Domain.Common;

namespace Broker.Backoffice.Domain.Audit;

public sealed class EntityChange : Entity
{
    public Guid OperationId { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public string? EntityDisplayName { get; set; }
    public string? RelatedEntityType { get; set; }
    public string? RelatedEntityId { get; set; }
    public string? RelatedEntityDisplayName { get; set; }
    public string ChangeType { get; set; } = string.Empty; // "Created" | "Modified" | "Deleted"
    public string FieldName { get; set; } = string.Empty;
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public string? UserId { get; set; }
    public string? UserName { get; set; }
    public DateTime Timestamp { get; set; }
}
