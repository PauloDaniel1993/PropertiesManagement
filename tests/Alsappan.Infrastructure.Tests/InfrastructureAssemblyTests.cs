using Alsappan.Infrastructure;

namespace Alsappan.Infrastructure.Tests;

public sealed class InfrastructureAssemblyTests
{
  [Fact]
  public void InfrastructureAssemblyMarkerIsAvailable()
  {
    Assert.Equal("Alsappan.Infrastructure", typeof(AssemblyReference).Assembly.GetName().Name);
  }
}
