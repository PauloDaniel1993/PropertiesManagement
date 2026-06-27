using System.Globalization;
using Alsappan.Application.Common.Audit;
using Alsappan.Application.Common.Authorization;
using Alsappan.Application.Common.Contracts;
using Alsappan.Application.Common.Events;
using Alsappan.Application.Common.Results;
using Alsappan.Application.Common.Tenancy;
using Alsappan.Application.Common.Validation;
using Alsappan.Application.Inspections.Repositories;
using Alsappan.Domain.Common.Events;
using Alsappan.Domain.Common.Identifiers;
using Alsappan.Domain.Inspections;

namespace Alsappan.Application.Inspections;

public sealed class InspectionService : IInspectionService
{
  private readonly IInspectionRepository inspectionRepository;
  private readonly IPermissionService permissionService;
  private readonly IActiveOrganizationContextResolver activeOrganizationContextResolver;
  private readonly IAuditWriter auditWriter;
  private readonly IModuleEventOutboxWriter outboxWriter;
  private readonly TimeProvider timeProvider;

  public InspectionService(
    IInspectionRepository inspectionRepository,
    IPermissionService permissionService,
    IActiveOrganizationContextResolver activeOrganizationContextResolver,
    IAuditWriter auditWriter,
    IModuleEventOutboxWriter outboxWriter,
    TimeProvider timeProvider)
  {
    this.inspectionRepository = inspectionRepository ??
      throw new ArgumentNullException(nameof(inspectionRepository));
    this.permissionService = permissionService ?? throw new ArgumentNullException(nameof(permissionService));
    this.activeOrganizationContextResolver = activeOrganizationContextResolver ??
      throw new ArgumentNullException(nameof(activeOrganizationContextResolver));
    this.auditWriter = auditWriter ?? throw new ArgumentNullException(nameof(auditWriter));
    this.outboxWriter = outboxWriter ?? throw new ArgumentNullException(nameof(outboxWriter));
    this.timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
  }

  public async Task<ApplicationOperationResult<PagedResultDto<InspectionListItemDto>>> ListAsync(
    InspectionListRequestDto request,
    CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(request);

    var context = await AuthorizeContextAsync(PermissionCodes.Read(PermissionModules.Inspections), cancellationToken)
      .ConfigureAwait(false);
    if (context is null)
    {
      return ApplicationOperationResult<PagedResultDto<InspectionListItemDto>>.Failed(
        ApplicationOperationFailure.Forbidden);
    }

    var now = timeProvider.GetUtcNow();
    var page = await inspectionRepository.ListAsync(request, context.OrganizationId, now, cancellationToken)
      .ConfigureAwait(false);
    var items = page.Items.Select(snapshot => ToListItem(snapshot, request.Locale, now)).ToArray();

    return ApplicationOperationResult<PagedResultDto<InspectionListItemDto>>.Success(
      new PagedResultDto<InspectionListItemDto>(items, page.Page, page.PageSize, page.TotalItems));
  }

  public async Task<ApplicationOperationResult<InspectionDetailDto>> GetAsync(
    Guid id,
    string? locale = null,
    CancellationToken cancellationToken = default)
  {
    var validation = ValidateId(id).ToList();
    if (validation.Count > 0)
    {
      return ApplicationOperationResult<InspectionDetailDto>.Invalid(validation);
    }

    var context = await AuthorizeContextAsync(PermissionCodes.Read(PermissionModules.Inspections), cancellationToken)
      .ConfigureAwait(false);
    if (context is null)
    {
      return ApplicationOperationResult<InspectionDetailDto>.Failed(ApplicationOperationFailure.Forbidden);
    }

    var snapshot = await inspectionRepository.FindSnapshotAsync(
        new EntityId(id),
        context.OrganizationId,
        includeArchived: true,
        cancellationToken)
      .ConfigureAwait(false);

    return snapshot is null
      ? ApplicationOperationResult<InspectionDetailDto>.Failed(ApplicationOperationFailure.NotFound)
      : ApplicationOperationResult<InspectionDetailDto>.Success(
        ToDetail(snapshot, locale, timeProvider.GetUtcNow()));
  }

