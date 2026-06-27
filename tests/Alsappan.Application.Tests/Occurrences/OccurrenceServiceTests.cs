using Alsappan.Application.Common.Audit;
using Alsappan.Application.Common.Auth;
using Alsappan.Application.Common.Authorization;
using Alsappan.Application.Common.Contracts;
using Alsappan.Application.Common.Events;
using Alsappan.Application.Common.Results;
using Alsappan.Application.Common.Tenancy;
using Alsappan.Application.Occurrences;
using Alsappan.Application.Occurrences.Repositories;
using Alsappan.Domain.Common.Events;
using Alsappan.Domain.Common.Identifiers;
using Alsappan.Domain.Occurrences;

namespace Alsappan.Application.Tests.Occurrences;

public sealed class OccurrenceServiceTests
{
  [Fact]
  public async Task CreateAsyncResolvesContractAssignmentAndWritesSideEffects()
  {
    var organizationId = OrganizationId.New();
    var repository = new FakeOccurrenceRepository(organizationId);
    var audit = new RecordingAuditWriter();
    var outbox = new RecordingOutboxWriter();
    var service = CreateService(
      organizationId,
      repository,
      audit,
      outbox,
      [PermissionCodes.Write(PermissionModules.Occurrences)]);

    var result = await service.CreateAsync(new OccurrenceCreateRequestDto(
      "Vazamento na cozinha",
      "Morador relatou vazamento recorrente.",
      "maintenance",
      "high",
      null,
      null,
      repository.Contract.ContractId.Value,
      repository.Assignee.UserId.Value,
      new DateOnly(2026, 7, 2)));

    Assert.True(result.Succeeded);
    Assert.Equal("assigned", result.Value!.Status.Code);
    Assert.Equal(repository.Property.PropertyId.Value, result.Value.Property!.Id);
    Assert.Equal(repository.Resident.ResidentId.Value, result.Value.Resident!.Id);
    Assert.Equal(repository.Assignee.UserId.Value, result.Value.AssignedUser!.Id);
    Assert.Single(repository.Occurrences);
    Assert.Single(audit.Entries);
    var envelope = Assert.Single(outbox.Envelopes);
    Assert.Equal("occurrence.created", envelope.EventName);
    Assert.True(envelope.Consumers.HasFlag(ModuleEventConsumer.Notifications));
    Assert.True(envelope.Consumers.HasFlag(ModuleEventConsumer.DashboardProjection));
  }

  [Fact]
  public async Task AssignAsyncRequiresManagePermissionAndEmitsAssignmentNotification()
  {
    var organizationId = OrganizationId.New();
    var repository = new FakeOccurrenceRepository(organizationId);
    var occurrence = repository.AddExistingOccurrence();
    var forbiddenService = CreateService(
      organizationId,
      repository,
      new RecordingAuditWriter(),
      new RecordingOutboxWriter(),
      [PermissionCodes.Write(PermissionModules.Occurrences)]);

    var forbidden = await forbiddenService.AssignAsync(
      occurrence.Id.Value,
      new OccurrenceAssignmentRequestDto(repository.Assignee.UserId.Value, "Direcionar manutencao"));

    Assert.False(forbidden.Succeeded);
    Assert.Equal(ApplicationOperationFailure.Forbidden, forbidden.Failure);

    var outbox = new RecordingOutboxWriter();
    var service = CreateService(
      organizationId,
      repository,
      new RecordingAuditWriter(),
      outbox,
      [PermissionCodes.Manage(PermissionModules.Occurrences)]);

    var assigned = await service.AssignAsync(
      occurrence.Id.Value,
      new OccurrenceAssignmentRequestDto(repository.Assignee.UserId.Value, "Direcionar manutencao"));

    Assert.True(assigned.Succeeded);
    Assert.Equal("assigned", assigned.Value!.Status.Code);
    var envelope = Assert.Single(outbox.Envelopes);
    Assert.Equal("occurrence.assigned", envelope.EventName);
    Assert.True(envelope.Consumers.HasFlag(ModuleEventConsumer.Notifications));
    Assert.Contains(envelope.RelatedEntities, entity => entity.EntityType == "identityUser");
  }

