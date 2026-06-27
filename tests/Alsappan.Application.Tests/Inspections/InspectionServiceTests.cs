using Alsappan.Application.Common.Audit;
using Alsappan.Application.Common.Auth;
using Alsappan.Application.Common.Authorization;
using Alsappan.Application.Common.Contracts;
using Alsappan.Application.Common.Events;
using Alsappan.Application.Common.Results;
using Alsappan.Application.Common.Tenancy;
using Alsappan.Application.Inspections;
using Alsappan.Application.Inspections.Repositories;
using Alsappan.Domain.Common.Events;
using Alsappan.Domain.Common.Identifiers;
using Alsappan.Domain.Inspections;

namespace Alsappan.Application.Tests.Inspections;

public sealed class InspectionServiceTests
{
  [Fact]
  public async Task ScheduleAsyncCreatesInspectionWithSignatureAndDashboardEvent()
  {
    var organizationId = OrganizationId.New();
    var repository = new FakeInspectionRepository(organizationId);
    var audit = new RecordingAuditWriter();
    var outbox = new RecordingOutboxWriter();
    var service = CreateService(
      organizationId,
      repository,
      audit,
      outbox,
      [PermissionCodes.Write(PermissionModules.Inspections)]);

    var result = await service.ScheduleAsync(CreateScheduleRequest(repository));

    Assert.True(result.Succeeded);
    var inspection = Assert.Single(repository.Inspections);
    Assert.Equal(InspectionStatus.Scheduled, inspection.Status);
    Assert.Equal(repository.Property.PropertyId.Value, result.Value!.Property.Id);
    Assert.Single(result.Value.SignatureSlots);
    Assert.Single(audit.Entries);
    var envelope = Assert.Single(outbox.Envelopes);
    Assert.Equal("inspection.scheduled", envelope.EventName);
    Assert.True((envelope.Consumers & ModuleEventConsumer.DashboardProjection) == ModuleEventConsumer.DashboardProjection);
  }

  [Fact]
  public async Task ChecklistProgressBlocksCompletionUntilRequiredItemsAreRated()
  {
    var organizationId = OrganizationId.New();
    var repository = new FakeInspectionRepository(organizationId);
    var service = CreateService(
      organizationId,
      repository,
      new RecordingAuditWriter(),
      new RecordingOutboxWriter(),
      [
        PermissionCodes.Write(PermissionModules.Inspections),
        PermissionCodes.Manage(PermissionModules.Inspections)
      ]);
    var scheduled = await service.ScheduleAsync(CreateScheduleRequest(repository));
    var inspectionId = scheduled.Value!.Id;

    var checklist = await service.AddChecklistItemAsync(
      inspectionId,
      new InspectionChecklistItemRequestDto("Sala", "Piso", true, "pending", null, 0));

    Assert.True(checklist.Succeeded);
    Assert.Equal(1, checklist.Value!.Progress.TotalItems);
    Assert.Equal(0, checklist.Value.Progress.CompletedItems);

    var blocked = await service.CompleteAsync(inspectionId, new InspectionLifecycleRequestDto("Concluir"));
    Assert.False(blocked.Succeeded);
    Assert.Equal(ApplicationOperationFailure.Conflict, blocked.Failure);

    var item = Assert.Single(checklist.Value.ChecklistItems);
    var updated = await service.UpdateChecklistItemAsync(
      inspectionId,
      item.Id,
      new InspectionChecklistItemRequestDto("Sala", "Piso", true, "good", "Sem danos", 0));
    var completed = await service.CompleteAsync(
      inspectionId,
      new InspectionLifecycleRequestDto("Checklist completo"));

    Assert.True(updated.Succeeded);
    Assert.True(completed.Succeeded);
    Assert.Equal("completed", completed.Value!.Status.Code);
    Assert.Equal(100m, completed.Value.Progress.Percentage);
    Assert.Equal("Checklist completo", completed.Value.CompletionNotes);
  }

  [Fact]
  public async Task LinkDocumentAsyncRequiresExistingDocumentAndReturnsLocalizedKind()
  {
    var organizationId = OrganizationId.New();
    var repository = new FakeInspectionRepository(organizationId);
    var documentId = EntityId.New();
    repository.Documents.Add(documentId);
    var service = CreateService(
      organizationId,
      repository,
      new RecordingAuditWriter(),
      new RecordingOutboxWriter(),
      [PermissionCodes.Write(PermissionModules.Inspections)]);
    var scheduled = await service.ScheduleAsync(CreateScheduleRequest(repository));
    var inspectionId = scheduled.Value!.Id;

    var missing = await service.LinkDocumentAsync(
      inspectionId,
      new InspectionDocumentLinkRequestDto(Guid.NewGuid(), null, "photo", "Foto"));
    var linked = await service.LinkDocumentAsync(
      inspectionId,
      new InspectionDocumentLinkRequestDto(documentId.Value, null, "photo", "Foto da sala"),
      "pt-BR");

    Assert.False(missing.Succeeded);
    Assert.Equal(ApplicationOperationFailure.Validation, missing.Failure);
    Assert.True(linked.Succeeded);
    var photo = Assert.Single(linked.Value!.PhotoDocuments);
    Assert.Equal("Foto", photo.Kind.Label);
    Assert.Contains("entityType=inspection", photo.Route, StringComparison.Ordinal);
  }

