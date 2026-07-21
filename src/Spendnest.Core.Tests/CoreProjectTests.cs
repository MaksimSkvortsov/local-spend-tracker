namespace Spendnest.Core.Tests;

using FluentAssertions;
using Spendnest.Core;

public class CoreProjectTests
{
    [Fact]
    public void CoreAssemblyMarker_ShouldBeAvailable()
    {
        typeof(CoreAssemblyMarker).Namespace.Should().Be("Spendnest.Core");
    }
}