  [Fact]
  public async Task CommentsAndAttachmentsUseWritePermissionAndValidateDocuments()
  {
    var organizationId = OrganizationId.New();
    var repository = new FakeOccurrenceRepository(organizationId);
    var occurrence = repository.AddExistingOccurrence();
    var outbox = new RecordingOutboxWriter();
    var service = CreateService(
      organizationId,
      repository,
      new RecordingAuditWriter(),
      outbox,
      [PermissionCodes.Write(PermissionModules.Occurrences)]);

    var missingDocument = await service.AttachDocumentAsync(
      occurrence.Id.Value,
      new OccurrenceAttachmentRequestDto(Guid.NewGuid(), "Foto"));

    Assert.False(missingDocument.Succeeded);
    Assert.Equal(ApplicationOperationFailure.Validation, missingDocument.Failure);

    var commented = await service.AddCommentAsync(
      occurrence.Id.Value,
      new OccurrenceCommentRequestDto("Morador confirmou acesso.", true));
    var attached = await service.AttachDocumentAsync(
      occurrence.Id.Value,
      new OccurrenceAttachmentRequestDto(repository.DocumentId.Value, "Foto do vazamento"));

    Assert.True(commented.Succeeded);
    Assert.True(attached.Succeeded);
    Assert.Single(attached.Value!.Comments);
    Assert.Single(attached.Value.Attachments);
    Assert.Contains(outbox.Envelopes, envelope => envelope.EventName == "occurrence.comment-added");
    Assert.Contains(outbox.Envelopes, envelope => envelope.EventName == "occurrence.document-linked");
  }

  [Fact]
  public async Task LifecycleWorkflowRequiresManageAndUpdatesDashboardProjectionData()
  {
    var organizationId = OrganizationId.New();
    var repository = new FakeOccurrenceRepository(organizationId);
    var occurrence = repository.AddExistingOccurrence();
    var outbox = new RecordingOutboxWriter();
    var service = CreateService(
      organizationId,
      repository,
      new RecordingAuditWriter(),
      outbox,
      [PermissionCodes.Manage(PermissionModules.Occurrences)]);

    var priority = await service.ChangePriorityAsync(
      occurrence.Id.Value,
      new OccurrencePriorityChangeRequestDto("urgent", "Risco de dano"));
    var status = await service.ChangeStatusAsync(
      occurrence.Id.Value,
      new OccurrenceStatusChangeRequestDto("in-progress", "Equipe acionada"));
    var resolved = await service.ResolveAsync(
      occurrence.Id.Value,
      new OccurrenceResolutionRequestDto("Conserto realizado."));

    Assert.True(priority.Succeeded);
    Assert.True(status.Succeeded);
    Assert.True(resolved.Succeeded);
    var resolvedEnvelope = Assert.Single(outbox.Envelopes, envelope => envelope.EventName == "occurrence.resolved");
    Assert.Equal("False", resolvedEnvelope.Data["unresolved"]);
    Assert.True(resolvedEnvelope.Consumers.HasFlag(ModuleEventConsumer.DashboardProjection));
  }

  [Fact]
  public async Task ArchiveRestoreAndCatalogOptionsUseExpectedPermissionsAndLocalization()
  {
    var organizationId = OrganizationId.New();
    var repository = new FakeOccurrenceRepository(organizationId);
    var occurrence = repository.AddExistingOccurrence();
    var service = CreateService(
      organizationId,
      repository,
      new RecordingAuditWriter(),
      new RecordingOutboxWriter(),
      [PermissionCodes.Archive(PermissionModules.Occurrences)]);

    var archived = await service.ArchiveAsync(occurrence.Id.Value);
    var restored = await service.RestoreAsync(occurrence.Id.Value, "pt-BR");
    var englishTypes = await service.GetTypeOptionsAsync("en-US");
    var portuguesePriorities = await service.GetPriorityOptionsAsync("pt-BR");

    Assert.True(archived.Succeeded);
    Assert.True(restored.Succeeded);
    Assert.Contains(englishTypes, option => option.Code == "maintenance" && option.Label == "Maintenance");
    Assert.Contains(portuguesePriorities, option => option.Code == "urgent" && option.Label == "Urgente");
  }

