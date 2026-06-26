using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Alsappan.Api.Tests;

public sealed class ApiPlatformSmokeTests : IClassFixture<WebApplicationFactory<Program>>
{
  private readonly WebApplicationFactory<Program> factory;

  public ApiPlatformSmokeTests(WebApplicationFactory<Program> factory)
  {
    this.factory = factory;
  }

  [Fact]
  public async Task HealthEndpointReturnsHealthyStatus()
  {
    using var client = CreateClient();

    using var response = await client.GetAsync(new Uri("/health", UriKind.Relative));

    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
  }

  [Fact]
  public async Task SystemInfoReturnsDefaultAndSupportedCultures()
  {
    using var client = CreateClient();

    using var response = await client.GetAsync(new Uri("/v1/system/info", UriKind.Relative));

    response.EnsureSuccessStatusCode();
    using var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
    var root = payload.RootElement;

    Assert.Equal("pt-BR", root.GetProperty("defaultCulture").GetString());
    Assert.Contains(
      root.GetProperty("supportedCultures").EnumerateArray(),
      culture => culture.GetString() == "en-US");
  }

  [Fact]
  public async Task ProtectedEndpointReturnsLocalizedProblemDetailsWhenUnauthenticated()
  {
    using var client = CreateClient();
    using var request = new HttpRequestMessage(HttpMethod.Get, "/v1/system/protected");
    request.Headers.AcceptLanguage.ParseAdd("en-US");

    using var response = await client.SendAsync(request);

    Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

    using var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
    Assert.Equal("Authentication required", payload.RootElement.GetProperty("title").GetString());
  }

  private HttpClient CreateClient() =>
    factory.CreateClient(new WebApplicationFactoryClientOptions
    {
      BaseAddress = new Uri("https://localhost")
    });
}
