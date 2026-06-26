using Alsappan.Domain;

namespace Alsappan.Domain.Tests;

public sealed class DomainAssemblyTests
{
  [Fact]
  public void DomainAssemblyMarkerIsAvailable()
  {
    Assert.Equal("Alsappan.Domain", typeof(AssemblyReference).Assembly.GetName().Name);
  }
}
