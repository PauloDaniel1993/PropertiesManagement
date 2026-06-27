using Alsappan.Application.Common.Audit;
using Alsappan.Application.Common.Authorization;
using Alsappan.Application.Common.Contracts;
using Alsappan.Application.Common.Events;
using Alsappan.Application.Common.Results;
using Alsappan.Application.Common.Tenancy;
using Alsappan.Application.Common.Validation;
using Alsappan.Application.Contracts.Repositories;
using Alsappan.Application.Properties.Repositories;
using Alsappan.Domain.Common.Events;
using Alsappan.Domain.Common.Identifiers;
using Alsappan.Domain.Common.ValueObjects;
using Alsappan.Domain.Contracts;
using Alsappan.Domain.Properties;

namespace Alsappan.Application.Contracts;

public sealed class ContractService : IContractService
{
  private readonly IContractRepository contractRepository;
  private readonly IPropertyRepository propertyRepository;
  private readonly IPermissionService permissionService;
  private readonly IActiveOrganizationContextResolver activeOrganizationContextResolver;
  private readonly IAuditWriter auditWriter;
  private readonly IModuleEventOutboxWriter outboxWriter;
  private readonly TimeProvider timeProvider;

  public ContractService(
    IContractRepository contractRepository,
    IPropertyRepository propertyRepository,
    IPermissionService permissionService,
    IActiveOrganizationContextResolver activeOrganizationContextResolver,
    IAuditWriter auditWriter,
    IModuleEventOutboxWriter outboxWriter,
    TimeProvider? timeProvider = null)
  {
    this.contractRepository = contractRepository ?? throw new ArgumentNullException(nameof(contractRepository));
    this.propertyRepository = propertyRepository ?? throw new ArgumentNullException(nameof(propertyRepository));
    this.permissionService = permissionService ?? throw new ArgumentNullException(nameof(permissionService));
    this.activeOrganizationContextResolver = activeOrganizationContextResolver
      ?? throw new ArgumentNullException(nameof(activeOrganizationContextResolver));
    this.auditWriter = auditWriter ?? throw new ArgumentNullException(nameof(auditWriter));
    this.outboxWriter = outboxWriter ?? throw new ArgumentNullException(nameof(outboxWriter));
    this.timeProvider = timeProvider ?? TimeProvider.System;
  }

  public async Task<ApplicationOperationResult<PagedResultDto<ContractListItemDto>>> ListAsync(
    ContractListRequestDto request,
    CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(request);

    var context = await AuthorizeContextAsync(PermissionCodes.Read(PermissionModules.Contracts), cancellationToken)
      .ConfigureAwait(false);
    if (context is null)
    {
      return ApplicationOperationResult<PagedResultDto<ContractListItemDto>>.Failed(
        ApplicationOperationFailure.Forbidden);
    }

    var today = Today();
    var page = await contractRepository.ListAsync(request, context.OrganizationId, today, cancellationToken)
      .ConfigureAwait(false);
    var items = page.Items.Select(snapshot => ToListItem(snapshot, request.Locale, today)).ToArray();

    return ApplicationOperationResult<PagedResultDto<ContractListItemDto>>.Success(
      new PagedResultDto<ContractListItemDto>(items, page.Page, page.PageSize, page.TotalItems));
  }

  public async Task<ApplicationOperationResult<ContractDetailDto>> GetAsync(
    Guid id,
    string? locale = null,
    CancellationToken cancellationToken = default)
  {
    var validation = ValidateId(id).ToList();
    if (validation.Count > 0)
    {
      return ApplicationOperationResult<ContractDetailDto>.Invalid(validation);
    }

    var context = await AuthorizeContextAsync(PermissionCodes.Read(PermissionModules.Contracts), cancellationToken)
      .ConfigureAwait(false);
    if (context is null)
    {
      return ApplicationOperationResult<ContractDetailDto>.Failed(ApplicationOperationFailure.Forbidden);
    }

    var snapshot = await contractRepository.FindSnapshotAsync(
        new EntityId(id),
        context.OrganizationId,
        includeArchived: true,
        cancellationToken)
      .ConfigureAwait(false);

    return snapshot is null
      ? ApplicationOperationResult<ContractDetailDto>.Failed(ApplicationOperationFailure.NotFound)
      : ApplicationOperationResult<ContractDetailDto>.Success(ToDetail(snapshot, locale, Today()));
  }

