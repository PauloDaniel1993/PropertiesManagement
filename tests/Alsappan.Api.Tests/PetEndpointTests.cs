using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Alsappan.Application.Common.Contracts;
using Alsappan.Application.Common.Results;
using Alsappan.Application.Pets;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;

namespace Alsappan.Api.Tests;

#pragma warning disable CA1812, CA2000, CA2234

public sealed class PetEndpointTests
{
  private static readonly Guid PetId = new("cccccccc-cccc-cccc-cccc-cccccccccccc");
  private static readonly Guid UserId = new("22222222-2222-2222-2222-222222222222");

  [Fact]
  public async Task PetsListRequiresAuthentication()
  {
    using var factory = CreateFactory();
    using var client = factory.CreateClient();

    using var response = await client.GetAsync(new Uri("/v1/pets", UriKind.Relative));

    Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
  }

  [Fact]
  public async Task PetEndpointsUseAuthenticatedUser()
  {
    using var factory = CreateFactory();
    using var client = factory.CreateClient();
    client.DefaultRequestHeaders.Authorization = new("Bearer", CreateJwt());

    using var listResponse = await client.GetAsync(new Uri(
      "/v1/pets?species=cat&authorizationStatus=pending&activeContractOnly=true",
      UriKind.Relative));
    listResponse.EnsureSuccessStatusCode();
    using var listPayload = JsonDocument.Parse(await listResponse.Content.ReadAsStringAsync());
    Assert.Equal(1, listPayload.RootElement.GetProperty("totalItems").GetInt32());

    using var optionsResponse = await client.GetAsync(new Uri("/v1/pets/options", UriKind.Relative));
    optionsResponse.EnsureSuccessStatusCode();

    using var detailResponse = await client.GetAsync(new Uri($"/v1/pets/{PetId}", UriKind.Relative));
    detailResponse.EnsureSuccessStatusCode();

    using var createResponse = await client.PostAsJsonAsync("/v1/pets", CreateRequest());
    createResponse.EnsureSuccessStatusCode();

    using var updateResponse = await client.PutAsJsonAsync($"/v1/pets/{PetId}", UpdateRequest());
    updateResponse.EnsureSuccessStatusCode();

    using var authorizeResponse = await client.PostAsJsonAsync(
      $"/v1/pets/{PetId}/authorize",
      new PetLifecycleRequestDto("Autorizado"));
    authorizeResponse.EnsureSuccessStatusCode();

    using var denyResponse = await client.PostAsJsonAsync(
      $"/v1/pets/{PetId}/deny",
      new PetLifecycleRequestDto("Pendente de vacina"));
    denyResponse.EnsureSuccessStatusCode();

    using var restoreResponse = await client.PostAsync($"/v1/pets/{PetId}/restore", null);
    restoreResponse.EnsureSuccessStatusCode();

    using var archiveResponse = await client.DeleteAsync(new Uri($"/v1/pets/{PetId}", UriKind.Relative));
    Assert.Equal(HttpStatusCode.NoContent, archiveResponse.StatusCode);
  }

  private static WebApplicationFactory<Program> CreateFactory() =>
    new WebApplicationFactory<Program>()
      .WithWebHostBuilder(builder =>
      {
        builder.ConfigureTestServices(services =>
        {
          services.RemoveAll<IPetService>();
          services.AddSingleton<IPetService, FakePetService>();
        });
      });

  private static PetCreateRequestDto CreateRequest() =>
    new(
      Guid.NewGuid(),
      Guid.NewGuid(),
      Guid.NewGuid(),
      "Luna",
      "cat",
      "SRD",
      "pending",
      "Aguardando formulario",
      Guid.NewGuid(),
      Guid.NewGuid(),
      "Docil");

  private static PetUpdateRequestDto UpdateRequest() =>
    new(
      Guid.NewGuid(),
      Guid.NewGuid(),
      Guid.NewGuid(),
      "Luna",
      "cat",
      "SRD",
      "pending",
      "Aguardando formulario",
      Guid.NewGuid(),
      Guid.NewGuid(),
      "Docil",
      "concurrency-token");

