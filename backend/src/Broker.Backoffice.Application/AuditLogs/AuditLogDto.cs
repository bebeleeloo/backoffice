namespace Broker.Backoffice.Application.AuditLogs;

public sealed record AuditLogDto(
    Guid Id, Guid? UserId, string? UserName, string Action,
    string? EntityType, string? EntityId, string? BeforeJson, string? AfterJson,
    string? CorrelationId, string? IpAddress, string Path, string Method,
    int StatusCode, bool IsSuccess, DateTime CreatedAt);
