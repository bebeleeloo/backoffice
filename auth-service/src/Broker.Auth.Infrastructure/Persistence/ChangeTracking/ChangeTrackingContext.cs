using Broker.Auth.Application.Abstractions;

namespace Broker.Auth.Infrastructure.Persistence.ChangeTracking;

public sealed class ChangeTrackingContext : IChangeTrackingContext
{
    private Guid? _operationId;
    public Guid OperationId => _operationId ??= Guid.NewGuid();
}
