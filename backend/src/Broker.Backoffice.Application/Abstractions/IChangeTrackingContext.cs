namespace Broker.Backoffice.Application.Abstractions;

public interface IChangeTrackingContext
{
    Guid OperationId { get; }
}
