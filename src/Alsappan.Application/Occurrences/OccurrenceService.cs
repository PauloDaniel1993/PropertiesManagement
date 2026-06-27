using System.Globalization;
using Alsappan.Application.Common.Audit;
using Alsappan.Application.Common.Authorization;
using Alsappan.Application.Common.Contracts;
using Alsappan.Application.Common.Events;
using Alsappan.Application.Common.Results;
using Alsappan.Application.Common.Tenancy;
using Alsappan.Application.Common.Validation;
using Alsappan.Application.Occurrences.Repositories;
using Alsappan.Domain.Common.Events;
using Alsappan.Domain.Common.Identifiers;
using Alsappan.Domain.Occurrences;

namespace Alsappan.Application.Occurrences;

public sealed class OccurrenceService : IOccurrenceService
{
  private readonly IOccurrenceRepository occurrenceRepository;
  private readonly IPermissionService permissionService;
  private readonly IActiveOrganizationContextResolver activeOrganizationContextResolver;
  private readonly IAuditWriter auditWriter;
  private readonly IModuleEventOutboxWriter outboxWriter;
  private readonly TimeProvider timeProvider;

  public OccurrenceService(
    IOccurrenceRepository occurrenceRepository,
    IPermissionService permissionService,
    IActiveOrganizationContextResolver activeOrganizationContextResolver,
    IAuditWriter auditWriter,
    IModuleEventOutboxWriter outboxWriter,
    TimeProvider timeProvider)
  {
    this.occurrenceRepository = occurrenceRepository ?? throw new ArgumentNullException(nameof(occurrenceRepository));
    this.permissionService = permissionService ?? throw new ArgumentNullException(nameof(permissionService));
    this.activeOrganizationContextResolver = activeOrganizationContextResolver ??
      throw new ArgumentNullException(nameof(activeOrganizationContextResolver));
    this.auditWriter = auditWriter ?? throw new ArgumentNullException(nameof(auditWriter));
    this.outboxWriter = outboxWriter ?? throw new ArgumentNullException(nameof(outboxWriter));
    this.timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
  }

  public async Task<ApplicationOperationResult<PagedResultDto<OccurrenceListItemDto>>> ListAsync(
    OccurrenceListRequestDto request,
    CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(request);

    var context = await AuthorizeContextAsync(PermissionCodes.Read(PermissionModules.Occurrences), cancellationToken)
      .ConfigureAwait(false);
    if (context is null)
    {
      return ApplicationOperationResult<PagedResultDto<OccurrenceListItemDto>>.Failed(
        ApplicationOperationFailure.Forbidden);
    }

    var page = await occurrenceRepository.ListAsync(request, context.OrganizationId, cancellationToken)
      .ConfigureAwait(false);
    var items = page.Items.Select(snapshot => ToListItem(snapshot, request.Locale)).ToArray();

    return ApplicationOperationResult<PagedResultDto<OccurrenceListItemDto>>.Success(
      new PagedResultDto<OccurrenceListItemDto>(items, page.Page, page.PageSize, page.TotalItems));
  }

  public async Task<ApplicationOperationResult<OccurrenceDetailDto>> GetAsync(
    Guid id,
    string? locale = null,
    CancellationToken cancellationToken = default)
  {
    var validation = ValidateId(id).ToList();
    if (validation.Count > 0)
    {
      return ApplicationOperationResult<OccurrenceDetailDto>.Invalid(validation);
    }

    var context = await AuthorizeContextAsync(PermissionCodes.Read(PermissionModules.Occurrences), cancellationToken)
      .ConfigureAwait(false);
    if (context is null)
    {
      return ApplicationOperationResult<OccurrenceDetailDto>.Failed(ApplicationOperationFailure.Forbidden);
    }

    var snapshot = await occurrenceRepository.FindSnapshotAsync(
        new EntityId(id),
        context.OrganizationId,
        includeArchived: true,
        cancellationToken)
      .ConfigureAwait(false);

    return snapshot is null
      ? ApplicationOperationResult<OccurrenceDetailDto>.Failed(ApplicationOperationFailure.NotFound)
      : ApplicationOperationResult<OccurrenceDetailDto>.Success(ToDetail(snapshot, locale));
  }