  public async Task<ApplicationOperationResult<InspectionDetailDto>> ScheduleAsync(
    InspectionScheduleRequestDto request,
    string? locale = null,
    CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(request);

    var validation = ValidateScheduleRequest(request).ToList();
    validation.AddRange(ValidateSignatureSlots(request.SignatureSlots));
    if (validation.Count > 0)
    {
      return ApplicationOperationResult<InspectionDetailDto>.Invalid(validation);
    }

    var context = await AuthorizeContextAsync(PermissionCodes.Write(PermissionModules.Inspections), cancellationToken)
      .ConfigureAwait(false);
    if (context is null)
    {
      return ApplicationOperationResult<InspectionDetailDto>.Failed(ApplicationOperationFailure.Forbidden);
    }

    var related = await ResolveRelatedEntitiesAsync(request, context.OrganizationId, cancellationToken)
      .ConfigureAwait(false);
    validation.AddRange(related.Errors);
    if (validation.Count > 0)
    {
      return ApplicationOperationResult<InspectionDetailDto>.Invalid(validation);
    }

    if (!InspectionCatalog.TryParseType(request.Type, out var type))
    {
      return ApplicationOperationResult<InspectionDetailDto>.Invalid(ValidateScheduleRequest(request).ToList());
    }

    var now = timeProvider.GetUtcNow();
    var inspection = Inspection.Create(
      EntityId.New(),
      context.OrganizationId,
      type,
      related.PropertyId!.Value,
      related.ContractId,
      related.ResidentId,
      request.ScheduledAt,
      related.Assignee!.UserId,
      related.Assignee.Name,
      request.Title,
      request.Notes,
      related.Property?.Name,
      related.Contract?.DisplayName,
      related.Resident?.Name,
      now,
      context.UserId);

    if (request.SignatureSlots is { Count: > 0 })
    {
      inspection.ReplaceSignatureSlots(
        request.SignatureSlots.Select(ToSignatureSlotDraft),
        now,
        context.UserId);
    }

    await inspectionRepository.AddAsync(inspection, cancellationToken).ConfigureAwait(false);
    var snapshot = await inspectionRepository.FindSnapshotAsync(
        inspection.Id,
        context.OrganizationId,
        includeArchived: true,
        cancellationToken)
      .ConfigureAwait(false);
    await WriteMutationSideEffectsAsync("inspection.scheduled", inspection, context, cancellationToken)
      .ConfigureAwait(false);

    return ApplicationOperationResult<InspectionDetailDto>.Success(ToDetail(snapshot!, locale, now));
  }

  public async Task<ApplicationOperationResult<InspectionDetailDto>> UpdateAsync(
    Guid id,
    InspectionUpdateRequestDto request,
    string? locale = null,
    CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(request);

    var validation = ValidateId(id).Concat(ValidateUpdateRequest(request)).ToList();
    validation.AddRange(ValidateSignatureSlots(request.SignatureSlots));
    if (validation.Count > 0)
    {
      return ApplicationOperationResult<InspectionDetailDto>.Invalid(validation);
    }

    var context = await AuthorizeContextAsync(PermissionCodes.Write(PermissionModules.Inspections), cancellationToken)
      .ConfigureAwait(false);
    if (context is null)
    {
      return ApplicationOperationResult<InspectionDetailDto>.Failed(ApplicationOperationFailure.Forbidden);
    }

    var inspection = await inspectionRepository.FindAsync(new EntityId(id), context.OrganizationId, false, cancellationToken)
      .ConfigureAwait(false);
    if (inspection is null)
    {
      return ApplicationOperationResult<InspectionDetailDto>.Failed(ApplicationOperationFailure.NotFound);
    }

    if (!string.IsNullOrWhiteSpace(request.ConcurrencyToken) &&
      !string.Equals(inspection.ConcurrencyToken.Value, request.ConcurrencyToken, StringComparison.Ordinal))
    {
      return ApplicationOperationResult<InspectionDetailDto>.Failed(ApplicationOperationFailure.Conflict);
    }

    var related = await ResolveRelatedEntitiesAsync(request, context.OrganizationId, cancellationToken)
      .ConfigureAwait(false);
    validation.AddRange(related.Errors);
    if (validation.Count > 0)
    {
      return ApplicationOperationResult<InspectionDetailDto>.Invalid(validation);
    }

    if (!InspectionCatalog.TryParseType(request.Type, out var type))
    {
      return ApplicationOperationResult<InspectionDetailDto>.Invalid(ValidateUpdateRequest(request).ToList());
    }

    var now = timeProvider.GetUtcNow();
    try
    {
      inspection.UpdateSchedule(
        type,
        related.PropertyId!.Value,
        related.ContractId,
        related.ResidentId,
        request.ScheduledAt,
        related.Assignee!.UserId,
        related.Assignee.Name,
        request.Title,
        request.Notes,
        related.Property?.Name,
        related.Contract?.DisplayName,
        related.Resident?.Name,
        now,
        context.UserId);
      if (request.SignatureSlots is not null)
      {
        inspection.ReplaceSignatureSlots(
          request.SignatureSlots.Select(ToSignatureSlotDraft),
          now,
          context.UserId);
      }
    }
    catch (InvalidOperationException)
    {
      return ApplicationOperationResult<InspectionDetailDto>.Failed(ApplicationOperationFailure.Conflict);
    }

    await inspectionRepository.UpdateAsync(inspection, cancellationToken).ConfigureAwait(false);
    var snapshot = await inspectionRepository.FindSnapshotAsync(inspection.Id, context.OrganizationId, true, cancellationToken)
      .ConfigureAwait(false);
    await WriteMutationSideEffectsAsync("inspection.updated", inspection, context, cancellationToken)
      .ConfigureAwait(false);

    return ApplicationOperationResult<InspectionDetailDto>.Success(ToDetail(snapshot!, locale, now));
  }

  public async Task<ApplicationOperationResult<InspectionDetailDto>> StartAsync(
    Guid id,
    string? locale = null,
    CancellationToken cancellationToken = default) =>
    await MutateLifecycleAsync(
        id,
        PermissionCodes.Write(PermissionModules.Inspections),
        "inspection.started",
        locale,
        (inspection, context) =>
        {
          inspection.Start(timeProvider.GetUtcNow(), context.UserId);
          return null;
        },
        cancellationToken)
      .ConfigureAwait(false);