  private static OccurrenceService CreateService(
    OrganizationId organizationId,
    FakeOccurrenceRepository repository,
    RecordingAuditWriter auditWriter,
    RecordingOutboxWriter outboxWriter,
    IEnumerable<string> permissions) =>
    new(
      repository,
      new FixedPermissionService(permissions),
      new FixedActiveOrganizationContextResolver(organizationId, repository.AdminUser.UserId),
      auditWriter,
      outboxWriter,
      TimeProvider.System);

  private sealed class FakeOccurrenceRepository : IOccurrenceRepository
  {
    public FakeOccurrenceRepository(OrganizationId organizationId)
    {
      OrganizationId = organizationId;
      Property = new OccurrencePropertySnapshot(EntityId.New(), "Casa Calabria", "Rua Calabria, 82");
      Resident = new OccurrenceResidentSnapshot(EntityId.New(), "Joao da Silva");
      Contract = new OccurrenceContractSnapshot(
        EntityId.New(),
        Property.PropertyId,
        Resident.ResidentId,
        [Resident.ResidentId],
        "Contrato Casa Calabria",
        Property.Name,
        Resident.Name);
      AdminUser = new OccurrenceUserSnapshot(UserId.New(), "Ana Admin", "ana@example.com");
      Assignee = new OccurrenceUserSnapshot(UserId.New(), "Bruno Operador", "bruno@example.com");
      DocumentId = EntityId.New();
      Users[AdminUser.UserId] = AdminUser;
      Users[Assignee.UserId] = Assignee;
    }

    public OrganizationId OrganizationId { get; }

    public OccurrencePropertySnapshot Property { get; }

    public OccurrenceResidentSnapshot Resident { get; }

    public OccurrenceContractSnapshot Contract { get; }

    public OccurrenceUserSnapshot AdminUser { get; }

    public OccurrenceUserSnapshot Assignee { get; }

    public EntityId DocumentId { get; }

    public Dictionary<UserId, OccurrenceUserSnapshot> Users { get; } = [];

    public List<Occurrence> Occurrences { get; } = [];

    public Occurrence AddExistingOccurrence()
    {
      var occurrence = Occurrence.Create(
        EntityId.New(),
        OrganizationId,
        "Vazamento na cozinha",
        "Morador relatou vazamento recorrente.",
        OccurrenceType.Maintenance,
        OccurrencePriority.High,
        Property.PropertyId,
        Resident.ResidentId,
        null,
        null,
        new DateOnly(2026, 7, 2),
        Property.Name,
        Resident.Name,
        null,
        null,
        DateTimeOffset.UtcNow,
        AdminUser.UserId);
      Occurrences.Add(occurrence);
      return occurrence;
    }

    public Task<PagedResultDto<OccurrenceSnapshot>> ListAsync(
      OccurrenceListRequestDto request,
      OrganizationId organizationId,
      CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();
      var rows = Occurrences
        .Where(occurrence => occurrence.OrganizationId == organizationId)
        .Select(BuildSnapshot)
        .ToArray();
      return Task.FromResult(new PagedResultDto<OccurrenceSnapshot>(rows, request.Page, request.PageSize, rows.Length));
    }

    public Task<Occurrence?> FindAsync(
      EntityId occurrenceId,
      OrganizationId organizationId,
      bool includeArchived = false,
      CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();
      return Task.FromResult(Occurrences.FirstOrDefault(occurrence =>
        occurrence.Id == occurrenceId &&
        occurrence.OrganizationId == organizationId &&
        (includeArchived || (!occurrence.IsDeleted && occurrence.Status != OccurrenceStatus.Archived))));
    }

    public async Task<OccurrenceSnapshot?> FindSnapshotAsync(
      EntityId occurrenceId,
      OrganizationId organizationId,
      bool includeArchived = false,
      CancellationToken cancellationToken = default)
    {
      var occurrence = await FindAsync(occurrenceId, organizationId, includeArchived, cancellationToken)
        .ConfigureAwait(false);
      return occurrence is null ? null : BuildSnapshot(occurrence);
    }