  [Fact]
  public async Task ListAsyncRequiresReadPermission()
  {
    var organizationId = OrganizationId.New();
    var service = CreateService(
      organizationId,
      new FakeInspectionRepository(organizationId),
      new RecordingAuditWriter(),
      new RecordingOutboxWriter(),
      [PermissionCodes.Write(PermissionModules.Inspections)]);

    var result = await service.ListAsync(new InspectionListRequestDto());

    Assert.False(result.Succeeded);
    Assert.Equal(ApplicationOperationFailure.Forbidden, result.Failure);
  }

  [Fact]
  public async Task OptionsUseRequestedLocale()
  {
    var organizationId = OrganizationId.New();
    var service = CreateService(
      organizationId,
      new FakeInspectionRepository(organizationId),
      new RecordingAuditWriter(),
      new RecordingOutboxWriter(),
      [PermissionCodes.Read(PermissionModules.Inspections)]);

    var englishTypes = await service.GetTypeOptionsAsync("en-US");
    var portugueseStatuses = await service.GetStatusOptionsAsync("pt-BR");

    Assert.Contains(englishTypes, option => option.Code == "move-in" && option.Label == "Move-in");
    Assert.Contains(portugueseStatuses, option => option.Code == "in-progress" && option.Label == "Em andamento");
  }

  private static InspectionService CreateService(
    OrganizationId organizationId,
    FakeInspectionRepository repository,
    RecordingAuditWriter auditWriter,
    RecordingOutboxWriter outboxWriter,
    IEnumerable<string> permissions) =>
    new(
      repository,
      new FixedPermissionService(permissions),
      new FixedActiveOrganizationContextResolver(organizationId),
      auditWriter,
      outboxWriter,
      TimeProvider.System);

  private static InspectionScheduleRequestDto CreateScheduleRequest(FakeInspectionRepository repository) =>
    new(
      "move-in",
      repository.Property.PropertyId.Value,
      repository.Contract.ContractId.Value,
      repository.Resident.ResidentId.Value,
      DateTimeOffset.UtcNow.AddDays(1),
      repository.Assignee.UserId.Value,
      "Vistoria de entrada",
      "Conferir sala e cozinha",
      [new InspectionSignatureSlotRequestDto("Morador", "Joao da Silva", true)]);

  private sealed class FakeInspectionRepository : IInspectionRepository
  {
    public FakeInspectionRepository(OrganizationId organizationId)
    {
      OrganizationId = organizationId;
      Property = new InspectionPropertySnapshot(EntityId.New(), "Casa Calabria", "Rua Calabria, 82");
      Resident = new InspectionResidentSnapshot(EntityId.New(), "Joao da Silva");
      Contract = new InspectionContractSnapshot(
        EntityId.New(),
        Property.PropertyId,
        Resident.ResidentId,
        [Resident.ResidentId],
        "Contrato Casa Calabria",
        Property.Name,
        Resident.Name,
        false);
      Assignee = new InspectionUserSnapshot(UserId.New(), "Ana Admin", "ana@example.com");
    }

    public OrganizationId OrganizationId { get; }

    public InspectionPropertySnapshot Property { get; }

    public InspectionResidentSnapshot Resident { get; }

    public InspectionContractSnapshot Contract { get; }

    public InspectionUserSnapshot Assignee { get; }

    public List<EntityId> Documents { get; } = [];

    public List<Inspection> Inspections { get; } = [];

    public Task<PagedResultDto<InspectionSnapshot>> ListAsync(
      InspectionListRequestDto request,
      OrganizationId organizationId,
      DateTimeOffset now,
      CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();
      var rows = Inspections
        .Where(inspection => inspection.OrganizationId == organizationId)
        .Where(inspection => request.IncludeArchived || !inspection.IsDeleted)
        .Select(BuildSnapshot)
        .ToArray();
      return Task.FromResult(new PagedResultDto<InspectionSnapshot>(
        rows,
        request.Page,
        request.PageSize,
        rows.Length));
    }

    public Task<Inspection?> FindAsync(
      EntityId inspectionId,
      OrganizationId organizationId,
      bool includeArchived = false,
      CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();
      return Task.FromResult(Inspections.FirstOrDefault(inspection =>
        inspection.Id == inspectionId &&
        inspection.OrganizationId == organizationId &&
        (includeArchived || !inspection.IsDeleted)));
    }

    public async Task<InspectionSnapshot?> FindSnapshotAsync(
      EntityId inspectionId,
      OrganizationId organizationId,
      bool includeArchived = false,
      CancellationToken cancellationToken = default)
    {
      var inspection = await FindAsync(inspectionId, organizationId, includeArchived, cancellationToken)
        .ConfigureAwait(false);
      return inspection is null ? null : BuildSnapshot(inspection);
    }