  public async Task<ApplicationOperationResult<InspectionDetailDto>> CompleteAsync(
    Guid id,
    InspectionLifecycleRequestDto request,
    string? locale = null,
    CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(request);

    return await MutateLifecycleAsync(
        id,
        PermissionCodes.Manage(PermissionModules.Inspections),
        "inspection.completed",
        locale,
        (inspection, context) =>
        {
          inspection.Complete(request.Notes, timeProvider.GetUtcNow(), context.UserId);
          return null;
        },
        cancellationToken)
      .ConfigureAwait(false);
  }

  public async Task<ApplicationOperationResult<InspectionDetailDto>> CancelAsync(
    Guid id,
    InspectionLifecycleRequestDto request,
    string? locale = null,
    CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(request);

    return await MutateLifecycleAsync(
        id,
        PermissionCodes.Manage(PermissionModules.Inspections),
        "inspection.cancelled",
        locale,
        (inspection, context) =>
        {
          inspection.Cancel(request.Notes, timeProvider.GetUtcNow(), context.UserId);
          return null;
        },
        cancellationToken)
      .ConfigureAwait(false);
  }

  public async Task<ApplicationOperationResult> ArchiveAsync(Guid id, CancellationToken cancellationToken = default)
  {
    var validation = ValidateId(id).ToList();
    if (validation.Count > 0)
    {
      return ApplicationOperationResult.Invalid(validation);
    }

    var context = await AuthorizeContextAsync(PermissionCodes.Archive(PermissionModules.Inspections), cancellationToken)
      .ConfigureAwait(false);
    if (context is null)
    {
      return ApplicationOperationResult.Failed(ApplicationOperationFailure.Forbidden);
    }

    var inspection = await inspectionRepository.FindAsync(new EntityId(id), context.OrganizationId, false, cancellationToken)
      .ConfigureAwait(false);
    if (inspection is null)
    {
      return ApplicationOperationResult.Failed(ApplicationOperationFailure.NotFound);
    }

    inspection.Archive(timeProvider.GetUtcNow(), context.UserId);
    await inspectionRepository.UpdateAsync(inspection, cancellationToken).ConfigureAwait(false);
    await WriteMutationSideEffectsAsync("inspection.archived", inspection, context, cancellationToken)
      .ConfigureAwait(false);

    return ApplicationOperationResult.Success();
  }

  public async Task<ApplicationOperationResult<InspectionDetailDto>> RestoreAsync(
    Guid id,
    string? locale = null,
    CancellationToken cancellationToken = default) =>
    await MutateLifecycleAsync(
        id,
        PermissionCodes.Archive(PermissionModules.Inspections),
        "inspection.restored",
        locale,
        (inspection, context) =>
        {
          inspection.Restore(timeProvider.GetUtcNow(), context.UserId);
          return null;
        },
        cancellationToken,
        includeArchived: true)
      .ConfigureAwait(false);

  public async Task<ApplicationOperationResult<InspectionDetailDto>> AddChecklistItemAsync(
    Guid inspectionId,
    InspectionChecklistItemRequestDto request,
    string? locale = null,
    CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(request);

    var validation = ValidateId(inspectionId, nameof(inspectionId))
      .Concat(ValidateChecklistItemRequest(request))
      .ToList();
    if (validation.Count > 0)
    {
      return ApplicationOperationResult<InspectionDetailDto>.Invalid(validation);
    }

    return await MutateInspectionAsync(
        inspectionId,
        PermissionCodes.Write(PermissionModules.Inspections),
        "inspection.checklist-updated",
        locale,
        (inspection, context) =>
        {
          if (!InspectionCatalog.TryParseConditionRating(request.ConditionRating, out var rating))
          {
            return ApplicationOperationResult<InspectionDetailDto>.Invalid(ValidateChecklistItemRequest(request).ToList());
          }

          inspection.AddChecklistItem(
            request.AreaName,
            request.ItemName,
            request.IsRequired,
            rating,
            request.Observations,
            request.SortOrder,
            timeProvider.GetUtcNow(),
            context.UserId);
          return null;
        },
        cancellationToken)
      .ConfigureAwait(false);
  }

  public async Task<ApplicationOperationResult<InspectionDetailDto>> UpdateChecklistItemAsync(
    Guid inspectionId,
    Guid checklistItemId,
    InspectionChecklistItemRequestDto request,
    string? locale = null,
    CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(request);

    var validation = ValidateId(inspectionId, nameof(inspectionId))
      .Concat(ValidateId(checklistItemId, nameof(checklistItemId)))
      .Concat(ValidateChecklistItemRequest(request))
      .ToList();
    if (validation.Count > 0)
    {
      return ApplicationOperationResult<InspectionDetailDto>.Invalid(validation);
    }

    return await MutateInspectionAsync(
        inspectionId,
        PermissionCodes.Write(PermissionModules.Inspections),
        "inspection.checklist-updated",
        locale,
        (inspection, context) =>
        {
          if (!InspectionCatalog.TryParseConditionRating(request.ConditionRating, out var rating))
          {
            return ApplicationOperationResult<InspectionDetailDto>.Invalid(ValidateChecklistItemRequest(request).ToList());
          }

          inspection.UpdateChecklistItem(
            new EntityId(checklistItemId),
            request.AreaName,
            request.ItemName,
            request.IsRequired,
            rating,
            request.Observations,
            request.SortOrder,
            timeProvider.GetUtcNow(),
            context.UserId);
          return null;
        },
        cancellationToken)
      .ConfigureAwait(false);
  }

