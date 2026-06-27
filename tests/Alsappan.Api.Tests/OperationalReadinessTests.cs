using System.Net;
using System.Security.Claims;
using System.Text.Json;
using Alsappan.Api.Configuration;
using Alsappan.Application.Common.Auth;
using Alsappan.Application.Common.Tenancy;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace Alsappan.Api.Tests;

public sealed class OperationalReadinessTests : IClassFixture<WebApplicationFactory<Program>>
{
  private readonly WebApplicationFactory<Program> factory;

  public OperationalReadinessTests(WebApplicationFactory<Program> factory)
  {
    this.factory = factory;
  }

  [Fact]
  public async Task ReadinessEndpointReportsOperationalChecks()
  {
    var storagePath = Path.Combine(
      Path.GetTempPath(),
      "alsappan-health",
      Guid.NewGuid().ToString("N"));

    using var configuredFactory = factory.WithWebHostBuilder(builder =>
    {
      builder.ConfigureAppConfiguration((_, configuration) =>
      {
        configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
          ["Alsappan:Database:ConnectionString"] =
            "Host=localhost;Port=1;Database=alsappan;Username=alsappan;Password=alsappan;Timeout=1;Command Timeout=1",
          ["Alsappan:Storage:LocalPath"] = storagePath
        });
      });
    });
    using var client = configuredFactory.CreateClient(new WebApplicationFactoryClientOptions
    {
      BaseAddress = new Uri("https://localhost")
    });

    using var response = await client.GetAsync(new Uri("/health/ready", UriKind.Relative));

    Assert.True(response.StatusCode is HttpStatusCode.OK or HttpStatusCode.ServiceUnavailable);

    using var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
    var checks = payload.RootElement.GetProperty("checks");

    Assert.True(checks.TryGetProperty("api", out _));
    Assert.True(checks.TryGetProperty("database", out _));
    Assert.True(checks.TryGetProperty("storage", out var storage));
    Assert.True(checks.TryGetProperty("background-worker", out var backgroundWorker));
    Assert.True(checks.TryGetProperty("localization", out var localization));
    Assert.Equal("Healthy", storage.GetProperty("status").GetString());
    Assert.Equal("Healthy", backgroundWorker.GetProperty("status").GetString());
    Assert.Equal("Healthy", localization.GetProperty("status").GetString());
  }

  [Fact]
  public async Task RequestIdHeaderIsEchoedForOperationalCorrelation()
  {
    using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
    {
      BaseAddress = new Uri("https://localhost")
    });
    using var request = new HttpRequestMessage(HttpMethod.Get, "/v1/system/info");
    request.Headers.Add(OperationalLoggingMiddleware.RequestIdHeaderName, "request-id-from-client");

    using var response = await client.SendAsync(request);

    response.EnsureSuccessStatusCode();
    Assert.True(response.Headers.TryGetValues(
      OperationalLoggingMiddleware.RequestIdHeaderName,
      out var values));
    Assert.Equal("request-id-from-client", Assert.Single(values));
  }

  [Fact]
  public void OperationalLogScopeIncludesTenantUserRequestActionAndEntityFields()
  {
    var httpContext = new DefaultHttpContext();
    httpContext.TraceIdentifier = "request-123";
    httpContext.Request.Method = HttpMethods.Get;
    httpContext.Request.Path = "/v1/properties/9d769695-f390-4dd1-84cc-d092fce29d04";
    httpContext.Request.Headers[TenancyHeaderNames.ActiveOrganizationId] = "organization-123";
    httpContext.Request.RouteValues["id"] = "9d769695-f390-4dd1-84cc-d092fce29d04";
    httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(
      [new Claim(AuthClaimTypes.UserId, "user-123")],
      authenticationType: "Test"));

    var scope = OperationalLogScope.Create(httpContext);

    Assert.Equal("organization-123", scope["OrganizationId"]);
    Assert.Equal("user-123", scope["UserId"]);
    Assert.Equal("request-123", scope["RequestId"]);
    Assert.Equal("GET /v1/properties/9d769695-f390-4dd1-84cc-d092fce29d04", scope["Action"]);
    Assert.Equal("properties", scope["EntityType"]);
    Assert.Equal("9d769695-f390-4dd1-84cc-d092fce29d04", scope["EntityId"]);
  }
}