  public async Task<ApplicationOperationResult<OccurrenceDetailDto>> CreateAsync(
    OccurrenceCreateRequestDto request,
    string? locale = null,
    CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(request);

    var validation = ValidateCreateRequest(request).ToList();
    if (validation.Count > 0)
    {
      return ApplicationOperationResult<OccurrenceDetailDto>.Invalid(validation);
    }

    var context = await AuthorizeContextAsync(PermissionCodes.Write(PermissionModules.Occurrences), cancellationToken)
      .ConfigureAwait(false);
    if (context is null)
    {
      return ApplicationOperationResult<OccurrenceDetailDto>.Failed(ApplicationOperationFailure.Forbidden);
    }

    var related = await ResolveRelatedEntitiesAsync(
        request.ContractId,
        request.PropertyId,
        request.ResidentId,
        context.OrganizationId,
        cancellationToken)
      .ConfigureAwait(false);
    validation.AddRange(related.Errors);

    var assignedUser = await ResolveAssignedUserAsync(
        request.AssignedUserId,
        context.OrganizationId,
        validation,
        cancellationToken)
      .ConfigureAwait(false);

    if (validation.Count > 0)
    {
      return ApplicationOperationResult<OccurrenceDetailDto>.Invalid(validation);
    }

    if (!OccurrenceCatalog.TryParseType(request.Type, out var type) ||
      !OccurrenceCatalog.TryParsePriority(request.Priority, out var priority))
    {
      return ApplicationOperationResult<OccurrenceDetailDto>.Invalid(ValidateCreateRequest(request).ToList());
    }

    var now = timeProvider.GetUtcNow();
    var occurrence = Occurrence.Create(
      EntityId.New(),
      context.OrganizationId,
      request.Title,
      request.Description,
      type,
      priority,
      related.PropertyId,
      related.ResidentId,
      related.ContractId,
      assignedUser?.UserId,
      request.DueDate,
      related.Property?.Name,
      related.Resident?.Name,
      related.Contract?.DisplayName,
      assignedUser?.DisplayName,
      now,
      context.UserId);

    await occurrenceRepository.AddAsync(occurrence, cancellationToken).ConfigureAwait(false);
    var snapshot = await occurrenceRepository.FindSnapshotAsync(occurrence.Id, context.OrganizationId, true, cancellationToken)
      .ConfigureAwait(false);
    await WriteMutationSideEffectsAsync("occurrence.created", occurrence, context, cancellationToken)
      .ConfigureAwait(false);

    return ApplicationOperationResult<OccurrenceDetailDto>.Success(ToDetail(snapshot!, locale));
  }

  public async Task<ApplicationOperationResult<OccurrenceDetailDto>> UpdateAsync(
    Guid id,
    OccurrenceUpdateRequestDto request,
    string? locale = null,
    CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(request);

    var validation = ValidateId(id).Concat(ValidateUpdateRequest(request)).ToList();
    if (validation.Count > 0)
    {
      return ApplicationOperationResult<OccurrenceDetailDto>.Invalid(validation);
    }

    var context = await AuthorizeContextAsync(PermissionCodes.Write(PermissionModules.Occurrences), cancellationToken)
      .ConfigureAwait(false);
    if (context is null)
    {
      return ApplicationOperationResult<OccurrenceDetailDto>.Failed(ApplicationOperationFailure.Forbidden);
    }

    var occurrence = await occurrenceRepository.FindAsync(new EntityId(id), context.OrganizationId, false, cancellationToken)
      .ConfigureAwait(false);
    if (occurrence is null)
    {
      return ApplicationOperationResult<OccurrenceDetailDto>.Failed(ApplicationOperationFailure.NotFound);
    }

    if (ConcurrencyMismatch(occurrence, request.ConcurrencyToken))
    {
      return ApplicationOperationResult<OccurrenceDetailDto>.Failed(ApplicationOperationFailure.Conflict);
    }

    var related = await ResolveRelatedEntitiesAsync(
        request.ContractId,
        request.PropertyId,
        request.ResidentId,
        context.OrganizationId,
        cancellationToken)
      .ConfigureAwait(false);
    validation.AddRange(related.Errors);
    if (validation.Count > 0)
    {
      return ApplicationOperationResult<OccurrenceDetailDto>.Invalid(validation);
    }

    if (!OccurrenceCatalog.TryParseType(request.Type, out var type))
    {
      return ApplicationOperationResult<OccurrenceDetailDto>.Invalid(ValidateUpdateRequest(request).ToList());
    }

    var assignedUser = occurrence.AssignedUserId.HasValue
      ? await occurrenceRepository.GetAssignableUserSnapshotAsync(
          occurrence.AssignedUserId.Value,
          context.OrganizationId,
          cancellationToken)
        .ConfigureAwait(false)
      : null;

    try
    {
      occurrence.UpdateDetails(
        request.Title,
        request.Description,
        type,
        related.PropertyId,
        related.ResidentId,
        related.ContractId,
        request.DueDate,
        related.Property?.Name,
        related.Resident?.Name,
        related.Contract?.DisplayName,
        assignedUser?.DisplayName,
        timeProvider.GetUtcNow(),
        context.UserId);
    }
    catch (InvalidOperationException)
    {
      return ApplicationOperationResult<OccurrenceDetailDto>.Failed(ApplicationOperationFailure.Conflict);
    }

    await occurrenceRepository.UpdateAsync(occurrence, cancellationToken).ConfigureAwait(false);
    var snapshot = await occurrenceRepository.FindSnapshotAsync(occurrence.Id, context.OrganizationId, true, cancellationToken)
      .ConfigureAwait(false);
    await WriteMutationSideEffectsAsync("occurrence.updated", occurrence, context, cancellationToken)
      .ConfigureAwait(false);

    return ApplicationOperationResult<OccurrenceDetailDto>.Success(ToDetail(snapshot!, locale));
  }

