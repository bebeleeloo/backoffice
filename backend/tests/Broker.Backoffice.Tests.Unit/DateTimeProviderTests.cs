using Broker.Backoffice.Infrastructure.Services;
using FluentAssertions;

namespace Broker.Backoffice.Tests.Unit;

public class DateTimeProviderTests
{
    [Fact]
    public void UtcNow_ShouldReturnCurrentUtcTime()
    {
        // Arrange
        var provider = new DateTimeProvider();
        var before = DateTime.UtcNow;

        // Act
        var result = provider.UtcNow;

        // Assert
        var after = DateTime.UtcNow;
        result.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
        result.Kind.Should().Be(DateTimeKind.Utc);
    }
}
