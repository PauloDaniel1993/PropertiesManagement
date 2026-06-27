using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Alsappan.Application.Common.Contracts;
using Alsappan.Application.Common.Results;
using Alsappan.Application.Dashboard;
using Alsappan.Application.Search;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;

namespace Alsappan.Api.Tests;

#pragma warning disable CA1812, CA2000

public sealed class DashboardSearchEndpointTests
{
  private static readonly Guid PropertyId = new("11111111-2222-3333-4444-555555555555");
  private static readonly Guid UserId = new("22222222-2222-3333-4444-555555555555");

  [Theory]
  [InlineData("/v1/dashboard")]
  [InlineData("/v1/search?query=casa")]
  [InlineData("/v1/search/contract")]
  public async Task DashboardAndSearchEndpointsRequireAuthentication(string path)
  {
    using var factory = CreateFactory();
    using var client = factory.CreateClient();

    using var response = await client.GetAsync(new Uri(path, UriKind.Relative));

    Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
  }

  [Fact]
  public async Task DashboardAndSearchEndpointsReturnContractsForAuthenticatedUsers()
  {
    using var factory = CreateFactory();
    using var client = factory.CreateClient();
    client.DefaultRequestHeaders.Authorization = new("Bearer", CreateJwt());

    using var dashboardResponse = await client.GetAsync(new Uri("/v1/dashboard?locale=pt-BR", UriKind.Relative));
    dashboardResponse.EnsureSuccessStatusCode();
    using var dashboardPayload = JsonDocument.Parse(await dashboardResponse.Content.ReadAsStringAsync());
    Assert.Equal(
      "Ocupacao",
      dashboardPayload.RootElement.GetProperty("metrics")[0].GetProperty("label").GetString());
    Assert.Equal(
      "/imoveis?status=rented",
      dashboardPayload.RootElement.GetProperty("metrics")[0].GetProperty("route").GetString());

    using var searchResponse = await client.GetAsync(new Uri("/v1/search?query=casa&limit=3&locale=pt-BR", UriKind.Relative));
    searchResponse.EnsureSuccessStatusCode();
    using var searchPayload = JsonDocument.Parse(await searchResponse.Content.ReadAsStringAsync());
    Assert.Equal(1, searchPayload.RootElement.GetProperty("totalItems").GetInt32());
    Assert.Equal(
      "Casa Calabria",
      searchPayload.RootElement.GetProperty("groups")[0].GetProperty("results")[0].GetProperty("label").GetString());

    using var contractResponse = await client.GetAsync(new Uri("/v1/search/contract?locale=en-US", UriKind.Relative));
    contractResponse.EnsureSuccessStatusCode();
    using var contractPayload = JsonDocument.Parse(await contractResponse.Content.ReadAsStringAsync());
    Assert.Equal(
      "Property",
      contractPayload.RootElement.GetProperty("entityTypes")[0].GetProperty("entityTypeLabel").GetString());
  }

  private static WebApplicationFactory<Program> CreateFactory() =>
    new WebApplicationFactory<Program>()
      .WithWebHostBuilder(builder =>
      {
        builder.ConfigureTestServices(services =>
        {
          services.RemoveAll<IDashboardService>();
          services.RemoveAll<IGlobalSearchService>();
          services.AddSingleton<IDashboardService, FakeDashboardService>();
          services.AddSingleton<IGlobalSearchService, FakeGlobalSearchService>();
        });
      });

  private static string CreateJwt()
  {
    var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
      "replace-this-dev-only-signing-key-with-at-least-32-characters"));
    var token = new JwtSecurityToken(
      issuer: "Alsappan",
      audience: "Alsappan.Web",
      claims:
      [
        new Claim(JwtRegisteredClaimNames.Sub, UserId.ToString()),
        new Claim(JwtRegisteredClaimNames.Email, "admin@alsappan.local"),
        new Claim("alsappan:active_organization_id", Guid.NewGuid().ToString()),
        new Claim("alsappan:permission", "*")
      ],
      expires: DateTime.UtcNow.AddMinutes(15),
      signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));

    return new JwtSecurityTokenHandler().WriteToken(token);
  }

  private sealed class FakeDashboardService : IDashboardService
  {
    public Task<ApplicationOperationResult<DashboardOverviewDto>> GetOverviewAsync(
      DashboardOverviewRequestDto request,
      CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();
      var overview = new DashboardOverviewDto(
        new DateTimeOffset(2026, 6, 27, 12, 0, 0, TimeSpan.Zero),
        [
          new DashboardMetricDto(
            "occupancy",
            request.Locale == "en-US" ? "Occupancy" : "Ocupacao",
            "Imoveis alugados sobre o total ativo.",
            75m,
            "75%",
            "%",
            "success",
            "/imoveis?status=rented",
            true,
            null,
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
              ["totalProperties"] = "4",
              ["rentedProperties"] = "3"
            })
        ],
        new DashboardRecentActivitySectionDto(
          "Atividade recente",
          "Ultimos eventos operacionais.",
          true,
          "/timeline",
          null,
          []),
        null);

      return Task.FromResult(ApplicationOperationResult<DashboardOverviewDto>.Success(overview));
    }
  }

  private sealed class FakeGlobalSearchService : IGlobalSearchService
  {
    public Task<ApplicationOperationResult<GlobalSearchResponseDto>> SearchAsync(
      GlobalSearchRequestDto request,
      CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();
      var response = new GlobalSearchResponseDto(
        request.Query,
        1,
        [
          new GlobalSearchGroupDto(
            "property",
            request.Locale == "en-US" ? "Property" : "Imovel",
            $"/imoveis?search={Uri.EscapeDataString(request.Query)}",
            [
              new GlobalSearchResultDto(
                "property",
                request.Locale == "en-US" ? "Property" : "Imovel",
                PropertyId,
                "Casa Calabria",
                "Rua Calabria, 82",
                request.Locale == "en-US" ? "Name" : "Nome",
                $"/imoveis?propertyId={PropertyId:D}")
            ])
        ]);

      return Task.FromResult(ApplicationOperationResult<GlobalSearchResponseDto>.Success(response));
    }

    public Task<GlobalSearchResultContractDto> GetContractAsync(
      string? locale = null,
      CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();
      return Task.FromResult(
        new GlobalSearchResultContractDto(
          [
            new GlobalSearchEntityOptionDto(
              "property",
              locale == "en-US" ? "Property" : "Imovel",
              "properties.read")
          ],
          [new SelectOptionDto("name", locale == "en-US" ? "Name" : "Nome")]));
    }
  }
}
