namespace Broker.Backoffice.Application.Abstractions;

public interface IAuditContext
{
    string? EntityType { get; set; }
    string? EntityId { get; set; }
    string? BeforeJson { get; set; }
    string? AfterJson { get; set; }
}

public sealed class AuditContext : IAuditContext
{
    public string? EntityType { get; set; }
    public string? EntityId { get; set; }
    public string? BeforeJson { get; set; }
    public string? AfterJson { get; set; }
}
