using Alsappan.Application;

namespace Alsappan.Application.Tests;

public sealed class ApplicationAssemblyTests
{
  [Fact]
  public void ApplicationAssemblyMarkerIsAvailable()
  {
    Assert.Equal("Alsappan.Application", typeof(AssemblyReference).Assembly.GetName().Name);
  }
}
