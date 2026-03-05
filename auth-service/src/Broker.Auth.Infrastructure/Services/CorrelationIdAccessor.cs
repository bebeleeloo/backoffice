using Broker.Auth.Application.Abstractions;

namespace Broker.Auth.Infrastructure.Services;

public sealed class CorrelationIdAccessor : ICorrelationIdAccessor
{
    public string CorrelationId { get; set; } = Guid.NewGuid().ToString("N");
}
