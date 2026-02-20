using Broker.Backoffice.Infrastructure.Services;
using FluentAssertions;

namespace Broker.Backoffice.Tests.Unit;

public class CorrelationIdAccessorTests
{
    [Fact]
    public void CorrelationId_ShouldHaveDefaultValue()
    {
        var accessor = new CorrelationIdAccessor();
        accessor.CorrelationId.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void CorrelationId_ShouldBeSettable()
    {
        var accessor = new CorrelationIdAccessor();
        accessor.CorrelationId = "test-123";
        accessor.CorrelationId.Should().Be("test-123");
    }
}