  public async Task<ApplicationOperationResult<InspectionDetailDto>> DeleteChecklistItemAsync(
    Guid inspectionId,
    Guid checklistItemId,
    string? locale = null,
    CancellationToken cancellationToken = default)
  {
    var validation = ValidateId(inspectionId, nameof(inspectionId))
      .Concat(ValidateId(checklistItemId, nameof(checklistItemId)))
      .ToList();
    if (validation.Count > 0)
    {
      return ApplicationOperationResult<InspectionDetailDto>.Invalid(validation);
    }

    return await MutateInspectionAsync(
        inspectionId,
        PermissionCodes.Write(PermissionModules.Inspections),
        "inspection.checklist-updated",
        locale,
        (inspection, context) =>
        {
          inspection.RemoveChecklistItem(new EntityId(checklistItemId), timeProvider.GetUtcNow(), context.UserId);
          return null;
        },
        cancellationToken)
      .ConfigureAwait(false);
  }

  public async Task<ApplicationOperationResult<InspectionDetailDto>> LinkDocumentAsync(
    Guid inspectionId,
    InspectionDocumentLinkRequestDto request,
    string? locale = null,
    CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(request);

    var validation = ValidateId(inspectionId, nameof(inspectionId))
      .Concat(ValidateDocumentLinkRequest(request))
      .ToList();
    if (validation.Count > 0)
    {
      return ApplicationOperationResult<InspectionDetailDto>.Invalid(validation);
    }

    var context = await AuthorizeContextAsync(PermissionCodes.Write(PermissionModules.Inspections), cancellationToken)
      .ConfigureAwait(false);
    if (context is null)
    {
      return ApplicationOperationResult<InspectionDetailDto>.Failed(ApplicationOperationFailure.Forbidden);
    }

    var documentId = new EntityId(request.DocumentId!.Value);
    if (!await inspectionRepository.DocumentExistsAsync(documentId, context.OrganizationId, cancellationToken)
        .ConfigureAwait(false))
    {
      return ApplicationOperationResult<InspectionDetailDto>.Invalid(
        [new ValidationFailure(nameof(request.DocumentId), "validation.document")]);
    }

    var inspection = await inspectionRepository.FindAsync(
        new EntityId(inspectionId),
        context.OrganizationId,
        includeArchived: false,
        cancellationToken)
      .ConfigureAwait(false);
    if (inspection is null)
    {
      return ApplicationOperationResult<InspectionDetailDto>.Failed(ApplicationOperationFailure.NotFound);
    }

    if (!InspectionCatalog.TryParseDocumentKind(request.Kind, out var kind))
    {
      return ApplicationOperationResult<InspectionDetailDto>.Invalid(ValidateDocumentLinkRequest(request).ToList());
    }

    try
    {
      inspection.LinkDocument(
        documentId,
        kind,
        ToEntityIdOrNull(request.ChecklistItemId),
        request.Label,
        timeProvider.GetUtcNow(),
        context.UserId);
    }
    catch (InvalidOperationException)
    {
      return ApplicationOperationResult<InspectionDetailDto>.Failed(ApplicationOperationFailure.Conflict);
    }

    await inspectionRepository.UpdateAsync(inspection, cancellationToken).ConfigureAwait(false);
    var snapshot = await inspectionRepository.FindSnapshotAsync(inspection.Id, context.OrganizationId, true, cancellationToken)
      .ConfigureAwait(false);
    await WriteMutationSideEffectsAsync("inspection.document-linked", inspection, context, cancellationToken)
      .ConfigureAwait(false);

    return ApplicationOperationResult<InspectionDetailDto>.Success(
      ToDetail(snapshot!, locale, timeProvider.GetUtcNow()));
  }

  public Task<IReadOnlyList<StatusLabelDto>> GetTypeOptionsAsync(
    string? locale = null,
    CancellationToken cancellationToken = default)
  {
    cancellationToken.ThrowIfCancellationRequested();
    return Task.FromResult(InspectionCatalog.GetTypeOptions(locale));
  }

  public Task<IReadOnlyList<StatusLabelDto>> GetStatusOptionsAsync(
    string? locale = null,
    CancellationToken cancellationToken = default)
  {
    cancellationToken.ThrowIfCancellationRequested();
    return Task.FromResult(InspectionCatalog.GetStatusOptions(locale));
  }

  public Task<IReadOnlyList<StatusLabelDto>> GetConditionRatingOptionsAsync(
    string? locale = null,
    CancellationToken cancellationToken = default)
  {
    cancellationToken.ThrowIfCancellationRequested();
    return Task.FromResult(InspectionCatalog.GetConditionRatingOptions(locale));
  }

  public Task<IReadOnlyList<StatusLabelDto>> GetDocumentKindOptionsAsync(
    string? locale = null,
    CancellationToken cancellationToken = default)
  {
    cancellationToken.ThrowIfCancellationRequested();
    return Task.FromResult(InspectionCatalog.GetDocumentKindOptions(locale));
  }

  private async Task<ApplicationOperationResult<InspectionDetailDto>> MutateLifecycleAsync(
    Guid id,
    string permissionCode,
    string eventName,
    string? locale,
    Func<Inspection, ActiveOrganizationContext, ApplicationOperationResult<InspectionDetailDto>?> mutate,
    CancellationToken cancellationToken,
    bool includeArchived = false) =>
    await MutateInspectionAsync(id, permissionCode, eventName, locale, mutate, cancellationToken, includeArchived)
      .ConfigureAwait(false);

