namespace Broker.Gateway.Api.Services;

public interface ICorrelationIdAccessor
{
    string CorrelationId { get; set; }
}

public sealed class CorrelationIdAccessor : ICorrelationIdAccessor
{
    public string CorrelationId { get; set; } = string.Empty;
}
