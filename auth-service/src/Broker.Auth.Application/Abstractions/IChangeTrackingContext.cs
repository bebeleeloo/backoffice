namespace Broker.Auth.Application.Abstractions;

public interface IChangeTrackingContext
{
    Guid OperationId { get; }
}