  public async Task<ApplicationOperationResult<ContractDetailDto>> CreateAsync(
    ContractCreateRequestDto request,
    string? locale = null,
    CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(request);

    var context = await AuthorizeContextAsync(PermissionCodes.Write(PermissionModules.Contracts), cancellationToken)
      .ConfigureAwait(false);
    if (context is null)
    {
      return ApplicationOperationResult<ContractDetailDto>.Failed(ApplicationOperationFailure.Forbidden);
    }

    var validation = ValidateMutationRequest(request).ToList();
    if (validation.Count > 0)
    {
      return ApplicationOperationResult<ContractDetailDto>.Invalid(validation);
    }

    var propertyId = new EntityId(request.PropertyId);
    var primaryResidentId = new EntityId(request.PrimaryResidentId);
    var residentIds = NormalizeResidentIds(request.ResidentIds).Select(id => new EntityId(id)).ToArray();

    var relatedValidation = await ValidateRelatedEntitiesAsync(
        propertyId,
        primaryResidentId,
        residentIds,
        context.OrganizationId,
        cancellationToken)
      .ConfigureAwait(false);
    if (relatedValidation.Errors.Count > 0)
    {
      return ApplicationOperationResult<ContractDetailDto>.Invalid(relatedValidation.Errors);
    }

    var lifecycleAction = ContractCode.NormalizeCode(request.LifecycleAction);
    if (lifecycleAction is not "draft" and not "activate")
    {
      return ApplicationOperationResult<ContractDetailDto>.Invalid(
        [new ValidationFailure(nameof(request.LifecycleAction), "validation.lifecycleAction")]);
    }

    if (lifecycleAction == "activate")
    {
      var availability = await EnsurePropertyAvailableAsync(
          propertyId,
          context.OrganizationId,
          request.StartDate,
          request.EndDate,
          ignoredContractId: null,
          cancellationToken)
        .ConfigureAwait(false);
      if (availability is not null)
      {
        return availability;
      }
    }

    var now = timeProvider.GetUtcNow();
    var contract = LeaseContract.Create(
      EntityId.New(),
      context.OrganizationId,
      propertyId,
      primaryResidentId,
      residentIds,
      request.StartDate,
      request.EndDate,
      ToMoney(request.MonthlyRent),
      request.DueDay,
      ToMoneyOrNull(request.DepositAmount),
      ParseAdjustmentIndex(request.AdjustmentIndex),
      request.AdjustmentIntervalMonths,
      request.NextAdjustmentDate,
      request.PenaltyNotes,
      request.DiscountNotes,
      request.GeneratePaymentsAutomatically,
      request.Notes,
      relatedValidation.Property!.PropertyName,
      string.Join(' ', relatedValidation.Residents.Select(resident => resident.ResidentName)),
      now,
      context.UserId);

    if (lifecycleAction == "activate")
    {
      contract.Activate(now, context.UserId);
    }

    await contractRepository.AddAsync(contract, cancellationToken).ConfigureAwait(false);
    if (contract.Status == ContractStatus.Active)
    {
      await UpdatePropertyStatusAsync(propertyId, PropertyStatus.Rented, context, cancellationToken)
        .ConfigureAwait(false);
    }

    var snapshot = await contractRepository.FindSnapshotAsync(contract.Id, context.OrganizationId, true, cancellationToken)
      .ConfigureAwait(false);
    await WriteMutationSideEffectsAsync("contract.created", snapshot!.Contract, snapshot, context, cancellationToken)
      .ConfigureAwait(false);

    return ApplicationOperationResult<ContractDetailDto>.Success(ToDetail(snapshot, locale, Today()));
  }

