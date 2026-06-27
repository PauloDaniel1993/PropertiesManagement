using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Alsappan.Application.Common.Contracts;
using Alsappan.Application.Common.Results;
using Alsappan.Application.Vehicles;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;

namespace Alsappan.Api.Tests;

#pragma warning disable CA1812, CA2000, CA2234

public sealed class VehicleEndpointTests
{
  private static readonly Guid VehicleId = new("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
  private static readonly Guid UserId = new("22222222-2222-2222-2222-222222222222");

  [Fact]
  public async Task VehiclesListRequiresAuthentication()
  {
    using var factory = CreateFactory();
    using var client = factory.CreateClient();

    using var response = await client.GetAsync(new Uri("/v1/vehicles", UriKind.Relative));

    Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
  }

  [Fact]
  public async Task VehicleEndpointsUseAuthenticatedUser()
  {
    using var factory = CreateFactory();
    using var client = factory.CreateClient();
    client.DefaultRequestHeaders.Authorization = new("Bearer", CreateJwt());

    using var listResponse = await client.GetAsync(new Uri(
      "/v1/vehicles?plate=abc-1234&type=car&authorizationStatus=pending&hasParkingAllocation=true",
      UriKind.Relative));
    listResponse.EnsureSuccessStatusCode();
    using var listPayload = JsonDocument.Parse(await listResponse.Content.ReadAsStringAsync());
    Assert.Equal(1, listPayload.RootElement.GetProperty("totalItems").GetInt32());

    foreach (var optionsPath in new[]
      {
        "/v1/vehicles/type-options",
        "/v1/vehicles/authorization-status-options"
      })
    {
      using var optionsResponse = await client.GetAsync(new Uri(optionsPath, UriKind.Relative));
      optionsResponse.EnsureSuccessStatusCode();
    }

    using var detailResponse = await client.GetAsync(new Uri($"/v1/vehicles/{VehicleId}", UriKind.Relative));
    detailResponse.EnsureSuccessStatusCode();

    using var createResponse = await client.PostAsJsonAsync("/v1/vehicles", CreateRequest());
    createResponse.EnsureSuccessStatusCode();

    using var updateResponse = await client.PutAsJsonAsync($"/v1/vehicles/{VehicleId}", UpdateRequest());
    updateResponse.EnsureSuccessStatusCode();

    using var authorizeResponse = await client.PostAsync($"/v1/vehicles/{VehicleId}/authorize", null);
    authorizeResponse.EnsureSuccessStatusCode();

    using var denyResponse = await client.PostAsJsonAsync(
      $"/v1/vehicles/{VehicleId}/deny",
      new VehicleLifecycleRequestDto("Documentacao recusada"));
    denyResponse.EnsureSuccessStatusCode();

    using var restoreResponse = await client.PostAsync($"/v1/vehicles/{VehicleId}/restore", null);
    restoreResponse.EnsureSuccessStatusCode();

    using var archiveResponse = await client.DeleteAsync(new Uri(
      $"/v1/vehicles/{VehicleId}",
      UriKind.Relative));
    Assert.Equal(HttpStatusCode.NoContent, archiveResponse.StatusCode);
  }

  private static WebApplicationFactory<Program> CreateFactory() =>
    new WebApplicationFactory<Program>()
      .WithWebHostBuilder(builder =>
      {
        builder.ConfigureTestServices(services =>
        {
          services.RemoveAll<IVehicleService>();
          services.AddSingleton<IVehicleService, FakeVehicleService>();
        });
      });

  private static VehicleCreateRequestDto CreateRequest() =>
    new(
      Guid.NewGuid(),
      Guid.NewGuid(),
      Guid.NewGuid(),
      "ABC-1234",
      "car",
      "Prata",
      "Honda",
      "Civic",
      2024,
      "pending",
      "A1",
      "Vaga vinculada",
      "Observacoes");

  private static VehicleUpdateRequestDto UpdateRequest() =>
    new(
      Guid.NewGuid(),
      Guid.NewGuid(),
      Guid.NewGuid(),
      "ABC-1234",
      "car",
      "Prata",
      "Honda",
      "Civic",
      2024,
      "pending",
      "A1",
      "Vaga vinculada",
      "Observacoes",
      "concurrency-token");

  private static VehicleDetailDto CreateDetail()
  {
    var resident = new VehicleEntitySummaryDto(Guid.NewGuid(), "Joao da Silva", Route: "/moradores?id=1");
    var property = new VehicleEntitySummaryDto(Guid.NewGuid(), "Casa Calabria", "Rua Calabria, 82", "/imoveis?id=1");
    var contract = new VehicleEntitySummaryDto(Guid.NewGuid(), "Contrato Casa", "Casa Calabria", "/contratos?id=1");

    return new VehicleDetailDto(
      VehicleId,
      resident,
      property,
      contract,
      "ABC-1234",
      "ABC1234",
      new StatusLabelDto("car", "Carro", StatusLabelTones.Info),
      "Prata",
      "Honda",
      "Civic",
      2024,
      new StatusLabelDto("pending", "Pendente", StatusLabelTones.Warning),
      "A1",
      "A1",
      "Vaga vinculada",
      true,
      "Observacoes",
      $"/timeline?entityType=vehicle&entityId={VehicleId}",
      $"/auditoria?entityType=vehicle&entityId={VehicleId}",
      DateTimeOffset.UtcNow,
      DateTimeOffset.UtcNow,
      null,
      "concurrency-token");
  }

  private static VehicleListItemDto CreateListItem()
  {
    var detail = CreateDetail();
    return new VehicleListItemDto(
      detail.Id,
      detail.Resident,
      detail.Property,
      detail.Contract,
      detail.Plate,
      detail.NormalizedPlate,
      detail.Type,
      detail.Color,
      detail.Brand,
      detail.Model,
      detail.Year,
      detail.AuthorizationStatus,
      detail.ParkingSpaceIdentifier,
      detail.NormalizedParkingSpaceIdentifier,
      detail.ParkingAllocationNotes,
      detail.HasParkingAllocation,
      false,
      detail.CreatedAt,
      detail.UpdatedAt,
      detail.ConcurrencyToken);
  }

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
        new Claim(JwtRegisteredClaimNames.Email, "admin@alsappan.local")
      ],
      expires: DateTime.UtcNow.AddMinutes(15),
      signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));

    return new JwtSecurityTokenHandler().WriteToken(token);
  }

  private sealed class FakeVehicleService : IVehicleService
  {
    public Task<ApplicationOperationResult<PagedResultDto<VehicleListItemDto>>> ListAsync(
      VehicleListRequestDto request,
      CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();
      var page = new PagedResultDto<VehicleListItemDto>([CreateListItem()], request.Page, request.PageSize, 1);
      return Task.FromResult(ApplicationOperationResult<PagedResultDto<VehicleListItemDto>>.Success(page));
    }

    public Task<ApplicationOperationResult<VehicleDetailDto>> GetAsync(
      Guid id,
      string? locale = null,
      CancellationToken cancellationToken = default) =>
      Detail(cancellationToken);

    public Task<ApplicationOperationResult<VehicleDetailDto>> CreateAsync(
      VehicleCreateRequestDto request,
      string? locale = null,
      CancellationToken cancellationToken = default) =>
      Detail(cancellationToken);

    public Task<ApplicationOperationResult<VehicleDetailDto>> UpdateAsync(
      Guid id,
      VehicleUpdateRequestDto request,
      string? locale = null,
      CancellationToken cancellationToken = default) =>
      Detail(cancellationToken);

    public Task<ApplicationOperationResult<VehicleDetailDto>> AuthorizeAsync(
      Guid id,
      string? locale = null,
      CancellationToken cancellationToken = default) =>
      Detail(cancellationToken);

    public Task<ApplicationOperationResult<VehicleDetailDto>> DenyAsync(
      Guid id,
      VehicleLifecycleRequestDto request,
      string? locale = null,
      CancellationToken cancellationToken = default) =>
      Detail(cancellationToken);

    public Task<ApplicationOperationResult> ArchiveAsync(Guid id, CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();
      return Task.FromResult(ApplicationOperationResult.Success());
    }

    public Task<ApplicationOperationResult<VehicleDetailDto>> RestoreAsync(
      Guid id,
      string? locale = null,
      CancellationToken cancellationToken = default) =>
      Detail(cancellationToken);

    public Task<IReadOnlyList<StatusLabelDto>> GetTypeOptionsAsync(
      string? locale = null,
      CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();
      return Task.FromResult<IReadOnlyList<StatusLabelDto>>([new StatusLabelDto("car", "Carro")]);
    }

    public Task<IReadOnlyList<StatusLabelDto>> GetAuthorizationStatusOptionsAsync(
      string? locale = null,
      CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();
      return Task.FromResult<IReadOnlyList<StatusLabelDto>>([new StatusLabelDto("pending", "Pendente")]);
    }

    private static Task<ApplicationOperationResult<VehicleDetailDto>> Detail(CancellationToken cancellationToken)
    {
      cancellationToken.ThrowIfCancellationRequested();
      return Task.FromResult(ApplicationOperationResult<VehicleDetailDto>.Success(CreateDetail()));
    }
  }
}

#pragma warning restore CA1812, CA2000, CA2234
