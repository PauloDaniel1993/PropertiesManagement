using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Alsappan.Application.Common.Contracts;
using Alsappan.Application.Common.Results;
using Alsappan.Application.Inspections;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;

namespace Alsappan.Api.Tests;

#pragma warning disable CA1812, CA2000, CA2234

public sealed class InspectionEndpointTests
{
  private static readonly Guid InspectionId = new("cccccccc-cccc-cccc-cccc-cccccccccccc");
  private static readonly Guid ChecklistItemId = new("dddddddd-dddd-dddd-dddd-dddddddddddd");
  private static readonly Guid UserId = new("22222222-2222-2222-2222-222222222222");

  [Fact]
  public async Task InspectionsListRequiresAuthentication()
  {
    using var factory = CreateFactory();
    using var client = factory.CreateClient();

    using var response = await client.GetAsync(new Uri("/v1/inspections", UriKind.Relative));

    Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
  }

  [Fact]
  public async Task InspectionEndpointsUseAuthenticatedUser()
  {
    using var factory = CreateFactory();
    using var client = factory.CreateClient();
    client.DefaultRequestHeaders.Authorization = new("Bearer", CreateJwt());

    using var listResponse = await client.GetAsync(new Uri(
      "/v1/inspections?type=move-in&status=scheduled&pendingOnly=true",
      UriKind.Relative));
    listResponse.EnsureSuccessStatusCode();
    using var listPayload = JsonDocument.Parse(await listResponse.Content.ReadAsStringAsync());
    Assert.Equal(1, listPayload.RootElement.GetProperty("totalItems").GetInt32());

    foreach (var optionsPath in new[]
      {
        "/v1/inspections/type-options",
        "/v1/inspections/status-options",
        "/v1/inspections/condition-rating-options",
        "/v1/inspections/document-kind-options"
      })
    {
      using var optionsResponse = await client.GetAsync(new Uri(optionsPath, UriKind.Relative));
      optionsResponse.EnsureSuccessStatusCode();
    }

    using var detailResponse = await client.GetAsync(new Uri($"/v1/inspections/{InspectionId}", UriKind.Relative));
    detailResponse.EnsureSuccessStatusCode();

    using var scheduleResponse = await client.PostAsJsonAsync("/v1/inspections", ScheduleRequest());
    scheduleResponse.EnsureSuccessStatusCode();

    using var updateResponse = await client.PutAsJsonAsync($"/v1/inspections/{InspectionId}", UpdateRequest());
    updateResponse.EnsureSuccessStatusCode();

    using var startResponse = await client.PostAsync($"/v1/inspections/{InspectionId}/start", null);
    startResponse.EnsureSuccessStatusCode();

    using var completeResponse = await client.PostAsJsonAsync(
      $"/v1/inspections/{InspectionId}/complete",
      new InspectionLifecycleRequestDto("Checklist completo"));
    completeResponse.EnsureSuccessStatusCode();

    using var cancelResponse = await client.PostAsJsonAsync(
      $"/v1/inspections/{InspectionId}/cancel",
      new InspectionLifecycleRequestDto("Cancelamento"));
    cancelResponse.EnsureSuccessStatusCode();

    using var checklistCreateResponse = await client.PostAsJsonAsync(
      $"/v1/inspections/{InspectionId}/checklist-items",
      ChecklistRequest());
    checklistCreateResponse.EnsureSuccessStatusCode();

    using var checklistUpdateResponse = await client.PutAsJsonAsync(
      $"/v1/inspections/{InspectionId}/checklist-items/{ChecklistItemId}",
      ChecklistRequest());
    checklistUpdateResponse.EnsureSuccessStatusCode();

    using var checklistDeleteResponse = await client.DeleteAsync(new Uri(
      $"/v1/inspections/{InspectionId}/checklist-items/{ChecklistItemId}",
      UriKind.Relative));
    checklistDeleteResponse.EnsureSuccessStatusCode();

    using var documentLinkResponse = await client.PostAsJsonAsync(
      $"/v1/inspections/{InspectionId}/document-links",
      new InspectionDocumentLinkRequestDto(Guid.NewGuid(), ChecklistItemId, "photo", "Foto"));
    documentLinkResponse.EnsureSuccessStatusCode();

    using var restoreResponse = await client.PostAsync($"/v1/inspections/{InspectionId}/restore", null);
    restoreResponse.EnsureSuccessStatusCode();

    using var archiveResponse = await client.DeleteAsync(new Uri(
      $"/v1/inspections/{InspectionId}",
      UriKind.Relative));
    Assert.Equal(HttpStatusCode.NoContent, archiveResponse.StatusCode);
  }