  private async Task<ApplicationOperationResult<InspectionDetailDto>> MutateInspectionAsync(
    Guid id,
    string permissionCode,
    string eventName,
    string? locale,
    Func<Inspection, ActiveOrganizationContext, ApplicationOperationResult<InspectionDetailDto>?> mutate,
    CancellationToken cancellationToken,
    bool includeArchived = false)
  {
    var validation = ValidateId(id).ToList();
    if (validation.Count > 0)
    {
      return ApplicationOperationResult<InspectionDetailDto>.Invalid(validation);
    }

    var context = await AuthorizeContextAsync(permissionCode, cancellationToken).ConfigureAwait(false);
    if (context is null)
    {
      return ApplicationOperationResult<InspectionDetailDto>.Failed(ApplicationOperationFailure.Forbidden);
    }

    var inspection = await inspectionRepository.FindAsync(new EntityId(id), context.OrganizationId, includeArchived, cancellationToken)
      .ConfigureAwait(false);
    if (inspection is null)
    {
      return ApplicationOperationResult<InspectionDetailDto>.Failed(ApplicationOperationFailure.NotFound);
    }

    try
    {
      var mutationResult = mutate(inspection, context);
      if (mutationResult is not null)
      {
        return mutationResult;
      }
    }
    catch (InvalidOperationException)
    {
      return ApplicationOperationResult<InspectionDetailDto>.Failed(ApplicationOperationFailure.Conflict);
    }

    await inspectionRepository.UpdateAsync(inspection, cancellationToken).ConfigureAwait(false);
    var snapshot = await inspectionRepository.FindSnapshotAsync(inspection.Id, context.OrganizationId, true, cancellationToken)
      .ConfigureAwait(false);
    await WriteMutationSideEffectsAsync(eventName, inspection, context, cancellationToken).ConfigureAwait(false);

    return ApplicationOperationResult<InspectionDetailDto>.Success(
      ToDetail(snapshot!, locale, timeProvider.GetUtcNow()));
  }

  private async Task<ResolvedInspectionEntities> ResolveRelatedEntitiesAsync(
    InspectionScheduleRequestDto request,
    OrganizationId organizationId,
    CancellationToken cancellationToken) =>
    await ResolveRelatedEntitiesAsync(
        request.ContractId,
        request.PropertyId,
        request.ResidentId,
        request.AssignedUserId,
        organizationId,
        cancellationToken)
      .ConfigureAwait(false);

  private async Task<ResolvedInspectionEntities> ResolveRelatedEntitiesAsync(
    InspectionUpdateRequestDto request,
    OrganizationId organizationId,
    CancellationToken cancellationToken) =>
    await ResolveRelatedEntitiesAsync(
        request.ContractId,
        request.PropertyId,
        request.ResidentId,
        request.AssignedUserId,
        organizationId,
        cancellationToken)
      .ConfigureAwait(false);

  private async Task<ResolvedInspectionEntities> ResolveRelatedEntitiesAsync(
    Guid? contractIdValue,
    Guid? propertyIdValue,
    Guid? residentIdValue,
    Guid? assignedUserIdValue,
    OrganizationId organizationId,
    CancellationToken cancellationToken)
  {
    var errors = new List<ValidationFailure>();
    InspectionContractSnapshot? contract = null;
    InspectionPropertySnapshot? property = null;
    InspectionResidentSnapshot? resident = null;
    InspectionUserSnapshot? assignee = null;
    var contractId = ToEntityIdOrNull(contractIdValue);
    var propertyId = ToEntityIdOrNull(propertyIdValue);
    var residentId = ToEntityIdOrNull(residentIdValue);
    var assignedUserId = ToUserIdOrNull(assignedUserIdValue);

    if (contractId.HasValue)
    {
      contract = await inspectionRepository.GetContractSnapshotAsync(contractId.Value, organizationId, cancellationToken)
        .ConfigureAwait(false);
      if (contract is null || contract.IsArchived)
      {
        errors.Add(new ValidationFailure("contractId", "validation.contract"));
      }
      else
      {
        if (propertyId.HasValue && propertyId.Value != contract.PropertyId)
        {
          errors.Add(new ValidationFailure("propertyId", "validation.propertyContractMismatch"));
        }

        if (residentId.HasValue && !contract.ResidentIds.Contains(residentId.Value))
        {
          errors.Add(new ValidationFailure("residentId", "validation.residentContractMismatch"));
        }

        propertyId ??= contract.PropertyId;
        residentId ??= contract.PrimaryResidentId;
      }
    }

    if (propertyId.HasValue)
    {
      property = await inspectionRepository.GetPropertySnapshotAsync(propertyId.Value, organizationId, cancellationToken)
        .ConfigureAwait(false);
      if (property is null)
      {
        errors.Add(new ValidationFailure("propertyId", "validation.property"));
      }
    }

    if (residentId.HasValue)
    {
      resident = await inspectionRepository.GetResidentSnapshotAsync(residentId.Value, organizationId, cancellationToken)
        .ConfigureAwait(false);
      if (resident is null)
      {
        errors.Add(new ValidationFailure("residentId", "validation.resident"));
      }
    }

    if (assignedUserId.HasValue)
    {
      assignee = await inspectionRepository.GetAssigneeSnapshotAsync(assignedUserId.Value, organizationId, cancellationToken)
        .ConfigureAwait(false);
      if (assignee is null)
      {
        errors.Add(new ValidationFailure("assignedUserId", "validation.assignee"));
      }
    }

    return new ResolvedInspectionEntities(
      errors,
      contractId,
      propertyId,
      residentId,
      assignedUserId,
      property,
      contract,
      resident,
      assignee);
  }

