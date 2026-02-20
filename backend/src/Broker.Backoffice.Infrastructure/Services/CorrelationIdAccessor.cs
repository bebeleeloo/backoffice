using Broker.Backoffice.Application.Abstractions;

namespace Broker.Backoffice.Infrastructure.Services;

public sealed class CorrelationIdAccessor : ICorrelationIdAccessor
{
    public string CorrelationId { get; set; } = Guid.NewGuid().ToString("N");
}