  public async Task<ApplicationOperationResult<ContractDetailDto>> UpdateAsync(
    Guid id,
    ContractUpdateRequestDto request,
    string? locale = null,
    CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(request);

    var validation = ValidateId(id).Concat(ValidateMutationRequest(request)).ToList();
    if (validation.Count > 0)
    {
      return ApplicationOperationResult<ContractDetailDto>.Invalid(validation);
    }

    var context = await AuthorizeContextAsync(PermissionCodes.Write(PermissionModules.Contracts), cancellationToken)
      .ConfigureAwait(false);
    if (context is null)
    {
      return ApplicationOperationResult<ContractDetailDto>.Failed(ApplicationOperationFailure.Forbidden);
    }

    var contract = await contractRepository.FindAsync(
        new EntityId(id),
        context.OrganizationId,
        includeArchived: false,
        cancellationToken)
      .ConfigureAwait(false);
    if (contract is null)
    {
      return ApplicationOperationResult<ContractDetailDto>.Failed(ApplicationOperationFailure.NotFound);
    }

    var stateConflict = EnsureTermsCanBeChanged(contract);
    if (stateConflict is not null)
    {
      return stateConflict;
    }

    var concurrencyConflict = EnsureCurrentConcurrencyToken(contract, request.ConcurrencyToken);
    if (concurrencyConflict is not null)
    {
      return concurrencyConflict;
    }

    var primaryResidentId = new EntityId(request.PrimaryResidentId);
    var residentIds = NormalizeResidentIds(request.ResidentIds).Select(residentId => new EntityId(residentId)).ToArray();
    var relatedValidation = await ValidateRelatedEntitiesAsync(
        contract.PropertyId,
        primaryResidentId,
        residentIds,
        context.OrganizationId,
        cancellationToken)
      .ConfigureAwait(false);
    if (relatedValidation.Errors.Count > 0)
    {
      return ApplicationOperationResult<ContractDetailDto>.Invalid(relatedValidation.Errors);
    }

    if (contract.Status == ContractStatus.Active)
    {
      var availability = await EnsurePropertyAvailableAsync(
          contract.PropertyId,
          context.OrganizationId,
          request.StartDate,
          request.EndDate,
          contract.Id,
          cancellationToken)
        .ConfigureAwait(false);
      if (availability is not null)
      {
        return availability;
      }
    }

    contract.Update(
      primaryResidentId,
      residentIds,
      request.StartDate,
      request.EndDate,
      ToMoney(request.MonthlyRent),
      request.DueDay,
      ToMoneyOrNull(request.DepositAmount),
      ParseAdjustmentIndex(request.AdjustmentIndex),
      request.AdjustmentIntervalMonths,
      request.NextAdjustmentDate,
      request.PenaltyNotes,
      request.DiscountNotes,
      request.GeneratePaymentsAutomatically,
      request.Notes,
      relatedValidation.Property!.PropertyName,
      string.Join(' ', relatedValidation.Residents.Select(resident => resident.ResidentName)),
      timeProvider.GetUtcNow(),
      context.UserId);

    await contractRepository.UpdateAsync(contract, cancellationToken).ConfigureAwait(false);
    var snapshot = await contractRepository.FindSnapshotAsync(contract.Id, context.OrganizationId, true, cancellationToken)
      .ConfigureAwait(false);
    await WriteMutationSideEffectsAsync("contract.updated", snapshot!.Contract, snapshot, context, cancellationToken)
      .ConfigureAwait(false);

    return ApplicationOperationResult<ContractDetailDto>.Success(ToDetail(snapshot, locale, Today()));
  }

  public async Task<ApplicationOperationResult<ContractDetailDto>> ActivateAsync(
    Guid id,
    ContractLifecycleRequestDto request,
    string? locale = null,
    CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(request);
    return await MutateLifecycleAsync(
        id,
        PermissionCodes.Manage(PermissionModules.Contracts),
        "contract.activated",
        locale,
        async (contract, context) =>
        {
          var availability = await EnsurePropertyAvailableAsync(
              contract.PropertyId,
              context.OrganizationId,
              contract.StartDate,
              contract.EndDate,
              contract.Id,
              cancellationToken)
            .ConfigureAwait(false);
          if (availability is not null)
          {
            return availability;
          }

          contract.Activate(timeProvider.GetUtcNow(), context.UserId);
          await UpdatePropertyStatusAsync(contract.PropertyId, PropertyStatus.Rented, context, cancellationToken)
            .ConfigureAwait(false);
          return null;
        },
        cancellationToken)
      .ConfigureAwait(false);
  }

  public async Task<ApplicationOperationResult<ContractDetailDto>> TerminateAsync(
    Guid id,
    ContractLifecycleRequestDto request,
    string? locale = null,
    CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(request);
    return await MutateLifecycleAsync(
        id,
        PermissionCodes.Manage(PermissionModules.Contracts),
        "contract.terminated",
        locale,
        async (contract, context) =>
        {
          contract.Terminate(request.EffectiveDate ?? Today(), timeProvider.GetUtcNow(), context.UserId);
          await RefreshPropertyRentalStatusAsync(contract.PropertyId, contract.Id, context, cancellationToken)
            .ConfigureAwait(false);
          return null;
        },
        cancellationToken)
      .ConfigureAwait(false);
  }

