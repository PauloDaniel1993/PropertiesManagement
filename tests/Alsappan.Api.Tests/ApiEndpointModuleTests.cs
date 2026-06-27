using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Alsappan.Api.Tests;

public sealed class ApiEndpointModuleTests : IClassFixture<WebApplicationFactory<Program>>
{
  private readonly WebApplicationFactory<Program> factory;

  public ApiEndpointModuleTests(WebApplicationFactory<Program> factory)
  {
    this.factory = factory;
  }

  [Theory]
  [InlineData("/v1/administrators")]
  [InlineData("/v1/contracts/status-options")]
  [InlineData("/v1/documents/category-options")]
  [InlineData("/v1/pets/options")]
  [InlineData("/v1/properties/status-options")]
  [InlineData("/v1/residents/status-options")]
  public async Task FeatureEndpointModulesAreDiscovered(string path)
  {
    using var client = CreateClient();

    using var response = await client.GetAsync(new Uri(path, UriKind.Relative));

    Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
  }

  private HttpClient CreateClient() =>
    factory.CreateClient(new WebApplicationFactoryClientOptions
    {
      BaseAddress = new Uri("https://localhost")
    });
}
