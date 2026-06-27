using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Alsappan.Application.Common.Contracts;
using Alsappan.Application.Common.Results;
using Alsappan.Application.Timeline;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;

namespace Alsappan.Api.Tests;

#pragma warning disable CA1812, CA2000

public sealed class TimelineEndpointTests
{
  private static readonly Guid UserId = new("22222222-2222-2222-2222-222222222222");

  [Fact]
  public async Task TimelineListRequiresAuthentication()
  {
    using var factory = CreateFactory();
    using var client = factory.CreateClient();

    using var response = await client.GetAsync(new Uri("/v1/timeline", UriKind.Relative));

    Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
  }

  [Fact]
  public async Task TimelineListReturnsPagedEvents()
  {
    using var factory = CreateFactory();
    using var client = factory.CreateClient();
    client.DefaultRequestHeaders.Authorization = new("Bearer", CreateJwt());

    using var response = await client.GetAsync(
      new Uri("/v1/timeline?entityType=property&eventType=property.created&pageSize=5", UriKind.Relative));

    response.EnsureSuccessStatusCode();
    using var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
    Assert.Equal(1, payload.RootElement.GetProperty("totalItems").GetInt32());
    Assert.Equal(5, payload.RootElement.GetProperty("pageSize").GetInt32());
    Assert.Equal(
      "property.created",
      payload.RootElement.GetProperty("items")[0].GetProperty("eventType").GetString());
  }

  [Fact]
  public async Task EntityTimelineEndpointUsesEntityRoute()
  {
    using var factory = CreateFactory();
    using var client = factory.CreateClient();
    client.DefaultRequestHeaders.Authorization = new("Bearer", CreateJwt());

    using var response = await client.GetAsync(
      new Uri("/v1/timeline/entities/property/property-a?eventType=property.updated", UriKind.Relative));

    response.EnsureSuccessStatusCode();
    using var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
    Assert.Equal(
      "property.updated",
      payload.RootElement.GetProperty("items")[0].GetProperty("eventType").GetString());
  }

  private static WebApplicationFactory<Program> CreateFactory() =>
    new WebApplicationFactory<Program>()
      .WithWebHostBuilder(builder =>
      {
        builder.ConfigureTestServices(services =>
        {
          services.RemoveAll<ITimelineService>();
          services.AddSingleton<ITimelineService, FakeTimelineService>();
        });
      });

  private static TimelineEntryDto CreateEntry(string eventType) =>
    new(
      Guid.NewGuid(),
      Guid.NewGuid(),
      "properties",
      eventType,
      eventType == "property.updated" ? "Imovel atualizado" : "Imovel criado",
      new DateTimeOffset(2026, 6, 27, 10, 0, 0, TimeSpan.Zero),
      new TimelineActorDto("user", "Usuario", UserId, "Ana Admin"),
      new TimelineEntityReferenceDto(
        "property",
        "Imovel",
        "property-a",
        "Casa A",
        "/imoveis?propertyId=property-a"),
      [],
      new Dictionary<string, string>(StringComparer.Ordinal),
      new TimelineDisplayDto("Imovel criado", "Imovel criado: Casa A"),
      null,
      "/imoveis?propertyId=property-a");

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

  private sealed class FakeTimelineService : ITimelineService
  {
    public Task<ApplicationOperationResult<PagedResultDto<TimelineEntryDto>>> ListAsync(
      TimelineListRequestDto request,
      CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();
      var item = CreateEntry(request.EventType ?? "property.created");

      return Task.FromResult(ApplicationOperationResult<PagedResultDto<TimelineEntryDto>>.Success(
        new PagedResultDto<TimelineEntryDto>([item], request.Page, request.PageSize, 1)));
    }

    public Task<ApplicationOperationResult<PagedResultDto<TimelineEntryDto>>> ListEntityAsync(
      string entityType,
      string entityId,
      TimelineEntityListRequestDto request,
      CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();
      var item = CreateEntry(request.EventType ?? "property.created");

      return Task.FromResult(ApplicationOperationResult<PagedResultDto<TimelineEntryDto>>.Success(
        new PagedResultDto<TimelineEntryDto>([item], request.Page, request.PageSize, 1)));
    }
  }
}