  public async Task<ApplicationOperationResult<OccurrenceDetailDto>> AssignAsync(
    Guid id,
    OccurrenceAssignmentRequestDto request,
    string? locale = null,
    CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(request);

    return await MutateAsync(
        id,
        PermissionCodes.Manage(PermissionModules.Occurrences),
        request.AssignedUserId.HasValue ? "occurrence.assigned" : "occurrence.unassigned",
        locale,
        async (occurrence, context, validation, token) =>
        {
          var assignedUser = await ResolveAssignedUserAsync(
              request.AssignedUserId,
              context.OrganizationId,
              validation,
              token)
            .ConfigureAwait(false);
          if (validation.Count > 0)
          {
            return ApplicationOperationResult<OccurrenceDetailDto>.Invalid(validation);
          }

          occurrence.Assign(
            assignedUser?.UserId,
            assignedUser?.DisplayName,
            request.Notes,
            timeProvider.GetUtcNow(),
            context.UserId);
          return null;
        },
        cancellationToken)
      .ConfigureAwait(false);
  }

  public async Task<ApplicationOperationResult<OccurrenceDetailDto>> ChangePriorityAsync(
    Guid id,
    OccurrencePriorityChangeRequestDto request,
    string? locale = null,
    CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(request);

    return await MutateAsync(
        id,
        PermissionCodes.Manage(PermissionModules.Occurrences),
        "occurrence.priority-changed",
        locale,
        (occurrence, context, validation, token) =>
        {
          token.ThrowIfCancellationRequested();
          if (!OccurrenceCatalog.TryParsePriority(request.Priority, out var priority))
          {
            validation.Add(new ValidationFailure(nameof(request.Priority), "validation.occurrencePriority"));
            return Task.FromResult<ApplicationOperationResult<OccurrenceDetailDto>?>(
              ApplicationOperationResult<OccurrenceDetailDto>.Invalid(validation));
          }

          occurrence.ChangePriority(priority, request.Notes, timeProvider.GetUtcNow(), context.UserId);
          return Task.FromResult<ApplicationOperationResult<OccurrenceDetailDto>?>(null);
        },
        cancellationToken)
      .ConfigureAwait(false);
  }

  public async Task<ApplicationOperationResult<OccurrenceDetailDto>> ChangeStatusAsync(
    Guid id,
    OccurrenceStatusChangeRequestDto request,
    string? locale = null,
    CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(request);

    return await MutateAsync(
        id,
        PermissionCodes.Manage(PermissionModules.Occurrences),
        "occurrence.status-changed",
        locale,
        (occurrence, context, validation, token) =>
        {
          token.ThrowIfCancellationRequested();
          if (!OccurrenceCatalog.TryParseStatus(request.Status, out var status) ||
            status is OccurrenceStatus.Archived or OccurrenceStatus.Resolved or OccurrenceStatus.Cancelled)
          {
            validation.Add(new ValidationFailure(nameof(request.Status), "validation.occurrenceStatus"));
            return Task.FromResult<ApplicationOperationResult<OccurrenceDetailDto>?>(
              ApplicationOperationResult<OccurrenceDetailDto>.Invalid(validation));
          }

          occurrence.ChangeStatus(status, request.Notes, timeProvider.GetUtcNow(), context.UserId);
          return Task.FromResult<ApplicationOperationResult<OccurrenceDetailDto>?>(null);
        },
        cancellationToken)
      .ConfigureAwait(false);
  }

