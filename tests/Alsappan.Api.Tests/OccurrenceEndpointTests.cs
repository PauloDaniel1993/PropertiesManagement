using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Alsappan.Application.Common.Auth;
using Alsappan.Application.Common.Authorization;
using Alsappan.Application.Common.Contracts;
using Alsappan.Application.Common.Results;
using Alsappan.Application.Occurrences;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;

namespace Alsappan.Api.Tests;

#pragma warning disable CA1812, CA2000, CA2234

public sealed class OccurrenceEndpointTests
{
  private static readonly Guid OccurrenceId = new("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
  private static readonly Guid UserId = new("22222222-2222-2222-2222-222222222222");
  private static readonly Guid PropertyId = new("33333333-3333-3333-3333-333333333333");
  private static readonly Guid ResidentId = new("44444444-4444-4444-4444-444444444444");
  private static readonly Guid ContractId = new("55555555-5555-5555-5555-555555555555");
  private static readonly Guid DocumentId = new("66666666-6666-6666-6666-666666666666");

  [Fact]
  public async Task OccurrencesListRequiresAuthentication()
  {
    using var factory = CreateFactory(new FakeOccurrenceService());
    using var client = factory.CreateClient();

    using var response = await client.GetAsync(new Uri("/v1/occurrences", UriKind.Relative));

    Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
  }

  [Fact]
  public async Task OccurrenceEndpointsUseAuthenticatedUserAndBindFilters()
  {
    var occurrenceService = new FakeOccurrenceService();
    using var factory = CreateFactory(occurrenceService);
    using var client = factory.CreateClient();
    client.DefaultRequestHeaders.Authorization = new("Bearer", CreateJwt());

    using var listResponse = await client.GetAsync(new Uri(
      $"/v1/occurrences?search=vazamento&type=maintenance&priority=urgent&status=assigned" +
      $"&assignedUserId={UserId}&propertyId={PropertyId}&residentId={ResidentId}&contractId={ContractId}" +
      "&dateFrom=2026-07-01&dateTo=2026-07-31&unresolvedOnly=true&includeArchived=true&sort=-priority&locale=pt-BR",
      UriKind.Relative));
    listResponse.EnsureSuccessStatusCode();
    using var listPayload = JsonDocument.Parse(await listResponse.Content.ReadAsStringAsync());
    Assert.Equal(1, listPayload.RootElement.GetProperty("totalItems").GetInt32());
    Assert.Equal("vazamento", occurrenceService.LastListRequest!.Search);
    Assert.Equal(UserId, occurrenceService.LastListRequest.AssignedUserId);
    Assert.True(occurrenceService.LastListRequest.UnresolvedOnly);
    Assert.True(occurrenceService.LastListRequest.IncludeArchived);

    foreach (var optionsPath in new[]
      {
        "/v1/occurrences/type-options",
        "/v1/occurrences/priority-options",
        "/v1/occurrences/status-options"
      })
    {
      using var optionsResponse = await client.GetAsync(new Uri(optionsPath, UriKind.Relative));
      optionsResponse.EnsureSuccessStatusCode();
    }

    using var detailResponse = await client.GetAsync(new Uri($"/v1/occurrences/{OccurrenceId}", UriKind.Relative));
    detailResponse.EnsureSuccessStatusCode();

    using var createResponse = await client.PostAsJsonAsync("/v1/occurrences", CreateRequest());
    createResponse.EnsureSuccessStatusCode();

    using var updateResponse = await client.PutAsJsonAsync($"/v1/occurrences/{OccurrenceId}", UpdateRequest());
    updateResponse.EnsureSuccessStatusCode();

    using var assignResponse = await client.PostAsJsonAsync(
      $"/v1/occurrences/{OccurrenceId}/assign",
      new OccurrenceAssignmentRequestDto(UserId, "Direcionar manutencao"));
    assignResponse.EnsureSuccessStatusCode();

    using var priorityResponse = await client.PostAsJsonAsync(
      $"/v1/occurrences/{OccurrenceId}/priority",
      new OccurrencePriorityChangeRequestDto("urgent", "Risco de dano"));
    priorityResponse.EnsureSuccessStatusCode();

    using var statusResponse = await client.PostAsJsonAsync(
      $"/v1/occurrences/{OccurrenceId}/status",
      new OccurrenceStatusChangeRequestDto("in-progress", "Equipe acionada"));
    statusResponse.EnsureSuccessStatusCode();

    using var resolveResponse = await client.PostAsJsonAsync(
      $"/v1/occurrences/{OccurrenceId}/resolve",
      new OccurrenceResolutionRequestDto("Conserto realizado"));
    resolveResponse.EnsureSuccessStatusCode();

    using var cancelResponse = await client.PostAsJsonAsync(
      $"/v1/occurrences/{OccurrenceId}/cancel",
      new OccurrenceLifecycleRequestDto("Solicitacao retirada"));
    cancelResponse.EnsureSuccessStatusCode();

    using var commentResponse = await client.PostAsJsonAsync(
      $"/v1/occurrences/{OccurrenceId}/comments",
      new OccurrenceCommentRequestDto("Morador confirmou acesso.", true));
    commentResponse.EnsureSuccessStatusCode();

    using var attachmentResponse = await client.PostAsJsonAsync(
      $"/v1/occurrences/{OccurrenceId}/attachments",
      new OccurrenceAttachmentRequestDto(DocumentId, "Foto"));
    attachmentResponse.EnsureSuccessStatusCode();

    using var restoreResponse = await client.PostAsync($"/v1/occurrences/{OccurrenceId}/restore", null);
    restoreResponse.EnsureSuccessStatusCode();

    using var archiveResponse = await client.DeleteAsync(new Uri(
      $"/v1/occurrences/{OccurrenceId}",
      UriKind.Relative));
    Assert.Equal(HttpStatusCode.NoContent, archiveResponse.StatusCode);
  }

  [Fact]
  public async Task OccurrenceAdminEndpointsRejectResidentMembership()
  {
    var occurrenceService = new FakeOccurrenceService();
    using var factory = CreateFactory(occurrenceService);
    using var client = factory.CreateClient();
    client.DefaultRequestHeaders.Authorization = new("Bearer", CreateJwt(RoleCodes.ResidentUser));

    using var response = await client.GetAsync(new Uri("/v1/occurrences", UriKind.Relative));

    Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    Assert.Null(occurrenceService.LastListRequest);
  }

  private static WebApplicationFactory<Program> CreateFactory(FakeOccurrenceService occurrenceService) =>
    new WebApplicationFactory<Program>()
      .WithWebHostBuilder(builder =>
      {
        builder.ConfigureTestServices(services =>
        {
          services.RemoveAll<IOccurrenceService>();
          services.AddSingleton<IOccurrenceService>(occurrenceService);
        });
      });

  private static OccurrenceCreateRequestDto CreateRequest() =>
    new(
      "Vazamento na cozinha",
      "Morador relatou vazamento recorrente.",
      "maintenance",
      "high",
      PropertyId,
      ResidentId,
      ContractId,
      UserId,
      new DateOnly(2026, 7, 2));

  private static OccurrenceUpdateRequestDto UpdateRequest() =>
    new(
      "Vazamento na cozinha",
      "Morador relatou vazamento recorrente.",
      "maintenance",
      PropertyId,
      ResidentId,
      ContractId,
      new DateOnly(2026, 7, 2),
      "concurrency-token");

  private static OccurrenceDetailDto CreateDetail()
  {
    var property = new OccurrenceEntitySummaryDto(PropertyId, "Casa Calabria", "Rua Calabria, 82", "/imoveis?id=1");
    var resident = new OccurrenceEntitySummaryDto(ResidentId, "Joao da Silva", Route: "/moradores?id=1");
    var contract = new OccurrenceEntitySummaryDto(ContractId, "Contrato Casa", "Casa Calabria", "/contratos?id=1");
    var assignee = new OccurrenceUserSummaryDto(UserId, "Bruno Operador", "bruno@example.com");
    var now = DateTimeOffset.UtcNow;

    return new OccurrenceDetailDto(
      OccurrenceId,
      "Vazamento na cozinha",
      "Morador relatou vazamento recorrente.",
      new StatusLabelDto("maintenance", "Manutencao", StatusLabelTones.Info),
      new StatusLabelDto("high", "Alta", StatusLabelTones.Warning),
      new StatusLabelDto("assigned", "Atribuida", StatusLabelTones.Info),
      property,
      resident,
      contract,
      assignee,
      new DateOnly(2026, 7, 2),
      null,
      null,
      null,
      null,
      null,
      null,
      [new OccurrenceCommentDto(Guid.NewGuid(), "Morador confirmou acesso.", true, UserId, "Ana Admin", now)],
      [new OccurrenceDocumentDto(DocumentId, "Foto", "/documentos?id=1", now)],
      [new OccurrenceStatusHistoryDto(Guid.NewGuid(), null, new StatusLabelDto("assigned", "Atribuida"), null, UserId, "Ana Admin", now)],
      [new OccurrencePriorityHistoryDto(Guid.NewGuid(), null, new StatusLabelDto("high", "Alta"), null, UserId, "Ana Admin", now)],
      [new OccurrenceAssignmentHistoryDto(Guid.NewGuid(), null, assignee, "Direcionar manutencao", UserId, "Ana Admin", now)],
      $"/timeline?entityType=occurrence&entityId={OccurrenceId}",
      $"/auditoria?entityType=occurrence&entityId={OccurrenceId}",
      now,
      now,
      null,
      "concurrency-token");
  }

  private static OccurrenceListItemDto CreateListItem()
  {
    var detail = CreateDetail();
    return new OccurrenceListItemDto(
      detail.Id,
      detail.Title,
      detail.Description,
      detail.Type,
      detail.Priority,
      detail.Status,
      detail.Property,
      detail.Resident,
      detail.Contract,
      detail.AssignedUser,
      detail.DueDate,
      true,
      false,
      detail.CreatedAt,
      detail.UpdatedAt,
      detail.ConcurrencyToken);
  }

  private static string CreateJwt(string? roleCode = null)
  {
    var claims = new List<Claim>
    {
      new(JwtRegisteredClaimNames.Sub, UserId.ToString()),
      new(JwtRegisteredClaimNames.Email, "admin@alsappan.local")
    };

    if (!string.IsNullOrWhiteSpace(roleCode))
    {
      claims.Add(new Claim(
        AuthClaimTypes.Membership,
        JsonSerializer.Serialize(new OrganizationMembershipClaimDto(Guid.NewGuid(), [roleCode], [], true))));
    }

    var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
      "replace-this-dev-only-signing-key-with-at-least-32-characters"));
    var token = new JwtSecurityToken(
      issuer: "Alsappan",
      audience: "Alsappan.Web",
      claims: claims,
      expires: DateTime.UtcNow.AddMinutes(15),
      signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));

    return new JwtSecurityTokenHandler().WriteToken(token);
  }

  private sealed class FakeOccurrenceService : IOccurrenceService
  {
    public OccurrenceListRequestDto? LastListRequest { get; private set; }

    public Task<ApplicationOperationResult<PagedResultDto<OccurrenceListItemDto>>> ListAsync(
      OccurrenceListRequestDto request,
      CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();
      LastListRequest = request;
      var page = new PagedResultDto<OccurrenceListItemDto>([CreateListItem()], request.Page, request.PageSize, 1);
      return Task.FromResult(ApplicationOperationResult<PagedResultDto<OccurrenceListItemDto>>.Success(page));
    }

    public Task<ApplicationOperationResult<OccurrenceDetailDto>> GetAsync(
      Guid id,
      string? locale = null,
      CancellationToken cancellationToken = default) =>
      Detail(cancellationToken);

    public Task<ApplicationOperationResult<OccurrenceDetailDto>> CreateAsync(
      OccurrenceCreateRequestDto request,
      string? locale = null,
      CancellationToken cancellationToken = default) =>
      Detail(cancellationToken);

    public Task<ApplicationOperationResult<OccurrenceDetailDto>> UpdateAsync(
      Guid id,
      OccurrenceUpdateRequestDto request,
      string? locale = null,
      CancellationToken cancellationToken = default) =>
      Detail(cancellationToken);

    public Task<ApplicationOperationResult<OccurrenceDetailDto>> AssignAsync(
      Guid id,
      OccurrenceAssignmentRequestDto request,
      string? locale = null,
      CancellationToken cancellationToken = default) =>
      Detail(cancellationToken);

    public Task<ApplicationOperationResult<OccurrenceDetailDto>> ChangePriorityAsync(
      Guid id,
      OccurrencePriorityChangeRequestDto request,
      string? locale = null,
      CancellationToken cancellationToken = default) =>
      Detail(cancellationToken);

    public Task<ApplicationOperationResult<OccurrenceDetailDto>> ChangeStatusAsync(
      Guid id,
      OccurrenceStatusChangeRequestDto request,
      string? locale = null,
      CancellationToken cancellationToken = default) =>
      Detail(cancellationToken);

    public Task<ApplicationOperationResult<OccurrenceDetailDto>> ResolveAsync(
      Guid id,
      OccurrenceResolutionRequestDto request,
      string? locale = null,
      CancellationToken cancellationToken = default) =>
      Detail(cancellationToken);

    public Task<ApplicationOperationResult<OccurrenceDetailDto>> CancelAsync(
      Guid id,
      OccurrenceLifecycleRequestDto request,
      string? locale = null,
      CancellationToken cancellationToken = default) =>
      Detail(cancellationToken);

    public Task<ApplicationOperationResult> ArchiveAsync(Guid id, CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();
      return Task.FromResult(ApplicationOperationResult.Success());
    }

    public Task<ApplicationOperationResult<OccurrenceDetailDto>> RestoreAsync(
      Guid id,
      string? locale = null,
      CancellationToken cancellationToken = default) =>
      Detail(cancellationToken);

    public Task<ApplicationOperationResult<OccurrenceDetailDto>> AddCommentAsync(
      Guid id,
      OccurrenceCommentRequestDto request,
      string? locale = null,
      CancellationToken cancellationToken = default) =>
      Detail(cancellationToken);

    public Task<ApplicationOperationResult<OccurrenceDetailDto>> AttachDocumentAsync(
      Guid id,
      OccurrenceAttachmentRequestDto request,
      string? locale = null,
      CancellationToken cancellationToken = default) =>
      Detail(cancellationToken);

    public Task<IReadOnlyList<StatusLabelDto>> GetTypeOptionsAsync(
      string? locale = null,
      CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();
      return Task.FromResult<IReadOnlyList<StatusLabelDto>>([new StatusLabelDto("maintenance", "Manutencao")]);
    }

    public Task<IReadOnlyList<StatusLabelDto>> GetPriorityOptionsAsync(
      string? locale = null,
      CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();
      return Task.FromResult<IReadOnlyList<StatusLabelDto>>([new StatusLabelDto("high", "Alta")]);
    }

    public Task<IReadOnlyList<StatusLabelDto>> GetStatusOptionsAsync(
      string? locale = null,
      CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();
      return Task.FromResult<IReadOnlyList<StatusLabelDto>>([new StatusLabelDto("assigned", "Atribuida")]);
    }

    private static Task<ApplicationOperationResult<OccurrenceDetailDto>> Detail(CancellationToken cancellationToken)
    {
      cancellationToken.ThrowIfCancellationRequested();
      return Task.FromResult(ApplicationOperationResult<OccurrenceDetailDto>.Success(CreateDetail()));
    }
  }
}

#pragma warning restore CA1812, CA2000, CA2234