  private static WebApplicationFactory<Program> CreateFactory() =>
    new WebApplicationFactory<Program>()
      .WithWebHostBuilder(builder =>
      {
        builder.ConfigureTestServices(services =>
        {
          services.RemoveAll<IInspectionService>();
          services.AddSingleton<IInspectionService, FakeInspectionService>();
        });
      });

  private static InspectionScheduleRequestDto ScheduleRequest() =>
    new(
      "move-in",
      Guid.NewGuid(),
      Guid.NewGuid(),
      Guid.NewGuid(),
      DateTimeOffset.UtcNow.AddDays(1),
      Guid.NewGuid(),
      "Vistoria de entrada",
      "Observacoes",
      [new InspectionSignatureSlotRequestDto("Morador", "Joao da Silva", true)]);

  private static InspectionUpdateRequestDto UpdateRequest() =>
    new(
      "move-in",
      Guid.NewGuid(),
      Guid.NewGuid(),
      Guid.NewGuid(),
      DateTimeOffset.UtcNow.AddDays(1),
      Guid.NewGuid(),
      "Vistoria de entrada atualizada",
      "Observacoes",
      [new InspectionSignatureSlotRequestDto("Morador", "Joao da Silva", true)],
      "concurrency-token");

  private static InspectionChecklistItemRequestDto ChecklistRequest() =>
    new("Sala", "Piso", true, "good", "Sem danos", 0);

  private static InspectionDetailDto CreateDetail()
  {
    var property = new InspectionEntitySummaryDto(Guid.NewGuid(), "Casa Calabria", "Rua Calabria, 82", "/imoveis?id=1");
    var contract = new InspectionEntitySummaryDto(Guid.NewGuid(), "Contrato Casa", "Casa Calabria", "/contratos?id=1");
    var resident = new InspectionEntitySummaryDto(Guid.NewGuid(), "Joao da Silva", null, "/moradores?id=1");
    var assignee = new InspectionEntitySummaryDto(Guid.NewGuid(), "Ana Admin", "ana@example.com", "/administradores?id=1");
    var progress = new InspectionProgressDto(1, 1, 100m);

    return new InspectionDetailDto(
      InspectionId,
      "Vistoria de entrada",
      new StatusLabelDto("move-in", "Entrada", StatusLabelTones.Success),
      new StatusLabelDto("scheduled", "Agendada", StatusLabelTones.Warning),
      property,
      contract,
      resident,
      assignee,
      DateTimeOffset.UtcNow.AddDays(1),
      null,
      null,
      null,
      null,
      null,
      "Observacoes",
      progress,
      [
        new InspectionChecklistItemDto(
          ChecklistItemId,
          "Sala",
          "Piso",
          true,
          new StatusLabelDto("good", "Bom", StatusLabelTones.Success),
          "Sem danos",
          0,
          true,
          DateTimeOffset.UtcNow,
          null)
      ],
      [
        new InspectionDocumentDto(
          Guid.NewGuid(),
          ChecklistItemId,
          new StatusLabelDto("photo", "Foto", StatusLabelTones.Info),
          "Foto",
          "/documentos?entityType=inspection&entityId=1")
      ],
      [],
      [
        new InspectionSignatureSlotDto(
          Guid.NewGuid(),
          "Morador",
          "Joao da Silva",
          true,
          false,
          null,
          null,
          null)
      ],
      $"/timeline?entityType=inspection&entityId={InspectionId}",
      $"/auditoria?entityType=inspection&entityId={InspectionId}",
      DateTimeOffset.UtcNow,
      DateTimeOffset.UtcNow,
      null,
      "concurrency-token");
  }