  public async Task<ApplicationOperationResult<OccurrenceDetailDto>> ResolveAsync(
    Guid id,
    OccurrenceResolutionRequestDto request,
    string? locale = null,
    CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(request);
    if (string.IsNullOrWhiteSpace(request.ResolutionNotes))
    {
      return ApplicationOperationResult<OccurrenceDetailDto>.Invalid(
        [new ValidationFailure(nameof(request.ResolutionNotes), ValidationMessageKeys.Required)]);
    }

    return await MutateAsync(
        id,
        PermissionCodes.Manage(PermissionModules.Occurrences),
        "occurrence.resolved",
        locale,
        (occurrence, context, validation, token) =>
        {
          token.ThrowIfCancellationRequested();
          occurrence.Resolve(request.ResolutionNotes, timeProvider.GetUtcNow(), context.UserId);
          return Task.FromResult<ApplicationOperationResult<OccurrenceDetailDto>?>(null);
        },
        cancellationToken)
      .ConfigureAwait(false);
  }

  public async Task<ApplicationOperationResult<OccurrenceDetailDto>> CancelAsync(
    Guid id,
    OccurrenceLifecycleRequestDto request,
    string? locale = null,
    CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(request);

    return await MutateAsync(
        id,
        PermissionCodes.Manage(PermissionModules.Occurrences),
        "occurrence.cancelled",
        locale,
        (occurrence, context, validation, token) =>
        {
          token.ThrowIfCancellationRequested();
          occurrence.Cancel(request.Notes, timeProvider.GetUtcNow(), context.UserId);
          return Task.FromResult<ApplicationOperationResult<OccurrenceDetailDto>?>(null);
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

    var context = await AuthorizeContextAsync(PermissionCodes.Archive(PermissionModules.Occurrences), cancellationToken)
      .ConfigureAwait(false);
    if (context is null)
    {
      return ApplicationOperationResult.Failed(ApplicationOperationFailure.Forbidden);
    }

    var occurrence = await occurrenceRepository.FindAsync(new EntityId(id), context.OrganizationId, false, cancellationToken)
      .ConfigureAwait(false);
    if (occurrence is null)
    {
      return ApplicationOperationResult.Failed(ApplicationOperationFailure.NotFound);
    }

    occurrence.Archive(timeProvider.GetUtcNow(), context.UserId);
    await occurrenceRepository.UpdateAsync(occurrence, cancellationToken).ConfigureAwait(false);
    await WriteMutationSideEffectsAsync("occurrence.archived", occurrence, context, cancellationToken)
      .ConfigureAwait(false);

    return ApplicationOperationResult.Success();
  }

  public async Task<ApplicationOperationResult<OccurrenceDetailDto>> RestoreAsync(
    Guid id,
    string? locale = null,
    CancellationToken cancellationToken = default) =>
    await MutateAsync(
        id,
        PermissionCodes.Archive(PermissionModules.Occurrences),
        "occurrence.restored",
        locale,
        (occurrence, context, validation, token) =>
        {
          token.ThrowIfCancellationRequested();
          occurrence.Restore(timeProvider.GetUtcNow(), context.UserId);
          return Task.FromResult<ApplicationOperationResult<OccurrenceDetailDto>?>(null);
        },
        cancellationToken,
        includeArchived: true)
      .ConfigureAwait(false);

  public async Task<ApplicationOperationResult<OccurrenceDetailDto>> AddCommentAsync(
    Guid id,
    OccurrenceCommentRequestDto request,
    string? locale = null,
    CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(request);
    if (string.IsNullOrWhiteSpace(request.Body))
    {
      return ApplicationOperationResult<OccurrenceDetailDto>.Invalid(
        [new ValidationFailure(nameof(request.Body), ValidationMessageKeys.Required)]);
    }

    return await MutateAsync(
        id,
        PermissionCodes.Write(PermissionModules.Occurrences),
        "occurrence.comment-added",
        locale,
        (occurrence, context, validation, token) =>
        {
          token.ThrowIfCancellationRequested();
          occurrence.AddComment(request.Body, request.IsInternal, timeProvider.GetUtcNow(), context.UserId);
          return Task.FromResult<ApplicationOperationResult<OccurrenceDetailDto>?>(null);
        },
        cancellationToken)
      .ConfigureAwait(false);
  }

  public async Task<ApplicationOperationResult<OccurrenceDetailDto>> AttachDocumentAsync(
    Guid id,
    OccurrenceAttachmentRequestDto request,
    string? locale = null,
    CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(request);

    if (request.DocumentId == Guid.Empty)
    {
      return ApplicationOperationResult<OccurrenceDetailDto>.Invalid(
        [new ValidationFailure(nameof(request.DocumentId), ValidationMessageKeys.InvalidId)]);
    }

    return await MutateAsync(
        id,
        PermissionCodes.Write(PermissionModules.Occurrences),
        "occurrence.document-linked",
        locale,
        async (occurrence, context, validation, token) =>
        {
          if (!await occurrenceRepository.DocumentExistsAsync(
              new EntityId(request.DocumentId),
              context.OrganizationId,
              token).ConfigureAwait(false))
          {
            validation.Add(new ValidationFailure(nameof(request.DocumentId), "validation.document"));
            return ApplicationOperationResult<OccurrenceDetailDto>.Invalid(validation);
          }

          occurrence.LinkDocument(
            new EntityId(request.DocumentId),
            request.Label,
            timeProvider.GetUtcNow(),
            context.UserId);
          return null;
        },
        cancellationToken)
      .ConfigureAwait(false);
  }

  public Task<IReadOnlyList<StatusLabelDto>> GetTypeOptionsAsync(
    string? locale = null,
    CancellationToken cancellationToken = default)
  {
    cancellationToken.ThrowIfCancellationRequested();
    return Task.FromResult(OccurrenceCatalog.GetTypeOptions(locale));
  }

  public Task<IReadOnlyList<StatusLabelDto>> GetPriorityOptionsAsync(
    string? locale = null,
    CancellationToken cancellationToken = default)
  {
    cancellationToken.ThrowIfCancellationRequested();
    return Task.FromResult(OccurrenceCatalog.GetPriorityOptions(locale));
  }

  public Task<IReadOnlyList<StatusLabelDto>> GetStatusOptionsAsync(
    string? locale = null,
    CancellationToken cancellationToken = default)
  {
    cancellationToken.ThrowIfCancellationRequested();
    return Task.FromResult(OccurrenceCatalog.GetStatusOptions(locale));
  }

  private async Task<ApplicationOperationResult<OccurrenceDetailDto>> MutateAsync(
    Guid id,
    string permissionCode,
    string eventName,
    string? locale,
    Func<Occurrence, ActiveOrganizationContext, List<ValidationFailure>, CancellationToken,
      Task<ApplicationOperationResult<OccurrenceDetailDto>?>> mutate,
    CancellationToken cancellationToken,
    bool includeArchived = false)
  {
    var validation = ValidateId(id).ToList();
    if (validation.Count > 0)
    {
      return ApplicationOperationResult<OccurrenceDetailDto>.Invalid(validation);
    }

    var context = await AuthorizeContextAsync(permissionCode, cancellationToken).ConfigureAwait(false);
    if (context is null)
    {
      return ApplicationOperationResult<OccurrenceDetailDto>.Failed(ApplicationOperationFailure.Forbidden);
    }

    var occurrence = await occurrenceRepository.FindAsync(new EntityId(id), context.OrganizationId, includeArchived, cancellationToken)
      .ConfigureAwait(false);
    if (occurrence is null)
    {
      return ApplicationOperationResult<OccurrenceDetailDto>.Failed(ApplicationOperationFailure.NotFound);
    }

    try
    {
      var mutationResult = await mutate(occurrence, context, validation, cancellationToken).ConfigureAwait(false);
      if (mutationResult is not null)
      {
        return mutationResult;
      }
    }
    catch (ArgumentException)
    {
      return ApplicationOperationResult<OccurrenceDetailDto>.Failed(ApplicationOperationFailure.Conflict);
    }
    catch (InvalidOperationException)
    {
      return ApplicationOperationResult<OccurrenceDetailDto>.Failed(ApplicationOperationFailure.Conflict);
    }

    await occurrenceRepository.UpdateAsync(occurrence, cancellationToken).ConfigureAwait(false);
    var snapshot = await occurrenceRepository.FindSnapshotAsync(occurrence.Id, context.OrganizationId, true, cancellationToken)
      .ConfigureAwait(false);
    await WriteMutationSideEffectsAsync(eventName, occurrence, context, cancellationToken).ConfigureAwait(false);

    return ApplicationOperationResult<OccurrenceDetailDto>.Success(ToDetail(snapshot!, locale));
  }

  private async Task<ResolvedOccurrenceEntities> ResolveRelatedEntitiesAsync(
    Guid? contractIdValue,
    Guid? propertyIdValue,
    Guid? residentIdValue,
    OrganizationId organizationId,
    CancellationToken cancellationToken)
  {
    var errors = new List<ValidationFailure>();
    OccurrenceContractSnapshot? contract = null;
    OccurrencePropertySnapshot? property = null;
    OccurrenceResidentSnapshot? resident = null;
    var contractId = ToEntityIdOrNull(contractIdValue);
    var propertyId = ToEntityIdOrNull(propertyIdValue);
    var residentId = ToEntityIdOrNull(residentIdValue);

    if (!contractId.HasValue && !propertyId.HasValue && !residentId.HasValue)
    {
      errors.Add(new ValidationFailure(nameof(propertyIdValue), "validation.linkedEntity"));
    }

    if (contractId.HasValue)
    {
      contract = await occurrenceRepository.GetContractSnapshotAsync(contractId.Value, organizationId, cancellationToken)
        .ConfigureAwait(false);
      if (contract is null)
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
      property = await occurrenceRepository.GetPropertySnapshotAsync(propertyId.Value, organizationId, cancellationToken)
        .ConfigureAwait(false);
      if (property is null)
      {
        errors.Add(new ValidationFailure("propertyId", "validation.property"));
      }
    }

    if (residentId.HasValue)
    {
      resident = await occurrenceRepository.GetResidentSnapshotAsync(residentId.Value, organizationId, cancellationToken)
        .ConfigureAwait(false);
      if (resident is null)
      {
        errors.Add(new ValidationFailure("residentId", "validation.resident"));
      }
    }

    return new ResolvedOccurrenceEntities(
      errors,
      contractId,
      propertyId,
      residentId,
      property,
      contract,
      resident);
  }

  private async Task<OccurrenceUserSnapshot?> ResolveAssignedUserAsync(
    Guid? assignedUserIdValue,
    OrganizationId organizationId,
    List<ValidationFailure> validation,
    CancellationToken cancellationToken)
  {
    var assignedUserId = ToUserIdOrNull(assignedUserIdValue);
    if (!assignedUserId.HasValue)
    {
      return null;
    }

    var user = await occurrenceRepository.GetAssignableUserSnapshotAsync(
        assignedUserId.Value,
        organizationId,
        cancellationToken)
      .ConfigureAwait(false);
    if (user is null)
    {
      validation.Add(new ValidationFailure("assignedUserId", "validation.assignedUser"));
    }

    return user;
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
    Occurrence occurrence,
    ActiveOrganizationContext context,
    CancellationToken cancellationToken)
  {
    var subject = EntityReference.FromGuid("occurrence", occurrence.Id.Value, occurrence.Title);
    var actor = EventActor.User(context.UserId, context.User.DisplayName);
    var data = new Dictionary<string, string>(StringComparer.Ordinal)
    {
      ["type"] = OccurrenceCatalog.ToTypeLabel(occurrence.Type).Code,
      ["priority"] = OccurrenceCatalog.ToPriorityLabel(occurrence.Priority).Code,
      ["status"] = OccurrenceCatalog.ToStatusLabel(occurrence.EffectiveStatus).Code,
      ["unresolved"] = occurrence.IsUnresolved.ToString(CultureInfo.InvariantCulture)
    };

    if (occurrence.DueDate.HasValue)
    {
      data["dueDate"] = occurrence.DueDate.Value.ToString("O", CultureInfo.InvariantCulture);
    }

    if (occurrence.AssignedUserId.HasValue)
    {
      data["assignedUserId"] = occurrence.AssignedUserId.Value.Value.ToString("D");
    }

    var related = new List<EntityReference>();
    if (occurrence.PropertyId.HasValue)
    {
      related.Add(EntityReference.FromGuid("property", occurrence.PropertyId.Value.Value));
    }

    if (occurrence.ResidentId.HasValue)
    {
      related.Add(EntityReference.FromGuid("resident", occurrence.ResidentId.Value.Value));
    }

    if (occurrence.ContractId.HasValue)
    {
      related.Add(EntityReference.FromGuid("contract", occurrence.ContractId.Value.Value));
    }

    if (occurrence.AssignedUserId.HasValue)
    {
      related.Add(EntityReference.FromGuid("identityUser", occurrence.AssignedUserId.Value.Value));
    }

    var envelope = ModuleEventEnvelope.Create(
      context.OrganizationId,
      "occurrences",
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

  private static OccurrenceListItemDto ToListItem(OccurrenceSnapshot snapshot, string? locale)
  {
    var occurrence = snapshot.Occurrence;
    return new OccurrenceListItemDto(
      occurrence.Id.Value,
      occurrence.Title,
      occurrence.Description,
      OccurrenceCatalog.ToTypeLabel(occurrence.Type, locale),
      OccurrenceCatalog.ToPriorityLabel(occurrence.Priority, locale),
      OccurrenceCatalog.ToStatusLabel(occurrence.EffectiveStatus, locale),
      ToPropertySummary(snapshot.Property),
      ToResidentSummary(snapshot.Resident),
      ToContractSummary(snapshot.Contract),
      ToUserSummary(snapshot.AssignedUser),
      occurrence.DueDate,
      occurrence.IsUnresolved,
      occurrence.IsDeleted || occurrence.EffectiveStatus == OccurrenceStatus.Archived,
      occurrence.CreatedAt,
      occurrence.UpdatedAt,
      occurrence.ConcurrencyToken.Value);
  }

  private static OccurrenceDetailDto ToDetail(OccurrenceSnapshot snapshot, string? locale)
  {
    var listItem = ToListItem(snapshot, locale);
    var occurrence = snapshot.Occurrence;
    var id = Uri.EscapeDataString(occurrence.Id.Value.ToString("D"));

    return new OccurrenceDetailDto(
      listItem.Id,
      listItem.Title,
      listItem.Description,
      listItem.Type,
      listItem.Priority,
      listItem.Status,
      listItem.Property,
      listItem.Resident,
      listItem.Contract,
      listItem.AssignedUser,
      listItem.DueDate,
      occurrence.ResolvedAt,
      occurrence.ResolvedByUserId?.Value,
      occurrence.ResolutionNotes,
      occurrence.CancelledAt,
      occurrence.CancelledByUserId?.Value,
      occurrence.CancellationNotes,
      snapshot.Comments.Select(comment => ToCommentDto(comment)).ToArray(),
      snapshot.Attachments.Select(attachment => ToDocumentDto(attachment, id)).ToArray(),
      snapshot.StatusHistory.Select(history => ToStatusHistoryDto(history, locale)).ToArray(),
      snapshot.PriorityHistory.Select(history => ToPriorityHistoryDto(history, locale)).ToArray(),
      snapshot.AssignmentHistory.Select(ToAssignmentHistoryDto).ToArray(),
      $"/timeline?entityType=occurrence&entityId={id}",
      $"/auditoria?entityType=occurrence&entityId={id}",
      occurrence.CreatedAt,
      occurrence.UpdatedAt,
      occurrence.DeletedAt,
      occurrence.ConcurrencyToken.Value);
  }

  private static OccurrenceEntitySummaryDto? ToPropertySummary(OccurrencePropertySnapshot? property) =>
    property is null
      ? null
      : new OccurrenceEntitySummaryDto(
        property.PropertyId.Value,
        property.Name,
        property.Location,
        $"/imoveis?id={property.PropertyId.Value:D}");

  private static OccurrenceEntitySummaryDto? ToResidentSummary(OccurrenceResidentSnapshot? resident) =>
    resident is null
      ? null
      : new OccurrenceEntitySummaryDto(
        resident.ResidentId.Value,
        resident.Name,
        Route: $"/moradores?id={resident.ResidentId.Value:D}");

  private static OccurrenceEntitySummaryDto? ToContractSummary(OccurrenceContractSnapshot? contract) =>
    contract is null
      ? null
      : new OccurrenceEntitySummaryDto(
        contract.ContractId.Value,
        contract.DisplayName,
        $"{contract.PropertyName} - {contract.ResidentName}",
        $"/contratos?id={contract.ContractId.Value:D}");

  private static OccurrenceUserSummaryDto? ToUserSummary(OccurrenceUserSnapshot? user) =>
    user is null ? null : new OccurrenceUserSummaryDto(user.UserId.Value, user.DisplayName, user.Email);

  private static OccurrenceCommentDto ToCommentDto(OccurrenceCommentSnapshot snapshot) =>
    new(
      snapshot.Comment.Id.Value,
      snapshot.Comment.Body,
      snapshot.Comment.IsInternal,
      snapshot.Comment.CreatedByUserId?.Value,
      snapshot.Author?.DisplayName,
      snapshot.Comment.CreatedAt);

  private static OccurrenceDocumentDto ToDocumentDto(OccurrenceDocumentSnapshot document, string id) =>
    new(
      document.DocumentId.Value,
      document.Label,
      $"/documentos?entityType=occurrence&entityId={id}",
      document.CreatedAt);

  private static OccurrenceStatusHistoryDto ToStatusHistoryDto(
    OccurrenceStatusHistorySnapshot snapshot,
    string? locale) =>
    new(
      snapshot.History.Id.Value,
      snapshot.History.PreviousStatus.HasValue
        ? OccurrenceCatalog.ToStatusLabel(snapshot.History.PreviousStatus.Value, locale)
        : null,
      OccurrenceCatalog.ToStatusLabel(snapshot.History.NewStatus, locale),
      snapshot.History.Notes,
      snapshot.History.CreatedByUserId?.Value,
      snapshot.Actor?.DisplayName,
      snapshot.History.CreatedAt);

  private static OccurrencePriorityHistoryDto ToPriorityHistoryDto(
    OccurrencePriorityHistorySnapshot snapshot,
    string? locale) =>
    new(
      snapshot.History.Id.Value,
      snapshot.History.PreviousPriority.HasValue
        ? OccurrenceCatalog.ToPriorityLabel(snapshot.History.PreviousPriority.Value, locale)
        : null,
      OccurrenceCatalog.ToPriorityLabel(snapshot.History.NewPriority, locale),
      snapshot.History.Notes,
      snapshot.History.CreatedByUserId?.Value,
      snapshot.Actor?.DisplayName,
      snapshot.History.CreatedAt);

  private static OccurrenceAssignmentHistoryDto ToAssignmentHistoryDto(
    OccurrenceAssignmentHistorySnapshot snapshot) =>
    new(
      snapshot.History.Id.Value,
      ToUserSummary(snapshot.PreviousAssignedUser),
      ToUserSummary(snapshot.NewAssignedUser),
      snapshot.History.Notes,
      snapshot.History.CreatedByUserId?.Value,
      snapshot.Actor?.DisplayName,
      snapshot.History.CreatedAt);

  private static IEnumerable<ValidationFailure> ValidateCreateRequest(OccurrenceCreateRequestDto request)
  {
    foreach (var failure in ValidateCommonFields(
      request.Title,
      request.Description,
      request.Type,
      request.PropertyId,
      request.ResidentId,
      request.ContractId,
      request.DueDate))
    {
      yield return failure;
    }

    if (!OccurrenceCatalog.TryParsePriority(request.Priority, out _))
    {
      yield return new ValidationFailure(nameof(request.Priority), "validation.occurrencePriority");
    }
  }

  private static IEnumerable<ValidationFailure> ValidateUpdateRequest(OccurrenceUpdateRequestDto request) =>
    ValidateCommonFields(
      request.Title,
      request.Description,
      request.Type,
      request.PropertyId,
      request.ResidentId,
      request.ContractId,
      request.DueDate);

  private static IEnumerable<ValidationFailure> ValidateCommonFields(
    string title,
    string description,
    string type,
    Guid? propertyId,
    Guid? residentId,
    Guid? contractId,
    DateOnly? dueDate)
  {
    if (string.IsNullOrWhiteSpace(title))
    {
      yield return new ValidationFailure(nameof(title), ValidationMessageKeys.Required);
    }
    else if (title.Trim().Length > 200)
    {
      yield return new ValidationFailure(nameof(title), ValidationMessageKeys.MaxLength);
    }

    if (string.IsNullOrWhiteSpace(description))
    {
      yield return new ValidationFailure(nameof(description), ValidationMessageKeys.Required);
    }
    else if (description.Trim().Length > 4000)
    {
      yield return new ValidationFailure(nameof(description), ValidationMessageKeys.MaxLength);
    }

    if (!OccurrenceCatalog.TryParseType(type, out _))
    {
      yield return new ValidationFailure(nameof(type), "validation.occurrenceType");
    }

    if (!HasNonEmptyId(propertyId) && !HasNonEmptyId(residentId) && !HasNonEmptyId(contractId))
    {
      yield return new ValidationFailure(nameof(propertyId), "validation.linkedEntity");
    }

    if (dueDate.HasValue && dueDate.Value == default)
    {
      yield return new ValidationFailure(nameof(dueDate), ValidationMessageKeys.InvalidDate);
    }
  }

  private static IEnumerable<ValidationFailure> ValidateId(Guid id, string propertyName = "id")
  {
    if (id == Guid.Empty)
    {
      yield return new ValidationFailure(propertyName, ValidationMessageKeys.InvalidId);
    }
  }

  private static EntityId? ToEntityIdOrNull(Guid? id) =>
    id.HasValue && id.Value != Guid.Empty ? new EntityId(id.Value) : null;

  private static UserId? ToUserIdOrNull(Guid? id) =>
    id.HasValue && id.Value != Guid.Empty ? new UserId(id.Value) : null;

  private static bool HasNonEmptyId(Guid? id) => id.HasValue && id.Value != Guid.Empty;

  private static bool ConcurrencyMismatch(Occurrence occurrence, string? concurrencyToken) =>
    !string.IsNullOrWhiteSpace(concurrencyToken) &&
    !string.Equals(occurrence.ConcurrencyToken.Value, concurrencyToken, StringComparison.Ordinal);

  private sealed record ResolvedOccurrenceEntities(
    IReadOnlyList<ValidationFailure> Errors,
    EntityId? ContractId,
    EntityId? PropertyId,
    EntityId? ResidentId,
    OccurrencePropertySnapshot? Property,
    OccurrenceContractSnapshot? Contract,
    OccurrenceResidentSnapshot? Resident);
}
