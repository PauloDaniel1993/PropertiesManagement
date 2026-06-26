namespace Alsappan.Api.Tests;

public sealed class ApiAssemblyTests
{
  [Fact]
  public void ApiEntryPointIsAvailable()
  {
    Assert.Equal("Alsappan.Api", typeof(Program).Assembly.GetName().Name);
  }
}
