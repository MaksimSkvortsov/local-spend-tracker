namespace Spendnest.Infrastructure.Tests;

using FluentAssertions;
using Spendnest.Core;
using Spendnest.Infrastructure;

public class InfrastructureProjectTests
{
    [Fact]
    public void InfrastructureAssembly_ShouldReferenceCoreAssembly()
    {
        typeof(InfrastructureAssemblyMarker).Assembly.FullName.Should().NotBe(typeof(CoreAssemblyMarker).Assembly.FullName);
        typeof(CoreAssemblyMarker).Namespace.Should().Be("Spendnest.Core");
    }
}