  public async Task<ApplicationOperationResult<ContractDetailDto>> CancelAsync(
    Guid id,
    ContractLifecycleRequestDto request,
    string? locale = null,
    CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(request);
    return await MutateLifecycleAsync(
        id,
        PermissionCodes.Manage(PermissionModules.Contracts),
        "contract.cancelled",
        locale,
        async (contract, context) =>
        {
          var wasActive = contract.Status == ContractStatus.Active;
          contract.Cancel(timeProvider.GetUtcNow(), context.UserId);
          if (wasActive)
          {
            await RefreshPropertyRentalStatusAsync(contract.PropertyId, contract.Id, context, cancellationToken)
              .ConfigureAwait(false);
          }

          return null;
        },
        cancellationToken)
      .ConfigureAwait(false);
  }

  public async Task<ApplicationOperationResult> ArchiveAsync(
    Guid id,
    CancellationToken cancellationToken = default)
  {
    var validation = ValidateId(id).ToList();
    if (validation.Count > 0)
    {
      return ApplicationOperationResult.Invalid(validation);
    }

    var context = await AuthorizeContextAsync(PermissionCodes.Archive(PermissionModules.Contracts), cancellationToken)
      .ConfigureAwait(false);
    if (context is null)
    {
      return ApplicationOperationResult.Failed(ApplicationOperationFailure.Forbidden);
    }

    var contract = await contractRepository.FindAsync(
        new EntityId(id),
        context.OrganizationId,
        includeArchived: false,
        cancellationToken)
      .ConfigureAwait(false);
    if (contract is null)
    {
      return ApplicationOperationResult.Failed(ApplicationOperationFailure.NotFound);
    }

    var wasActive = contract.Status == ContractStatus.Active;
    contract.Archive(timeProvider.GetUtcNow(), context.UserId);
    await contractRepository.UpdateAsync(contract, cancellationToken).ConfigureAwait(false);
    if (wasActive)
    {
      await RefreshPropertyRentalStatusAsync(contract.PropertyId, contract.Id, context, cancellationToken)
        .ConfigureAwait(false);
    }

    var snapshot = await contractRepository.FindSnapshotAsync(contract.Id, context.OrganizationId, true, cancellationToken)
      .ConfigureAwait(false);
    await WriteMutationSideEffectsAsync("contract.archived", contract, snapshot, context, cancellationToken)
      .ConfigureAwait(false);

    return ApplicationOperationResult.Success();
  }

  public async Task<ApplicationOperationResult<ContractDetailDto>> RestoreAsync(
    Guid id,
    string? locale = null,
    CancellationToken cancellationToken = default)
  {
    var validation = ValidateId(id).ToList();
    if (validation.Count > 0)
    {
      return ApplicationOperationResult<ContractDetailDto>.Invalid(validation);
    }

    var context = await AuthorizeContextAsync(PermissionCodes.Archive(PermissionModules.Contracts), cancellationToken)
      .ConfigureAwait(false);
    if (context is null)
    {
      return ApplicationOperationResult<ContractDetailDto>.Failed(ApplicationOperationFailure.Forbidden);
    }

    var contract = await contractRepository.FindAsync(
        new EntityId(id),
        context.OrganizationId,
        includeArchived: true,
        cancellationToken)
      .ConfigureAwait(false);
    if (contract is null)
    {
      return ApplicationOperationResult<ContractDetailDto>.Failed(ApplicationOperationFailure.NotFound);
    }

    contract.Restore(timeProvider.GetUtcNow(), context.UserId);
    await contractRepository.UpdateAsync(contract, cancellationToken).ConfigureAwait(false);
    var snapshot = await contractRepository.FindSnapshotAsync(contract.Id, context.OrganizationId, true, cancellationToken)
      .ConfigureAwait(false);
    await WriteMutationSideEffectsAsync("contract.restored", contract, snapshot, context, cancellationToken)
      .ConfigureAwait(false);

    return ApplicationOperationResult<ContractDetailDto>.Success(ToDetail(snapshot!, locale, Today()));
  }

