using Broker.Backoffice.Application.Abstractions;

namespace Broker.Backoffice.Infrastructure.Persistence.ChangeTracking;

public sealed class ChangeTrackingContext : IChangeTrackingContext
{
    private Guid? _operationId;

    public Guid OperationId => _operationId ??= Guid.NewGuid();
}