  private async Task<ActiveOrganizationContext?> AuthorizeContextAsync(
    string permissionCode,
    CancellationToken cancellationToken)
  {
    var permission = await permissionService.AuthorizeAsync(permissionCode, cancellationToken)
      .ConfigureAwait(false);
    if (!permission.IsGranted)
    {
      return null;
    }

    var context = await activeOrganizationContextResolver.ResolveAsync(cancellationToken)
      .ConfigureAwait(false);
    return context.Succeeded ? context.Context : null;
  }

  private async Task WriteMutationSideEffectsAsync(
    string eventName,
    Inspection inspection,
    ActiveOrganizationContext context,
    CancellationToken cancellationToken)
  {
    var subject = EntityReference.FromGuid("inspection", inspection.Id.Value, inspection.Title);
    var actor = EventActor.User(context.UserId, context.User.DisplayName);
    var data = new Dictionary<string, string>(StringComparer.Ordinal)
    {
      ["type"] = InspectionCatalog.ToTypeLabel(inspection.Type).Code,
      ["status"] = InspectionCatalog.ToStatusLabel(inspection.Status).Code,
      ["scheduledAt"] = inspection.ScheduledAt.ToString("O", CultureInfo.InvariantCulture),
      ["progress"] = inspection.CompletionPercentage.ToString(CultureInfo.InvariantCulture),
      ["propertyId"] = inspection.PropertyId.Value.ToString("D"),
      ["assignedUserId"] = inspection.AssignedUserId.Value.ToString("D")
    };

    if (inspection.CompletedAt.HasValue)
    {
      data["completedAt"] = inspection.CompletedAt.Value.ToString("O", CultureInfo.InvariantCulture);
    }

    var related = new List<EntityReference>
    {
      EntityReference.FromGuid("property", inspection.PropertyId.Value),
      EntityReference.FromGuid("user", inspection.AssignedUserId.Value)
    };
    if (inspection.ContractId.HasValue)
    {
      related.Add(EntityReference.FromGuid("contract", inspection.ContractId.Value.Value));
    }

    if (inspection.ResidentId.HasValue)
    {
      related.Add(EntityReference.FromGuid("resident", inspection.ResidentId.Value.Value));
    }

    var envelope = ModuleEventEnvelope.Create(
      context.OrganizationId,
      "inspections",
      eventName,
      timeProvider.GetUtcNow(),
      actor,
      subject,
      ModuleEventConsumer.Audit |
      ModuleEventConsumer.Timeline |
      ModuleEventConsumer.Notifications |
      ModuleEventConsumer.DashboardProjection,
      data,
      related);

    await auditWriter.WriteAsync(AuditEntryDraft.FromModuleEvent(envelope), cancellationToken)
      .ConfigureAwait(false);
    await outboxWriter.EnqueueAsync(envelope, cancellationToken).ConfigureAwait(false);
  }

  private static InspectionListItemDto ToListItem(
    InspectionSnapshot snapshot,
    string? locale,
    DateTimeOffset now)
  {
    var inspection = snapshot.Inspection;
    var effectiveStatus = inspection.IsDeleted ? InspectionStatus.Archived : inspection.Status;

    return new InspectionListItemDto(
      inspection.Id.Value,
      inspection.Title,
      InspectionCatalog.ToTypeLabel(inspection.Type, locale),
      InspectionCatalog.ToStatusLabel(effectiveStatus, locale),
      ToPropertySummary(snapshot.Property),
      ToContractSummary(snapshot.Contract),
      ToResidentSummary(snapshot.Resident),
      ToAssigneeSummary(snapshot.Assignee),
      inspection.ScheduledAt,
      inspection.StartedAt,
      inspection.CompletedAt,
      ToProgress(inspection),
      IsPending(inspection, now),
      inspection.IsDeleted || effectiveStatus == InspectionStatus.Archived,
      inspection.CreatedAt,
      inspection.UpdatedAt,
      inspection.ConcurrencyToken.Value);
  }