    public Task<OccurrenceContractSnapshot?> GetContractSnapshotAsync(
      EntityId contractId,
      OrganizationId organizationId,
      CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();
      return Task.FromResult<OccurrenceContractSnapshot?>(
        organizationId == OrganizationId && contractId == Contract.ContractId ? Contract : null);
    }

    public Task<OccurrencePropertySnapshot?> GetPropertySnapshotAsync(
      EntityId propertyId,
      OrganizationId organizationId,
      CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();
      return Task.FromResult<OccurrencePropertySnapshot?>(
        organizationId == OrganizationId && propertyId == Property.PropertyId ? Property : null);
    }

    public Task<OccurrenceResidentSnapshot?> GetResidentSnapshotAsync(
      EntityId residentId,
      OrganizationId organizationId,
      CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();
      return Task.FromResult<OccurrenceResidentSnapshot?>(
        organizationId == OrganizationId && residentId == Resident.ResidentId ? Resident : null);
    }

    public Task<OccurrenceUserSnapshot?> GetAssignableUserSnapshotAsync(
      UserId userId,
      OrganizationId organizationId,
      CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();
      return Task.FromResult<OccurrenceUserSnapshot?>(
        organizationId == OrganizationId && Users.TryGetValue(userId, out var user) ? user : null);
    }

    public Task<bool> DocumentExistsAsync(
      EntityId documentId,
      OrganizationId organizationId,
      CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();
      return Task.FromResult(organizationId == OrganizationId && documentId == DocumentId);
    }

    public Task AddAsync(Occurrence occurrence, CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();
      Occurrences.Add(occurrence);
      return Task.CompletedTask;
    }

    public Task UpdateAsync(Occurrence occurrence, CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();
      return Task.CompletedTask;
    }

    private OccurrenceSnapshot BuildSnapshot(Occurrence occurrence)
    {
      var assignedUser = occurrence.AssignedUserId.HasValue && Users.TryGetValue(occurrence.AssignedUserId.Value, out var user)
        ? user
        : null;
      var comments = occurrence.Comments
        .Select(comment => new OccurrenceCommentSnapshot(
          comment,
          comment.CreatedByUserId.HasValue && Users.TryGetValue(comment.CreatedByUserId.Value, out var author) ? author : null))
        .ToArray();
      var statusHistory = occurrence.StatusHistory
        .Select(history => new OccurrenceStatusHistorySnapshot(
          history,
          history.CreatedByUserId.HasValue && Users.TryGetValue(history.CreatedByUserId.Value, out var actor) ? actor : null))
        .ToArray();
      var priorityHistory = occurrence.PriorityHistory
        .Select(history => new OccurrencePriorityHistorySnapshot(
          history,
          history.CreatedByUserId.HasValue && Users.TryGetValue(history.CreatedByUserId.Value, out var actor) ? actor : null))
        .ToArray();
      var assignmentHistory = occurrence.AssignmentHistory
        .Select(history => new OccurrenceAssignmentHistorySnapshot(
          history,
          history.PreviousAssignedUserId.HasValue && Users.TryGetValue(history.PreviousAssignedUserId.Value, out var previous) ? previous : null,
          history.NewAssignedUserId.HasValue && Users.TryGetValue(history.NewAssignedUserId.Value, out var next) ? next : null,
          history.CreatedByUserId.HasValue && Users.TryGetValue(history.CreatedByUserId.Value, out var actor) ? actor : null))
        .ToArray();

      return new OccurrenceSnapshot(
        occurrence,
        occurrence.PropertyId == Property.PropertyId ? Property : null,
        occurrence.ResidentId == Resident.ResidentId ? Resident : null,
        occurrence.ContractId == Contract.ContractId ? Contract : null,
        assignedUser,
        comments,
        occurrence.Attachments.Select(link => new OccurrenceDocumentSnapshot(link.DocumentId, link.Label, link.CreatedAt)).ToArray(),
        statusHistory,
        priorityHistory,
        assignmentHistory);
    }
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

    public FixedActiveOrganizationContextResolver(OrganizationId organizationId, UserId userId)
    {
      var membership = new OrganizationMembership(organizationId, permissionCodes: [PermissionCodes.Wildcard]);
      var user = new AuthenticatedUser(userId, "ana@example.com", "Ana Admin", [membership]);
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