  private static InspectionListItemDto CreateListItem()
  {
    var detail = CreateDetail();
    return new InspectionListItemDto(
      detail.Id,
      detail.Title,
      detail.Type,
      detail.Status,
      detail.Property,
      detail.Contract,
      detail.Resident,
      detail.Assignee,
      detail.ScheduledAt,
      detail.StartedAt,
      detail.CompletedAt,
      detail.Progress,
      true,
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

  private sealed class FakeInspectionService : IInspectionService
  {
    public Task<ApplicationOperationResult<PagedResultDto<InspectionListItemDto>>> ListAsync(
      InspectionListRequestDto request,
      CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();
      var page = new PagedResultDto<InspectionListItemDto>([CreateListItem()], request.Page, request.PageSize, 1);
      return Task.FromResult(ApplicationOperationResult<PagedResultDto<InspectionListItemDto>>.Success(page));
    }

    public Task<ApplicationOperationResult<InspectionDetailDto>> GetAsync(
      Guid id,
      string? locale = null,
      CancellationToken cancellationToken = default) =>
      Detail(cancellationToken);

    public Task<ApplicationOperationResult<InspectionDetailDto>> ScheduleAsync(
      InspectionScheduleRequestDto request,
      string? locale = null,
      CancellationToken cancellationToken = default) =>
      Detail(cancellationToken);

    public Task<ApplicationOperationResult<InspectionDetailDto>> UpdateAsync(
      Guid id,
      InspectionUpdateRequestDto request,
      string? locale = null,
      CancellationToken cancellationToken = default) =>
      Detail(cancellationToken);

    public Task<ApplicationOperationResult<InspectionDetailDto>> StartAsync(
      Guid id,
      string? locale = null,
      CancellationToken cancellationToken = default) =>
      Detail(cancellationToken);

    public Task<ApplicationOperationResult<InspectionDetailDto>> CompleteAsync(
      Guid id,
      InspectionLifecycleRequestDto request,
      string? locale = null,
      CancellationToken cancellationToken = default) =>
      Detail(cancellationToken);

    public Task<ApplicationOperationResult<InspectionDetailDto>> CancelAsync(
      Guid id,
      InspectionLifecycleRequestDto request,
      string? locale = null,
      CancellationToken cancellationToken = default) =>
      Detail(cancellationToken);

    public Task<ApplicationOperationResult> ArchiveAsync(Guid id, CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();
      return Task.FromResult(ApplicationOperationResult.Success());
    }

    public Task<ApplicationOperationResult<InspectionDetailDto>> RestoreAsync(
      Guid id,
      string? locale = null,
      CancellationToken cancellationToken = default) =>
      Detail(cancellationToken);

    public Task<ApplicationOperationResult<InspectionDetailDto>> AddChecklistItemAsync(
      Guid inspectionId,
      InspectionChecklistItemRequestDto request,
      string? locale = null,
      CancellationToken cancellationToken = default) =>
      Detail(cancellationToken);

    public Task<ApplicationOperationResult<InspectionDetailDto>> UpdateChecklistItemAsync(
      Guid inspectionId,
      Guid checklistItemId,
      InspectionChecklistItemRequestDto request,
      string? locale = null,
      CancellationToken cancellationToken = default) =>
      Detail(cancellationToken);

    public Task<ApplicationOperationResult<InspectionDetailDto>> DeleteChecklistItemAsync(
      Guid inspectionId,
      Guid checklistItemId,
      string? locale = null,
      CancellationToken cancellationToken = default) =>
      Detail(cancellationToken);

    public Task<ApplicationOperationResult<InspectionDetailDto>> LinkDocumentAsync(
      Guid inspectionId,
      InspectionDocumentLinkRequestDto request,
      string? locale = null,
      CancellationToken cancellationToken = default) =>
      Detail(cancellationToken);

    public Task<IReadOnlyList<StatusLabelDto>> GetTypeOptionsAsync(
      string? locale = null,
      CancellationToken cancellationToken = default) =>
      Options("move-in", "Entrada", cancellationToken);

    public Task<IReadOnlyList<StatusLabelDto>> GetStatusOptionsAsync(
      string? locale = null,
      CancellationToken cancellationToken = default) =>
      Options("scheduled", "Agendada", cancellationToken);

    public Task<IReadOnlyList<StatusLabelDto>> GetConditionRatingOptionsAsync(
      string? locale = null,
      CancellationToken cancellationToken = default) =>
      Options("good", "Bom", cancellationToken);

    public Task<IReadOnlyList<StatusLabelDto>> GetDocumentKindOptionsAsync(
      string? locale = null,
      CancellationToken cancellationToken = default) =>
      Options("photo", "Foto", cancellationToken);

    private static Task<ApplicationOperationResult<InspectionDetailDto>> Detail(CancellationToken cancellationToken)
    {
      cancellationToken.ThrowIfCancellationRequested();
      return Task.FromResult(ApplicationOperationResult<InspectionDetailDto>.Success(CreateDetail()));
    }

    private static Task<IReadOnlyList<StatusLabelDto>> Options(
      string code,
      string label,
      CancellationToken cancellationToken)
    {
      cancellationToken.ThrowIfCancellationRequested();
      return Task.FromResult<IReadOnlyList<StatusLabelDto>>([new StatusLabelDto(code, label)]);
    }
  }
}

#pragma warning restore CA1812, CA2000, CA2234