  private static InspectionDetailDto ToDetail(
    InspectionSnapshot snapshot,
    string? locale,
    DateTimeOffset now)
  {
    var listItem = ToListItem(snapshot, locale, now);
    var inspection = snapshot.Inspection;
    var id = Uri.EscapeDataString(inspection.Id.Value.ToString("D"));
    var checklistItems = inspection.ChecklistItems
      .Where(item => !item.IsDeleted)
      .OrderBy(item => item.SortOrder)
      .ThenBy(item => item.AreaName, StringComparer.Ordinal)
      .ThenBy(item => item.ItemName, StringComparer.Ordinal)
      .Select(item => ToChecklistItemDto(item, locale))
      .ToArray();
    var documents = snapshot.Documents
      .OrderBy(document => document.Kind)
      .ThenBy(document => document.Label, StringComparer.Ordinal)
      .Select(document => ToDocumentDto(document, locale, id))
      .ToArray();
    var photos = documents.Where(document => document.Kind.Code == "photo").ToArray();
    var linkedDocuments = documents.Where(document => document.Kind.Code != "photo").ToArray();
    var signatureSlots = inspection.SignatureSlots
      .Where(slot => !slot.IsDeleted)
      .OrderBy(slot => slot.SignerRole, StringComparer.Ordinal)
      .Select(ToSignatureSlotDto)
      .ToArray();

    return new InspectionDetailDto(
      listItem.Id,
      listItem.Title,
      listItem.Type,
      listItem.Status,
      listItem.Property,
      listItem.Contract,
      listItem.Resident,
      listItem.Assignee,
      listItem.ScheduledAt,
      listItem.StartedAt,
      listItem.CompletedAt,
      inspection.CancelledAt,
      inspection.CompletionNotes,
      inspection.CancellationReason,
      inspection.Notes,
      listItem.Progress,
      checklistItems,
      photos,
      linkedDocuments,
      signatureSlots,
      $"/timeline?entityType=inspection&entityId={id}",
      $"/auditoria?entityType=inspection&entityId={id}",
      inspection.CreatedAt,
      inspection.UpdatedAt,
      inspection.DeletedAt,
      inspection.ConcurrencyToken.Value);
  }

  private static InspectionChecklistItemDto ToChecklistItemDto(
    InspectionChecklistItem item,
    string? locale) =>
    new(
      item.Id.Value,
      item.AreaName,
      item.ItemName,
      item.IsRequired,
      InspectionCatalog.ToConditionRatingLabel(item.ConditionRating, locale),
      item.Observations,
      item.SortOrder,
      item.IsComplete,
      item.CreatedAt,
      item.UpdatedAt);

  private static InspectionDocumentDto ToDocumentDto(
    InspectionDocumentSnapshot document,
    string? locale,
    string inspectionId) =>
    new(
      document.DocumentId.Value,
      document.ChecklistItemId?.Value,
      InspectionCatalog.ToDocumentKindLabel(document.Kind, locale),
      document.Label,
      $"/documentos?entityType=inspection&entityId={inspectionId}");

  private static InspectionSignatureSlotDto ToSignatureSlotDto(InspectionSignatureSlot slot) =>
    new(
      slot.Id.Value,
      slot.SignerRole,
      slot.SignerName,
      slot.IsRequired,
      slot.IsSigned,
      slot.SignedAt,
      slot.SignatureDocumentId?.Value,
      slot.Notes);

  private static InspectionEntitySummaryDto ToPropertySummary(InspectionPropertySnapshot property) =>
    new(
      property.PropertyId.Value,
      property.Name,
      property.Location,
      $"/imoveis?id={property.PropertyId.Value:D}");

  private static InspectionEntitySummaryDto? ToContractSummary(InspectionContractSnapshot? contract) =>
    contract is null
      ? null
      : new InspectionEntitySummaryDto(
        contract.ContractId.Value,
        contract.DisplayName,
        $"{contract.PropertyName} - {contract.ResidentName}",
        $"/contratos?id={contract.ContractId.Value:D}");

  private static InspectionEntitySummaryDto? ToResidentSummary(InspectionResidentSnapshot? resident) =>
    resident is null
      ? null
      : new InspectionEntitySummaryDto(
        resident.ResidentId.Value,
        resident.Name,
        Route: $"/moradores?id={resident.ResidentId.Value:D}");

  private static InspectionEntitySummaryDto ToAssigneeSummary(InspectionUserSnapshot assignee) =>
    new(assignee.UserId.Value, assignee.Name, assignee.Email, $"/administradores?id={assignee.UserId.Value:D}");

  private static InspectionProgressDto ToProgress(Inspection inspection) =>
    new(
      inspection.ActiveChecklistItemCount,
      inspection.CompletedChecklistItemCount,
      inspection.CompletionPercentage);

  private static bool IsPending(Inspection inspection, DateTimeOffset now) =>
    !inspection.IsDeleted &&
    inspection.Status is InspectionStatus.Scheduled or InspectionStatus.InProgress &&
    inspection.ScheduledAt <= now.AddDays(7);

  private static IEnumerable<ValidationFailure> ValidateScheduleRequest(InspectionScheduleRequestDto request) =>
    ValidateScheduleFields(
      request.Type,
      request.PropertyId,
      request.ScheduledAt,
      request.AssignedUserId,
      request.Title,
      request.Notes);

  private static IEnumerable<ValidationFailure> ValidateUpdateRequest(InspectionUpdateRequestDto request) =>
    ValidateScheduleFields(
      request.Type,
      request.PropertyId,
      request.ScheduledAt,
      request.AssignedUserId,
      request.Title,
      request.Notes);

  private static IEnumerable<ValidationFailure> ValidateScheduleFields(
    string type,
    Guid? propertyId,
    DateTimeOffset scheduledAt,
    Guid? assignedUserId,
    string? title,
    string? notes)
  {
    if (!InspectionCatalog.TryParseType(type, out _))
    {
      yield return new ValidationFailure(nameof(type), "validation.inspectionType");
    }

    if (!HasNonEmptyId(propertyId))
    {
      yield return new ValidationFailure(nameof(propertyId), "validation.property");
    }

    if (scheduledAt == default)
    {
      yield return new ValidationFailure(nameof(scheduledAt), ValidationMessageKeys.Required);
    }

    if (!HasNonEmptyId(assignedUserId))
    {
      yield return new ValidationFailure(nameof(assignedUserId), "validation.assignee");
    }

    foreach (var failure in ValidateOptionalMaxLengths(
      (nameof(title), title, 200),
      (nameof(notes), notes, 2000)))
    {
      yield return failure;
    }
  }