    public Task<InspectionPropertySnapshot?> GetPropertySnapshotAsync(
      EntityId propertyId,
      OrganizationId organizationId,
      CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();
      return Task.FromResult<InspectionPropertySnapshot?>(
        organizationId == OrganizationId && propertyId == Property.PropertyId ? Property : null);
    }

    public Task<InspectionContractSnapshot?> GetContractSnapshotAsync(
      EntityId contractId,
      OrganizationId organizationId,
      CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();
      return Task.FromResult<InspectionContractSnapshot?>(
        organizationId == OrganizationId && contractId == Contract.ContractId ? Contract : null);
    }

    public Task<InspectionResidentSnapshot?> GetResidentSnapshotAsync(
      EntityId residentId,
      OrganizationId organizationId,
      CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();
      return Task.FromResult<InspectionResidentSnapshot?>(
        organizationId == OrganizationId && residentId == Resident.ResidentId ? Resident : null);
    }

    public Task<InspectionUserSnapshot?> GetAssigneeSnapshotAsync(
      UserId userId,
      OrganizationId organizationId,
      CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();
      return Task.FromResult<InspectionUserSnapshot?>(
        organizationId == OrganizationId && userId == Assignee.UserId ? Assignee : null);
    }

    public Task<bool> DocumentExistsAsync(
      EntityId documentId,
      OrganizationId organizationId,
      CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();
      return Task.FromResult(organizationId == OrganizationId && Documents.Contains(documentId));
    }

    public Task AddAsync(Inspection inspection, CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();
      Inspections.Add(inspection);
      return Task.CompletedTask;
    }

    public Task UpdateAsync(Inspection inspection, CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();
      return Task.CompletedTask;
    }

    private InspectionSnapshot BuildSnapshot(Inspection inspection) =>
      new(
        inspection,
        Property,
        inspection.ContractId == Contract.ContractId ? Contract : null,
        inspection.ResidentId == Resident.ResidentId ? Resident : null,
        Assignee,
        inspection.DocumentLinks
          .Where(link => !link.IsDeleted)
          .Select(link => new InspectionDocumentSnapshot(
            link.DocumentId,
            link.ChecklistItemId,
            link.Kind,
            link.Label))
          .ToArray());
  }

  private sealed class FixedPermissionService : IPermissionService
  {
    private readonly HashSet<string> permissions;

    public FixedPermissionService(IEnumerable<string> permissions)
    {
      this.permissions = new HashSet<string>(permissions, StringComparer.OrdinalIgnoreCase);
    }

    public ValueTask<PermissionEvaluationResult> AuthorizeAsync(
      string permissionCode,
      CancellationToken cancellationToken = default) =>
      AuthorizeAsync(new PermissionRequirement(permissionCode), cancellationToken);

    public ValueTask<PermissionEvaluationResult> AuthorizeAsync(
      PermissionRequirement requirement,
      CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();
      var organizationId = OrganizationId.New();
      var granted = permissions.Contains(PermissionCodes.Wildcard) ||
        permissions.Contains(requirement.PermissionCode);
      return ValueTask.FromResult(
        granted
          ? PermissionEvaluationResult.Granted(requirement, organizationId)
          : PermissionEvaluationResult.Denied(
            requirement,
            PermissionEvaluationFailure.PermissionDenied,
            organizationId));
    }

    public ValueTask<IReadOnlySet<string>> GetEffectivePermissionsAsync(CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();
      return ValueTask.FromResult<IReadOnlySet<string>>(permissions);
    }
  }

  private sealed class FixedActiveOrganizationContextResolver : IActiveOrganizationContextResolver
  {
    private readonly ActiveOrganizationContext context;

    public FixedActiveOrganizationContextResolver(OrganizationId organizationId)
    {
      var membership = new OrganizationMembership(organizationId, permissionCodes: [PermissionCodes.Wildcard]);
      var user = new AuthenticatedUser(UserId.New(), "admin@alsappan.local", "Ana Admin", [membership]);
      context = new ActiveOrganizationContext(user, membership);
    }

    public ValueTask<ActiveOrganizationResolutionResult> ResolveAsync(CancellationToken cancellationToken = default) =>
      ValueTask.FromResult(ActiveOrganizationResolutionResult.Success(context));
  }

  private sealed class RecordingAuditWriter : IAuditWriter
  {
    public List<AuditEntryDraft> Entries { get; } = [];

    public Task WriteAsync(AuditEntryDraft entry, CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();
      Entries.Add(entry);
      return Task.CompletedTask;
    }
  }

  private sealed class RecordingOutboxWriter : IModuleEventOutboxWriter
  {
    public List<ModuleEventEnvelope> Envelopes { get; } = [];

    public Task EnqueueAsync(ModuleEventEnvelope envelope, CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();
      Envelopes.Add(envelope);
      return Task.CompletedTask;
    }
  }
}