  private static PetDetailDto CreateDetail()
  {
    var resident = new PetEntitySummaryDto(Guid.NewGuid(), "Joao da Silva", null, "/moradores?id=1");
    var property = new PetEntitySummaryDto(Guid.NewGuid(), "Casa Calabria", "Rua Calabria, 82", "/imoveis?id=1");
    var contract = new PetEntitySummaryDto(Guid.NewGuid(), "Contrato Casa", "Casa Calabria", "/contratos?id=1");
    var status = new StatusLabelDto("pending", "Pendente", StatusLabelTones.Warning);

    return new PetDetailDto(
      PetId,
      "Luna",
      new StatusLabelDto("cat", "Gato", StatusLabelTones.Info),
      "SRD",
      status,
      "Aguardando formulario",
      "Docil",
      resident,
      property,
      contract,
      [new PetDocumentDto(Guid.NewGuid(), "vaccination-record", "Carteira de vacinacao", "Carteira", "/documentos?entityType=pet&entityId=1")],
      [new PetDocumentDto(Guid.NewGuid(), "authorization-form", "Formulario de autorizacao", "Formulario", "/documentos?entityType=pet&entityId=1")],
      [new PetAuthorizationHistoryItemDto(status.Code, status.Label, "Aguardando formulario", DateTimeOffset.UtcNow)],
      $"/timeline?entityType=pet&entityId={PetId}",
      $"/auditoria?entityType=pet&entityId={PetId}",
      DateTimeOffset.UtcNow,
      DateTimeOffset.UtcNow,
      null,
      "concurrency-token");
  }

  private static PetListItemDto CreateListItem()
  {
    var detail = CreateDetail();
    return new PetListItemDto(
      detail.Id,
      detail.Name,
      detail.Species,
      detail.Breed,
      detail.AuthorizationStatus,
      detail.AuthorizationNotes,
      detail.Resident,
      detail.Property,
      detail.Contract,
      detail.VaccinationRecordDocuments,
      detail.AuthorizationFormDocuments,
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
        new Claim(JwtRegisteredClaimNames.Email, "admin@alsappan.local"),
        new Claim("alsappan:active_organization_id", Guid.NewGuid().ToString()),
        new Claim("alsappan:permission", "*")
      ],
      expires: DateTime.UtcNow.AddMinutes(15),
      signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));

    return new JwtSecurityTokenHandler().WriteToken(token);
  }

  private sealed class FakePetService : IPetService
  {
    public Task<ApplicationOperationResult<PagedResultDto<PetListItemDto>>> ListAsync(
      PetListRequestDto request,
      CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();
      var page = new PagedResultDto<PetListItemDto>([CreateListItem()], request.Page, request.PageSize, 1);
      return Task.FromResult(ApplicationOperationResult<PagedResultDto<PetListItemDto>>.Success(page));
    }

    public Task<ApplicationOperationResult<PetDetailDto>> GetAsync(
      Guid id,
      string? locale = null,
      CancellationToken cancellationToken = default) =>
      Detail(cancellationToken);

    public Task<ApplicationOperationResult<PetDetailDto>> CreateAsync(
      PetCreateRequestDto request,
      string? locale = null,
      CancellationToken cancellationToken = default) =>
      Detail(cancellationToken);

    public Task<ApplicationOperationResult<PetDetailDto>> UpdateAsync(
      Guid id,
      PetUpdateRequestDto request,
      string? locale = null,
      CancellationToken cancellationToken = default) =>
      Detail(cancellationToken);

    public Task<ApplicationOperationResult<PetDetailDto>> AuthorizeAsync(
      Guid id,
      PetLifecycleRequestDto request,
      string? locale = null,
      CancellationToken cancellationToken = default) =>
      Detail(cancellationToken);

    public Task<ApplicationOperationResult<PetDetailDto>> DenyAsync(
      Guid id,
      PetLifecycleRequestDto request,
      string? locale = null,
      CancellationToken cancellationToken = default) =>
      Detail(cancellationToken);

    public Task<ApplicationOperationResult> ArchiveAsync(Guid id, CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();
      return Task.FromResult(ApplicationOperationResult.Success());
    }

    public Task<ApplicationOperationResult<PetDetailDto>> RestoreAsync(
      Guid id,
      string? locale = null,
      CancellationToken cancellationToken = default) =>
      Detail(cancellationToken);

    public Task<PetOptionsDto> GetOptionsAsync(string? locale = null, CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();
      return Task.FromResult(new PetOptionsDto(
        [new StatusLabelDto("cat", "Gato")],
        [new StatusLabelDto("pending", "Pendente")],
        [new StatusLabelDto("vaccination-record", "Carteira de vacinacao")]));
    }

    private static Task<ApplicationOperationResult<PetDetailDto>> Detail(CancellationToken cancellationToken)
    {
      cancellationToken.ThrowIfCancellationRequested();
      return Task.FromResult(ApplicationOperationResult<PetDetailDto>.Success(CreateDetail()));
    }
  }
}

#pragma warning restore CA1812, CA2000, CA2234