  public Task<IReadOnlyList<StatusLabelDto>> GetStatusOptionsAsync(
    string? locale = null,
    CancellationToken cancellationToken = default)
  {
    cancellationToken.ThrowIfCancellationRequested();
    return Task.FromResult(ContractCatalog.GetStatusOptions(locale));
  }

  public Task<IReadOnlyList<SelectOptionDto>> GetAdjustmentIndexOptionsAsync(
    string? locale = null,
    CancellationToken cancellationToken = default)
  {
    cancellationToken.ThrowIfCancellationRequested();
    return Task.FromResult(ContractCatalog.GetAdjustmentIndexOptions(locale));
  }

  private async Task<ApplicationOperationResult<ContractDetailDto>> MutateLifecycleAsync(
    Guid id,
    string permissionCode,
    string action,
    string? locale,
    Func<LeaseContract, ActiveOrganizationContext, Task<ApplicationOperationResult<ContractDetailDto>?>> mutateAsync,
    CancellationToken cancellationToken)
  {
    var validation = ValidateId(id).ToList();
    if (validation.Count > 0)
    {
      return ApplicationOperationResult<ContractDetailDto>.Invalid(validation);
    }

    var context = await AuthorizeContextAsync(permissionCode, cancellationToken).ConfigureAwait(false);
    if (context is null)
    {
      return ApplicationOperationResult<ContractDetailDto>.Failed(ApplicationOperationFailure.Forbidden);
    }

    var contract = await contractRepository.FindAsync(
        new EntityId(id),
        context.OrganizationId,
        includeArchived: false,
        cancellationToken)
      .ConfigureAwait(false);
    if (contract is null)
    {
      return ApplicationOperationResult<ContractDetailDto>.Failed(ApplicationOperationFailure.NotFound);
    }

    var earlyResult = await mutateAsync(contract, context).ConfigureAwait(false);
    if (earlyResult is not null)
    {
      return earlyResult;
    }

    await contractRepository.UpdateAsync(contract, cancellationToken).ConfigureAwait(false);
    var snapshot = await contractRepository.FindSnapshotAsync(contract.Id, context.OrganizationId, true, cancellationToken)
      .ConfigureAwait(false);
    await WriteMutationSideEffectsAsync(action, contract, snapshot, context, cancellationToken).ConfigureAwait(false);

    return ApplicationOperationResult<ContractDetailDto>.Success(ToDetail(snapshot!, locale, Today()));
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

  private async Task<RelatedEntityValidationResult> ValidateRelatedEntitiesAsync(
    EntityId propertyId,
    EntityId primaryResidentId,
    EntityId[] residentIds,
    OrganizationId organizationId,
    CancellationToken cancellationToken)
  {
    var errors = new List<ValidationFailure>();
    var property = await contractRepository.GetPropertySnapshotAsync(propertyId, organizationId, cancellationToken)
      .ConfigureAwait(false);
    if (property is null)
    {
      errors.Add(new ValidationFailure("propertyId", "validation.property"));
    }

    if (!residentIds.Contains(primaryResidentId))
    {
      errors.Add(new ValidationFailure("primaryResidentId", "validation.primaryResident"));
    }

    var residents = await contractRepository.GetResidentSnapshotsAsync(
        residentIds,
        primaryResidentId,
        organizationId,
        cancellationToken)
      .ConfigureAwait(false);
    if (residents.Count != residentIds.Length)
    {
      errors.Add(new ValidationFailure("residentIds", "validation.residents"));
    }

    return new RelatedEntityValidationResult(errors, property, residents);
  }

  private async Task<ApplicationOperationResult<ContractDetailDto>?> EnsurePropertyAvailableAsync(
    EntityId propertyId,
    OrganizationId organizationId,
    DateOnly startDate,
    DateOnly? endDate,
    EntityId? ignoredContractId,
    CancellationToken cancellationToken)
  {
    var hasOverlap = await contractRepository.HasOverlappingActiveContractAsync(
        propertyId,
        organizationId,
        startDate,
        endDate,
        ignoredContractId,
        cancellationToken)
      .ConfigureAwait(false);

    return hasOverlap
      ? ApplicationOperationResult<ContractDetailDto>.Failed(
        ApplicationOperationFailure.Conflict,
        new Dictionary<string, string[]> { ["propertyId"] = ["validation.propertyAvailability"] })
      : null;
  }

  private async Task UpdatePropertyStatusAsync(
    EntityId propertyId,
    PropertyStatus status,
    ActiveOrganizationContext context,
    CancellationToken cancellationToken)
  {
    var property = await propertyRepository.FindAsync(propertyId, context.OrganizationId, false, cancellationToken)
      .ConfigureAwait(false);
    if (property is null)
    {
      return;
    }

    property.ChangeStatus(status, timeProvider.GetUtcNow(), context.UserId);
    await propertyRepository.UpdateAsync(property, cancellationToken).ConfigureAwait(false);
  }

  private async Task RefreshPropertyRentalStatusAsync(
    EntityId propertyId,
    EntityId releasedContractId,
    ActiveOrganizationContext context,
    CancellationToken cancellationToken)
  {
    var hasAnotherActiveContract = await contractRepository.HasAnyActiveContractAsync(
        propertyId,
        context.OrganizationId,
        releasedContractId,
        cancellationToken)
      .ConfigureAwait(false);
    await UpdatePropertyStatusAsync(
        propertyId,
        hasAnotherActiveContract ? PropertyStatus.Rented : PropertyStatus.Available,
        context,
        cancellationToken)
      .ConfigureAwait(false);
  }

  private static ApplicationOperationResult<ContractDetailDto>? EnsureTermsCanBeChanged(LeaseContract contract) =>
    contract.Status is ContractStatus.Archived or ContractStatus.Cancelled or ContractStatus.Terminated
      ? ApplicationOperationResult<ContractDetailDto>.Failed(
        ApplicationOperationFailure.Conflict,
        new Dictionary<string, string[]> { ["status"] = ["validation.contractStatus"] })
      : null;

  private static ApplicationOperationResult<ContractDetailDto>? EnsureCurrentConcurrencyToken(
    LeaseContract contract,
    string? concurrencyToken) =>
    string.IsNullOrWhiteSpace(concurrencyToken) ||
    !string.Equals(concurrencyToken.Trim(), contract.ConcurrencyToken.Value, StringComparison.Ordinal)
      ? ApplicationOperationResult<ContractDetailDto>.Failed(
        ApplicationOperationFailure.Conflict,
        new Dictionary<string, string[]> { ["concurrencyToken"] = ["validation.concurrency"] })
      : null;

  private async Task WriteMutationSideEffectsAsync(
    string action,
    LeaseContract contract,
    ContractSnapshot? snapshot,
    ActiveOrganizationContext context,
    CancellationToken cancellationToken)
  {
    var now = timeProvider.GetUtcNow();
    var displayName = snapshot is null
      ? contract.Id.Value.ToString("D")
      : $"{snapshot.Property.PropertyName} - {snapshot.Residents.FirstOrDefault(resident => resident.IsPrimary)?.ResidentName}";
    var subject = EntityReference.FromGuid("contract", contract.Id.Value, displayName);
    var actor = EventActor.User(context.UserId, context.User.DisplayName);
    var data = new Dictionary<string, string>(StringComparer.Ordinal)
    {
      ["status"] = ContractCatalog.ToStatusCode(contract.Status),
      ["effectiveStatus"] = ContractCatalog.ToStatusCode(contract.GetEffectiveStatus(Today())),
      ["propertyId"] = contract.PropertyId.Value.ToString("D"),
      ["primaryResidentId"] = contract.PrimaryResidentId.Value.ToString("D"),
      ["monthlyRent"] = contract.MonthlyRent.Amount.ToString(System.Globalization.CultureInfo.InvariantCulture),
      ["currency"] = contract.MonthlyRent.Currency
    };

    var related = new List<EntityReference>
    {
      EntityReference.FromGuid("property", contract.PropertyId.Value, snapshot?.Property.PropertyName)
    };
    related.AddRange(snapshot?.Residents.Select(resident =>
        EntityReference.FromGuid("resident", resident.ResidentId.Value, resident.ResidentName)) ??
      []);

    var envelope = ModuleEventEnvelope.Create(
      context.OrganizationId,
      "contracts",
      action,
      now,
      actor,
      subject,
      ModuleEventConsumer.Audit | ModuleEventConsumer.Timeline | ModuleEventConsumer.Notifications,
      data,
      related);

    await auditWriter.WriteAsync(AuditEntryDraft.FromModuleEvent(envelope), cancellationToken)
      .ConfigureAwait(false);
    await outboxWriter.EnqueueAsync(envelope, cancellationToken).ConfigureAwait(false);
  }

  private static ContractDetailDto ToDetail(ContractSnapshot snapshot, string? locale, DateOnly today)
  {
    var item = ToListItem(snapshot, locale, today);
    var contract = snapshot.Contract;
    return new ContractDetailDto(
      item.Id,
      item.Property,
      item.PrimaryResident,
      item.Residents,
      item.Status,
      item.StartDate,
      item.EndDate,
      item.MonthlyRent,
      item.DueDay,
      ToMoneyDtoOrNull(contract.DepositAmount),
      item.AdjustmentIndex,
      item.AdjustmentIndexLabel,
      contract.AdjustmentIntervalMonths,
      contract.NextAdjustmentDate,
      contract.PenaltyNotes,
      contract.DiscountNotes,
      contract.GeneratePaymentsAutomatically,
      contract.Notes,
      RelationshipSummaries(contract.Id.Value, locale),
      DocumentSummaries(contract.Id.Value, locale),
      contract.CreatedAt,
      contract.UpdatedAt,
      contract.DeletedAt,
      contract.ConcurrencyToken.Value);
  }

  private static ContractListItemDto ToListItem(ContractSnapshot snapshot, string? locale, DateOnly today)
  {
    var contract = snapshot.Contract;
    var residents = snapshot.Residents
      .OrderByDescending(resident => resident.IsPrimary)
      .ThenBy(resident => resident.ResidentName, StringComparer.OrdinalIgnoreCase)
      .Select(resident => new ContractPartyDto(resident.ResidentId.Value, resident.ResidentName, resident.IsPrimary))
      .ToArray();
    var primary = residents.FirstOrDefault(resident => resident.IsPrimary) ??
      new ContractPartyDto(contract.PrimaryResidentId.Value, string.Empty, true);

    return new ContractListItemDto(
      contract.Id.Value,
      new ContractPropertySummaryDto(snapshot.Property.PropertyId.Value, snapshot.Property.PropertyName, snapshot.Property.Location),
      primary,
      residents,
      ContractCatalog.GetStatusLabel(contract.GetEffectiveStatus(today), locale),
      contract.StartDate,
      contract.EndDate,
      ToMoneyDto(contract.MonthlyRent),
      contract.DueDay,
      ContractCatalog.ToAdjustmentIndexCode(contract.AdjustmentIndex),
      ContractCatalog.GetAdjustmentIndexLabel(contract.AdjustmentIndex, locale),
      contract.IsDeleted,
      contract.CreatedAt,
      contract.UpdatedAt,
      contract.ConcurrencyToken.Value);
  }

  private static IReadOnlyList<ContractRelationshipSummaryDto> RelationshipSummaries(Guid contractId, string? locale)
  {
    var portuguese = ContractCatalog.IsPortuguese(locale);
    var id = Uri.EscapeDataString(contractId.ToString("D"));

    return
    [
      new("property", portuguese ? "Imovel" : "Property", 1, $"/imoveis?contractId={id}"),
      new("residents", portuguese ? "Moradores" : "Residents", 0, $"/moradores?contractId={id}"),
      new("payments", portuguese ? "Pagamentos" : "Payments", 0, $"/pagamentos?contractId={id}"),
      new("utility-accounts", portuguese ? "Contas de consumo" : "Utility accounts", 0, $"/contas-de-consumo?contractId={id}"),
      new("documents", portuguese ? "Documentos" : "Documents", 0, $"/documentos?contractId={id}"),
      new("inspections", portuguese ? "Vistorias" : "Inspections", 0, $"/vistorias?contractId={id}"),
      new("timeline", "Timeline", 0, $"/timeline?entityType=contract&entityId={id}"),
      new("audit", portuguese ? "Auditoria" : "Audit", 0, $"/auditoria?entityType=contract&entityId={id}")
    ];
  }

  private static IReadOnlyList<ContractDocumentLinkSummaryDto> DocumentSummaries(Guid contractId, string? locale)
  {
    var portuguese = ContractCatalog.IsPortuguese(locale);
    var id = Uri.EscapeDataString(contractId.ToString("D"));
    return
    [
      new(null, "signed-contract", portuguese ? "Contrato assinado" : "Signed contract", 0, $"/documentos?contractId={id}&category=signed-contract")
    ];
  }

  private static IEnumerable<ValidationFailure> ValidateMutationRequest(ContractCreateRequestDto request)
  {
    if (request.PropertyId == Guid.Empty)
    {
      yield return new ValidationFailure(nameof(request.PropertyId), ValidationMessageKeys.InvalidId);
    }

    foreach (var failure in ValidateTerms(
      request.PrimaryResidentId,
      request.ResidentIds,
      request.StartDate,
      request.EndDate,
      request.MonthlyRent,
      request.DueDay,
      request.DepositAmount,
      request.AdjustmentIndex,
      request.AdjustmentIntervalMonths))
    {
      yield return failure;
    }
  }

  private static IEnumerable<ValidationFailure> ValidateMutationRequest(ContractUpdateRequestDto request) =>
    ValidateTerms(
      request.PrimaryResidentId,
      request.ResidentIds,
      request.StartDate,
      request.EndDate,
      request.MonthlyRent,
      request.DueDay,
      request.DepositAmount,
      request.AdjustmentIndex,
      request.AdjustmentIntervalMonths);

  private static IEnumerable<ValidationFailure> ValidateTerms(
    Guid primaryResidentId,
    IReadOnlyList<Guid>? residentIds,
    DateOnly startDate,
    DateOnly? endDate,
    ContractMoneyDto? monthlyRent,
    int dueDay,
    ContractMoneyDto? depositAmount,
    string adjustmentIndex,
    int adjustmentIntervalMonths)
  {
    if (primaryResidentId == Guid.Empty)
    {
      yield return new ValidationFailure(nameof(primaryResidentId), ValidationMessageKeys.InvalidId);
    }

    if (residentIds is null || residentIds.Count == 0)
    {
      yield return new ValidationFailure(nameof(residentIds), ValidationMessageKeys.Required);
    }
    else if (residentIds.Any(id => id == Guid.Empty))
    {
      yield return new ValidationFailure(nameof(residentIds), ValidationMessageKeys.InvalidId);
    }

    if (endDate.HasValue && endDate.Value < startDate)
    {
      yield return new ValidationFailure(nameof(endDate), "validation.dateRange");
    }

    if (monthlyRent is null || monthlyRent.Amount < 0 || string.IsNullOrWhiteSpace(monthlyRent.Currency))
    {
      yield return new ValidationFailure(nameof(monthlyRent), "validation.money");
    }

    if (depositAmount is not null && depositAmount.Amount < 0)
    {
      yield return new ValidationFailure(nameof(depositAmount), "validation.money");
    }

    if (dueDay is < 1 or > 31)
    {
      yield return new ValidationFailure(nameof(dueDay), "validation.dueDay");
    }

    if (!ContractCatalog.TryParseAdjustmentIndex(adjustmentIndex, out _))
    {
      yield return new ValidationFailure(nameof(adjustmentIndex), "validation.adjustmentIndex");
    }

    if (adjustmentIntervalMonths < 0)
    {
      yield return new ValidationFailure(nameof(adjustmentIntervalMonths), "validation.adjustmentInterval");
    }
  }

  private static IEnumerable<ValidationFailure> ValidateId(Guid id)
  {
    if (id == Guid.Empty)
    {
      yield return new ValidationFailure(nameof(id), ValidationMessageKeys.InvalidId);
    }
  }

  private static Guid[] NormalizeResidentIds(IReadOnlyList<Guid> residentIds) =>
    residentIds.Distinct().ToArray();

  private static Money ToMoney(ContractMoneyDto money) => new(money.Amount, money.Currency);

  private static Money? ToMoneyOrNull(ContractMoneyDto? money) => money is null ? null : ToMoney(money);

  private static ContractMoneyDto ToMoneyDto(Money money) => new(money.Amount, money.Currency);

  private static ContractMoneyDto? ToMoneyDtoOrNull(Money? money) => money is null ? null : ToMoneyDto(money);

  private static ContractAdjustmentIndex ParseAdjustmentIndex(string value) =>
    ContractCatalog.TryParseAdjustmentIndex(value, out var index)
      ? index
      : throw new ArgumentException("Invalid contract adjustment index.", nameof(value));

  private DateOnly Today() => DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);

  private sealed record RelatedEntityValidationResult(
    IReadOnlyList<ValidationFailure> Errors,
    ContractPropertySnapshot? Property,
    IReadOnlyList<ContractResidentSnapshot> Residents);
}