  private static IEnumerable<ValidationFailure> ValidateChecklistItemRequest(
    InspectionChecklistItemRequestDto request)
  {
    if (string.IsNullOrWhiteSpace(request.AreaName))
    {
      yield return new ValidationFailure(nameof(request.AreaName), ValidationMessageKeys.Required);
    }
    else if (ExceedsMaxLength(request.AreaName, 160))
    {
      yield return new ValidationFailure(nameof(request.AreaName), ValidationMessageKeys.MaxLength);
    }

    if (string.IsNullOrWhiteSpace(request.ItemName))
    {
      yield return new ValidationFailure(nameof(request.ItemName), ValidationMessageKeys.Required);
    }
    else if (ExceedsMaxLength(request.ItemName, 200))
    {
      yield return new ValidationFailure(nameof(request.ItemName), ValidationMessageKeys.MaxLength);
    }

    if (!InspectionCatalog.TryParseConditionRating(request.ConditionRating, out _))
    {
      yield return new ValidationFailure(nameof(request.ConditionRating), "validation.inspectionConditionRating");
    }

    if (request.SortOrder < 0)
    {
      yield return new ValidationFailure(nameof(request.SortOrder), ValidationMessageKeys.MinValue);
    }

    if (ExceedsMaxLength(request.Observations, 2000))
    {
      yield return new ValidationFailure(nameof(request.Observations), ValidationMessageKeys.MaxLength);
    }
  }

  private static IEnumerable<ValidationFailure> ValidateDocumentLinkRequest(
    InspectionDocumentLinkRequestDto request)
  {
    if (!HasNonEmptyId(request.DocumentId))
    {
      yield return new ValidationFailure(nameof(request.DocumentId), "validation.document");
    }

    if (!InspectionCatalog.TryParseDocumentKind(request.Kind, out _))
    {
      yield return new ValidationFailure(nameof(request.Kind), "validation.inspectionDocumentKind");
    }

    if (ExceedsMaxLength(request.Label, 160))
    {
      yield return new ValidationFailure(nameof(request.Label), ValidationMessageKeys.MaxLength);
    }
  }

  private static IEnumerable<ValidationFailure> ValidateSignatureSlots(
    IReadOnlyList<InspectionSignatureSlotRequestDto>? slots)
  {
    if (slots is null)
    {
      yield break;
    }

    for (var index = 0; index < slots.Count; index++)
    {
      var slot = slots[index];
      if (string.IsNullOrWhiteSpace(slot.SignerRole))
      {
        yield return new ValidationFailure($"signatureSlots[{index}].signerRole", ValidationMessageKeys.Required);
      }
      else if (ExceedsMaxLength(slot.SignerRole, 120))
      {
        yield return new ValidationFailure($"signatureSlots[{index}].signerRole", ValidationMessageKeys.MaxLength);
      }

      if (ExceedsMaxLength(slot.SignerName, 160))
      {
        yield return new ValidationFailure($"signatureSlots[{index}].signerName", ValidationMessageKeys.MaxLength);
      }
    }
  }

  private static IEnumerable<ValidationFailure> ValidateOptionalMaxLengths(
    params (string Name, string? Value, int MaxLength)[] fields)
  {
    foreach (var (name, value, maxLength) in fields)
    {
      if (ExceedsMaxLength(value, maxLength))
      {
        yield return new ValidationFailure(name, ValidationMessageKeys.MaxLength);
      }
    }
  }

  private static IEnumerable<ValidationFailure> ValidateId(Guid id, string propertyName = "id")
  {
    if (id == Guid.Empty)
    {
      yield return new ValidationFailure(propertyName, ValidationMessageKeys.Required);
    }
  }

  private static InspectionSignatureSlotDraft ToSignatureSlotDraft(InspectionSignatureSlotRequestDto slot) =>
    new(slot.SignerRole, slot.SignerName, slot.IsRequired);

  private static EntityId? ToEntityIdOrNull(Guid? id) =>
    id.HasValue && id.Value != Guid.Empty ? new EntityId(id.Value) : null;

  private static UserId? ToUserIdOrNull(Guid? id) =>
    id.HasValue && id.Value != Guid.Empty ? new UserId(id.Value) : null;

  private static bool HasNonEmptyId(Guid? id) => id.HasValue && id.Value != Guid.Empty;

  private static bool ExceedsMaxLength(string? value, int maxLength) =>
    !string.IsNullOrWhiteSpace(value) && value.Trim().Length > maxLength;

  private sealed record ResolvedInspectionEntities(
    IReadOnlyList<ValidationFailure> Errors,
    EntityId? ContractId,
    EntityId? PropertyId,
    EntityId? ResidentId,
    UserId? AssignedUserId,
    InspectionPropertySnapshot? Property,
    InspectionContractSnapshot? Contract,
    InspectionResidentSnapshot? Resident,
    InspectionUserSnapshot? Assignee);
}
