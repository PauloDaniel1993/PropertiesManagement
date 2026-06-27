using System.Globalization;
using Alsappan.Application.Common.Audit;
using Alsappan.Application.Common.Authorization;
using Alsappan.Application.Common.Contracts;
using Alsappan.Application.Common.Events;
using Alsappan.Application.Common.Results;
using Alsappan.Application.Common.Tenancy;
using Alsappan.Application.Common.Validation;
using Alsappan.Application.UtilityAccounts.Repositories;
using Alsappan.Domain.Common.Events;
using Alsappan.Domain.Common.Identifiers;
using Alsappan.Domain.Common.ValueObjects;
using Alsappan.Domain.UtilityAccounts;

namespace Alsappan.Application.UtilityAccounts;

public sealed class UtilityAccountService : IUtilityAccountService
{
  private readonly IUtilityAccountRepository utilityAccountRepository;
  private readonly IPermissionService permissionService;
  private readonly IActiveOrganizationContextResolver activeOrganizationContextResolver;
  private readonly IAuditWriter auditWriter;
  private readonly IModuleEventOutboxWriter outboxWriter;
  private readonly TimeProvider timeProvider;

  public UtilityAccountService(
    IUtilityAccountRepository utilityAccountRepository,
    IPermissionService permissionService,
    IActiveOrganizationContextResolver activeOrganizationContextResolver,
    IAuditWriter auditWriter,
    IModuleEventOutboxWriter outboxWriter,
    TimeProvider timeProvider)
  {
    this.utilityAccountRepository = utilityAccountRepository ??
      throw new ArgumentNullException(nameof(utilityAccountRepository));
    this.permissionService = permissionService ?? throw new ArgumentNullException(nameof(permissionService));
    this.activeOrganizationContextResolver = activeOrganizationContextResolver ??
      throw new ArgumentNullException(nameof(activeOrganizationContextResolver));
    this.auditWriter = auditWriter ?? throw new ArgumentNullException(nameof(auditWriter));
    this.outboxWriter = outboxWriter ?? throw new ArgumentNullException(nameof(outboxWriter));
    this.timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
  }

  public async Task<ApplicationOperationResult<PagedResultDto<UtilityAccountListItemDto>>> ListAsync(
    UtilityAccountListRequestDto request,
    CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(request);

    var context = await AuthorizeContextAsync(PermissionCodes.Read(PermissionModules.UtilityAccounts), cancellationToken)
      .ConfigureAwait(false);
    if (context is null)
    {
      return ApplicationOperationResult<PagedResultDto<UtilityAccountListItemDto>>.Failed(
        ApplicationOperationFailure.Forbidden);
    }

    var today = Today();
    var page = await utilityAccountRepository.ListAsync(request, context.OrganizationId, today, cancellationToken)
      .ConfigureAwait(false);
    var items = page.Items.Select(snapshot => ToListItem(snapshot, request.Locale, today)).ToArray();

    return ApplicationOperationResult<PagedResultDto<UtilityAccountListItemDto>>.Success(
      new PagedResultDto<UtilityAccountListItemDto>(items, page.Page, page.PageSize, page.TotalItems));
  }

  public async Task<ApplicationOperationResult<UtilityAccountDetailDto>> GetAsync(
    Guid id,
    string? locale = null,
    CancellationToken cancellationToken = default)
  {
    var validation = ValidateId(id).ToList();
    if (validation.Count > 0)
    {
      return ApplicationOperationResult<UtilityAccountDetailDto>.Invalid(validation);
    }

    var context = await AuthorizeContextAsync(PermissionCodes.Read(PermissionModules.UtilityAccounts), cancellationToken)
      .ConfigureAwait(false);
    if (context is null)
    {
      return ApplicationOperationResult<UtilityAccountDetailDto>.Failed(ApplicationOperationFailure.Forbidden);
    }

    var snapshot = await utilityAccountRepository.FindSnapshotAsync(
        new EntityId(id),
        context.OrganizationId,
        includeArchived: true,
        cancellationToken)
      .ConfigureAwait(false);

    return snapshot is null
      ? ApplicationOperationResult<UtilityAccountDetailDto>.Failed(ApplicationOperationFailure.NotFound)
      : ApplicationOperationResult<UtilityAccountDetailDto>.Success(ToDetail(snapshot, locale, Today()));
  }

  public async Task<ApplicationOperationResult<UtilityAccountDetailDto>> CreateAsync(
    UtilityAccountCreateRequestDto request,
    string? locale = null,
    CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(request);

    var validation = ValidateUtilityRequest(request).ToList();
    if (validation.Count > 0)
    {
      return ApplicationOperationResult<UtilityAccountDetailDto>.Invalid(validation);
    }

    var context = await AuthorizeContextAsync(PermissionCodes.Write(PermissionModules.UtilityAccounts), cancellationToken)
      .ConfigureAwait(false);
    if (context is null)
    {
      return ApplicationOperationResult<UtilityAccountDetailDto>.Failed(ApplicationOperationFailure.Forbidden);
    }

    var related = await ResolveRelatedEntitiesAsync(request, context.OrganizationId, cancellationToken)
      .ConfigureAwait(false);
    validation.AddRange(related.Errors);
    if (request.BillDocumentId.HasValue &&
      !await utilityAccountRepository.DocumentExistsAsync(
          new EntityId(request.BillDocumentId.Value),
          context.OrganizationId,
          cancellationToken)
        .ConfigureAwait(false))
    {
      validation.Add(new ValidationFailure(nameof(request.BillDocumentId), "validation.document"));
    }

    if (validation.Count > 0)
    {
      return ApplicationOperationResult<UtilityAccountDetailDto>.Invalid(validation);
    }

    if (!UtilityAccountCatalog.TryParseType(request.Type, out var type) ||
      !UtilityAccountCatalog.TryParseResponsibility(request.Responsibility, out var responsibility))
    {
      return ApplicationOperationResult<UtilityAccountDetailDto>.Invalid(ValidateUtilityRequest(request).ToList());
    }

    var now = timeProvider.GetUtcNow();
    var utilityAccount = UtilityAccount.Create(
      EntityId.New(),
      context.OrganizationId,
      related.PropertyId,
      related.ContractId,
      related.ResidentId,
      type,
      responsibility,
      request.Title,
      request.Description,
      request.BillingPeriodStart,
      request.BillingPeriodEnd,
      request.DueDate,
      ToMoney(request.Amount),
      request.Notes,
      related.Property?.Name,
      related.Contract?.DisplayName,
      related.Resident?.Name,
      now,
      context.UserId);
    if (request.BillDocumentId.HasValue)
    {
      utilityAccount.LinkDocument(
        new EntityId(request.BillDocumentId.Value),
        UtilityDocumentKind.Bill,
        "Conta",
        now,
        context.UserId);
    }

    await utilityAccountRepository.AddAsync(utilityAccount, cancellationToken).ConfigureAwait(false);
    var snapshot = await utilityAccountRepository.FindSnapshotAsync(
        utilityAccount.Id,
        context.OrganizationId,
        includeArchived: true,
        cancellationToken)
      .ConfigureAwait(false);
    await WriteMutationSideEffectsAsync("utility-account.created", utilityAccount, context, cancellationToken)
      .ConfigureAwait(false);

    return ApplicationOperationResult<UtilityAccountDetailDto>.Success(ToDetail(snapshot!, locale, Today()));
  }

  public async Task<ApplicationOperationResult<UtilityAccountDetailDto>> UpdateAsync(
    Guid id,
    UtilityAccountUpdateRequestDto request,
    string? locale = null,
    CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(request);

    var validation = ValidateId(id).Concat(ValidateUtilityRequest(request)).ToList();
    if (validation.Count > 0)
    {
      return ApplicationOperationResult<UtilityAccountDetailDto>.Invalid(validation);
    }

    var context = await AuthorizeContextAsync(PermissionCodes.Write(PermissionModules.UtilityAccounts), cancellationToken)
      .ConfigureAwait(false);
    if (context is null)
    {
      return ApplicationOperationResult<UtilityAccountDetailDto>.Failed(ApplicationOperationFailure.Forbidden);
    }

    var utilityAccount = await utilityAccountRepository.FindAsync(new EntityId(id), context.OrganizationId, false, cancellationToken)
      .ConfigureAwait(false);
    if (utilityAccount is null)
    {
      return ApplicationOperationResult<UtilityAccountDetailDto>.Failed(ApplicationOperationFailure.NotFound);
    }

    if (!string.IsNullOrWhiteSpace(request.ConcurrencyToken) &&
      !string.Equals(utilityAccount.ConcurrencyToken.Value, request.ConcurrencyToken, StringComparison.Ordinal))
    {
      return ApplicationOperationResult<UtilityAccountDetailDto>.Failed(ApplicationOperationFailure.Conflict);
    }

    var related = await ResolveRelatedEntitiesAsync(request, context.OrganizationId, cancellationToken)
      .ConfigureAwait(false);
    validation.AddRange(related.Errors);
    if (request.BillDocumentId.HasValue &&
      !await utilityAccountRepository.DocumentExistsAsync(
          new EntityId(request.BillDocumentId.Value),
          context.OrganizationId,
          cancellationToken)
        .ConfigureAwait(false))
    {
      validation.Add(new ValidationFailure(nameof(request.BillDocumentId), "validation.document"));
    }

    if (validation.Count > 0)
    {
      return ApplicationOperationResult<UtilityAccountDetailDto>.Invalid(validation);
    }

    if (!UtilityAccountCatalog.TryParseType(request.Type, out var type) ||
      !UtilityAccountCatalog.TryParseResponsibility(request.Responsibility, out var responsibility))
    {
      return ApplicationOperationResult<UtilityAccountDetailDto>.Invalid(ValidateUtilityRequest(request).ToList());
    }

    try
    {
      var now = timeProvider.GetUtcNow();
      utilityAccount.Update(
        related.PropertyId,
        related.ContractId,
        related.ResidentId,
        type,
        responsibility,
        request.Title,
        request.Description,
        request.BillingPeriodStart,
        request.BillingPeriodEnd,
        request.DueDate,
        ToMoney(request.Amount),
        request.Notes,
        related.Property?.Name,
        related.Contract?.DisplayName,
        related.Resident?.Name,
        now,
        context.UserId);
      if (request.BillDocumentId.HasValue)
      {
        utilityAccount.LinkDocument(
          new EntityId(request.BillDocumentId.Value),
          UtilityDocumentKind.Bill,
          "Conta",
          now,
          context.UserId);
      }
    }
    catch (InvalidOperationException)
    {
      return ApplicationOperationResult<UtilityAccountDetailDto>.Failed(ApplicationOperationFailure.Conflict);
    }

    await utilityAccountRepository.UpdateAsync(utilityAccount, cancellationToken).ConfigureAwait(false);
    var snapshot = await utilityAccountRepository.FindSnapshotAsync(utilityAccount.Id, context.OrganizationId, true, cancellationToken)
      .ConfigureAwait(false);
    await WriteMutationSideEffectsAsync("utility-account.updated", utilityAccount, context, cancellationToken)
      .ConfigureAwait(false);

    return ApplicationOperationResult<UtilityAccountDetailDto>.Success(ToDetail(snapshot!, locale, Today()));
  }

  public async Task<ApplicationOperationResult<UtilityAccountDetailDto>> MarkPaidAsync(
    Guid id,
    UtilityMarkPaidRequestDto request,
    string? locale = null,
    CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(request);

    var validation = ValidateId(id).Concat(ValidateMarkPaidRequest(request)).ToList();
    if (validation.Count > 0)
    {
      return ApplicationOperationResult<UtilityAccountDetailDto>.Invalid(validation);
    }

    var context = await AuthorizeContextAsync(PermissionCodes.Manage(PermissionModules.UtilityAccounts), cancellationToken)
      .ConfigureAwait(false);
    if (context is null)
    {
      return ApplicationOperationResult<UtilityAccountDetailDto>.Failed(ApplicationOperationFailure.Forbidden);
    }

    var utilityAccount = await utilityAccountRepository.FindAsync(new EntityId(id), context.OrganizationId, false, cancellationToken)
      .ConfigureAwait(false);
    if (utilityAccount is null)
    {
      return ApplicationOperationResult<UtilityAccountDetailDto>.Failed(ApplicationOperationFailure.NotFound);
    }

    if (request.ReceiptDocumentId.HasValue &&
      !await utilityAccountRepository.DocumentExistsAsync(
          new EntityId(request.ReceiptDocumentId.Value),
          context.OrganizationId,
          cancellationToken)
        .ConfigureAwait(false))
    {
      return ApplicationOperationResult<UtilityAccountDetailDto>.Invalid(
        [new ValidationFailure(nameof(request.ReceiptDocumentId), "validation.document")]);
    }

    try
    {
      utilityAccount.MarkPaid(
        ToMoney(request.Amount),
        request.PaidOn,
        request.PaymentMethod,
        request.BankReference,
        ToEntityIdOrNull(request.ReceiptDocumentId),
        request.Notes,
        timeProvider.GetUtcNow(),
        context.UserId);
    }
    catch (ArgumentOutOfRangeException)
    {
      return ApplicationOperationResult<UtilityAccountDetailDto>.Failed(
        ApplicationOperationFailure.Conflict,
        new Dictionary<string, string[]> { ["amount"] = ["validation.utilityBalance"] });
    }
    catch (InvalidOperationException)
    {
      return ApplicationOperationResult<UtilityAccountDetailDto>.Failed(ApplicationOperationFailure.Conflict);
    }

    await utilityAccountRepository.UpdateAsync(utilityAccount, cancellationToken).ConfigureAwait(false);
    var snapshot = await utilityAccountRepository.FindSnapshotAsync(utilityAccount.Id, context.OrganizationId, true, cancellationToken)
      .ConfigureAwait(false);
    await WriteMutationSideEffectsAsync("utility-account.paid", utilityAccount, context, cancellationToken)
      .ConfigureAwait(false);

    return ApplicationOperationResult<UtilityAccountDetailDto>.Success(ToDetail(snapshot!, locale, Today()));
  }

  public async Task<ApplicationOperationResult<UtilityAccountDetailDto>> CancelAsync(
    Guid id,
    UtilityLifecycleRequestDto request,
    string? locale = null,
    CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(request);

    return await MutateLifecycleAsync(
        id,
        PermissionCodes.Manage(PermissionModules.UtilityAccounts),
        "utility-account.cancelled",
        locale,
        (utilityAccount, context) =>
        {
          utilityAccount.Cancel(timeProvider.GetUtcNow(), context.UserId, request.Notes);
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

    var context = await AuthorizeContextAsync(PermissionCodes.Archive(PermissionModules.UtilityAccounts), cancellationToken)
      .ConfigureAwait(false);
    if (context is null)
    {
      return ApplicationOperationResult.Failed(ApplicationOperationFailure.Forbidden);
    }

    var utilityAccount = await utilityAccountRepository.FindAsync(new EntityId(id), context.OrganizationId, false, cancellationToken)
      .ConfigureAwait(false);
    if (utilityAccount is null)
    {
      return ApplicationOperationResult.Failed(ApplicationOperationFailure.NotFound);
    }

    utilityAccount.Archive(timeProvider.GetUtcNow(), context.UserId);
    await utilityAccountRepository.UpdateAsync(utilityAccount, cancellationToken).ConfigureAwait(false);
    await WriteMutationSideEffectsAsync("utility-account.archived", utilityAccount, context, cancellationToken)
      .ConfigureAwait(false);

    return ApplicationOperationResult.Success();
  }

  public async Task<ApplicationOperationResult<UtilityAccountDetailDto>> RestoreAsync(
    Guid id,
    string? locale = null,
    CancellationToken cancellationToken = default) =>
    await MutateLifecycleAsync(
        id,
        PermissionCodes.Archive(PermissionModules.UtilityAccounts),
        "utility-account.restored",
        locale,
        (utilityAccount, context) =>
        {
          utilityAccount.Restore(timeProvider.GetUtcNow(), context.UserId);
          return null;
        },
        cancellationToken,
        includeArchived: true)
      .ConfigureAwait(false);

  public Task<IReadOnlyList<StatusLabelDto>> GetStatusOptionsAsync(
    string? locale = null,
    CancellationToken cancellationToken = default)
  {
    cancellationToken.ThrowIfCancellationRequested();
    return Task.FromResult(UtilityAccountCatalog.GetStatusOptions(locale));
  }

  public Task<IReadOnlyList<StatusLabelDto>> GetTypeOptionsAsync(
    string? locale = null,
    CancellationToken cancellationToken = default)
  {
    cancellationToken.ThrowIfCancellationRequested();
    return Task.FromResult(UtilityAccountCatalog.GetTypeOptions(locale));
  }

  public Task<IReadOnlyList<StatusLabelDto>> GetResponsibilityOptionsAsync(
    string? locale = null,
    CancellationToken cancellationToken = default)
  {
    cancellationToken.ThrowIfCancellationRequested();
    return Task.FromResult(UtilityAccountCatalog.GetResponsibilityOptions(locale));
  }

  private async Task<ApplicationOperationResult<UtilityAccountDetailDto>> MutateLifecycleAsync(
    Guid id,
    string permissionCode,
    string eventName,
    string? locale,
    Func<UtilityAccount, ActiveOrganizationContext, ApplicationOperationResult<UtilityAccountDetailDto>?> mutate,
    CancellationToken cancellationToken,
    bool includeArchived = false)
  {
    var validation = ValidateId(id).ToList();
    if (validation.Count > 0)
    {
      return ApplicationOperationResult<UtilityAccountDetailDto>.Invalid(validation);
    }

    var context = await AuthorizeContextAsync(permissionCode, cancellationToken).ConfigureAwait(false);
    if (context is null)
    {
      return ApplicationOperationResult<UtilityAccountDetailDto>.Failed(ApplicationOperationFailure.Forbidden);
    }

    var utilityAccount = await utilityAccountRepository.FindAsync(
        new EntityId(id),
        context.OrganizationId,
        includeArchived,
        cancellationToken)
      .ConfigureAwait(false);
    if (utilityAccount is null)
    {
      return ApplicationOperationResult<UtilityAccountDetailDto>.Failed(ApplicationOperationFailure.NotFound);
    }

    try
    {
      var mutationResult = mutate(utilityAccount, context);
      if (mutationResult is not null)
      {
        return mutationResult;
      }
    }
    catch (InvalidOperationException)
    {
      return ApplicationOperationResult<UtilityAccountDetailDto>.Failed(ApplicationOperationFailure.Conflict);
    }

    await utilityAccountRepository.UpdateAsync(utilityAccount, cancellationToken).ConfigureAwait(false);
    var snapshot = await utilityAccountRepository.FindSnapshotAsync(utilityAccount.Id, context.OrganizationId, true, cancellationToken)
      .ConfigureAwait(false);
    await WriteMutationSideEffectsAsync(eventName, utilityAccount, context, cancellationToken).ConfigureAwait(false);

    return ApplicationOperationResult<UtilityAccountDetailDto>.Success(ToDetail(snapshot!, locale, Today()));
  }

  private async Task<ResolvedUtilityEntities> ResolveRelatedEntitiesAsync(
    UtilityAccountCreateRequestDto request,
    OrganizationId organizationId,
    CancellationToken cancellationToken) =>
    await ResolveRelatedEntitiesAsync(
        request.ContractId,
        request.PropertyId,
        request.ResidentId,
        organizationId,
        cancellationToken)
      .ConfigureAwait(false);

  private async Task<ResolvedUtilityEntities> ResolveRelatedEntitiesAsync(
    UtilityAccountUpdateRequestDto request,
    OrganizationId organizationId,
    CancellationToken cancellationToken) =>
    await ResolveRelatedEntitiesAsync(
        request.ContractId,
        request.PropertyId,
        request.ResidentId,
        organizationId,
        cancellationToken)
      .ConfigureAwait(false);

  private async Task<ResolvedUtilityEntities> ResolveRelatedEntitiesAsync(
    Guid? contractIdValue,
    Guid? propertyIdValue,
    Guid? residentIdValue,
    OrganizationId organizationId,
    CancellationToken cancellationToken)
  {
    var errors = new List<ValidationFailure>();
    UtilityContractSnapshot? contract = null;
    UtilityPropertySnapshot? property = null;
    UtilityResidentSnapshot? resident = null;
    var contractId = ToEntityIdOrNull(contractIdValue);
    var propertyId = ToEntityIdOrNull(propertyIdValue);
    var residentId = ToEntityIdOrNull(residentIdValue);

    if (contractId.HasValue)
    {
      contract = await utilityAccountRepository.GetContractSnapshotAsync(
          contractId.Value,
          organizationId,
          cancellationToken)
        .ConfigureAwait(false);
      if (contract is null)
      {
        errors.Add(new ValidationFailure("contractId", "validation.contract"));
      }
      else if (!contract.IsActive)
      {
        errors.Add(new ValidationFailure("contractId", "validation.contractStatus"));
      }
      else
      {
        if (propertyId.HasValue && propertyId.Value != contract.PropertyId)
        {
          errors.Add(new ValidationFailure("propertyId", "validation.propertyContractMismatch"));
        }

        if (residentId.HasValue && residentId.Value != contract.PrimaryResidentId)
        {
          errors.Add(new ValidationFailure("residentId", "validation.residentContractMismatch"));
        }

        propertyId ??= contract.PropertyId;
        residentId ??= contract.PrimaryResidentId;
      }
    }

    if (propertyId.HasValue)
    {
      property = await utilityAccountRepository.GetPropertySnapshotAsync(propertyId.Value, organizationId, cancellationToken)
        .ConfigureAwait(false);
      if (property is null)
      {
        errors.Add(new ValidationFailure("propertyId", "validation.property"));
      }
    }

    if (residentId.HasValue)
    {
      resident = await utilityAccountRepository.GetResidentSnapshotAsync(residentId.Value, organizationId, cancellationToken)
        .ConfigureAwait(false);
      if (resident is null)
      {
        errors.Add(new ValidationFailure("residentId", "validation.resident"));
      }
    }

    return new ResolvedUtilityEntities(
      errors,
      contractId,
      propertyId,
      residentId,
      property,
      contract,
      resident);
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
    UtilityAccount utilityAccount,
    ActiveOrganizationContext context,
    CancellationToken cancellationToken)
  {
    var subject = EntityReference.FromGuid("utilityAccount", utilityAccount.Id.Value, utilityAccount.Title);
    var actor = EventActor.User(context.UserId, context.User.DisplayName);
    var data = new Dictionary<string, string>(StringComparer.Ordinal)
    {
      ["status"] = UtilityAccountCatalog.ToStatusLabel(utilityAccount.GetEffectiveStatus(Today())).Code,
      ["amount"] = utilityAccount.Amount.Amount.ToString(CultureInfo.InvariantCulture),
      ["currency"] = utilityAccount.Amount.Currency,
      ["dueDate"] = utilityAccount.DueDate.ToString("O", CultureInfo.InvariantCulture),
      ["balance"] = utilityAccount.Balance().Amount.ToString(CultureInfo.InvariantCulture)
    };

    var related = new List<EntityReference>();
    if (utilityAccount.PropertyId.HasValue)
    {
      related.Add(EntityReference.FromGuid("property", utilityAccount.PropertyId.Value.Value));
    }

    if (utilityAccount.ContractId.HasValue)
    {
      related.Add(EntityReference.FromGuid("contract", utilityAccount.ContractId.Value.Value));
    }

    if (utilityAccount.ResidentId.HasValue)
    {
      related.Add(EntityReference.FromGuid("resident", utilityAccount.ResidentId.Value.Value));
    }

    var envelope = ModuleEventEnvelope.Create(
      context.OrganizationId,
      "utility-accounts",
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

  private static UtilityAccountListItemDto ToListItem(
    UtilityAccountSnapshot snapshot,
    string? locale,
    DateOnly today)
  {
    var utilityAccount = snapshot.UtilityAccount;
    var effectiveStatus = utilityAccount.GetEffectiveStatus(today);
    return new UtilityAccountListItemDto(
      utilityAccount.Id.Value,
      utilityAccount.Title,
      utilityAccount.Description,
      UtilityAccountCatalog.ToTypeLabel(utilityAccount.Type, locale),
      UtilityAccountCatalog.ToStatusLabel(effectiveStatus, locale),
      UtilityAccountCatalog.ToResponsibilityLabel(utilityAccount.Responsibility, locale),
      ToPropertySummary(snapshot.Property),
      ToContractSummary(snapshot.Contract),
      ToResidentSummary(snapshot.Resident),
      utilityAccount.BillingPeriodStart,
      utilityAccount.BillingPeriodEnd,
      utilityAccount.DueDate,
      ToMoneyDto(utilityAccount.Amount),
      ToMoneyDto(utilityAccount.PaidAmount),
      ToMoneyDto(utilityAccount.Balance()),
      utilityAccount.PaidOn,
      utilityAccount.PaymentMethod,
      utilityAccount.BankReference,
      effectiveStatus == UtilityAccountStatus.Overdue,
      utilityAccount.IsDeleted || effectiveStatus == UtilityAccountStatus.Archived,
      utilityAccount.CreatedAt,
      utilityAccount.UpdatedAt,
      utilityAccount.ConcurrencyToken.Value);
  }

  private static UtilityAccountDetailDto ToDetail(UtilityAccountSnapshot snapshot, string? locale, DateOnly today)
  {
    var listItem = ToListItem(snapshot, locale, today);
    var utilityAccount = snapshot.UtilityAccount;
    var id = Uri.EscapeDataString(utilityAccount.Id.Value.ToString("D"));
    var billDocuments = snapshot.Documents
      .Where(document => document.Kind == UtilityDocumentKind.Bill)
      .Select(document => ToDocumentDto(document, locale, id))
      .ToArray();
    var receiptDocuments = snapshot.Documents
      .Where(document => document.Kind == UtilityDocumentKind.Receipt)
      .Select(document => ToDocumentDto(document, locale, id))
      .ToArray();

    return new UtilityAccountDetailDto(
      listItem.Id,
      listItem.Title,
      listItem.Description,
      listItem.Type,
      listItem.Status,
      listItem.Responsibility,
      listItem.Property,
      listItem.Contract,
      listItem.Resident,
      listItem.BillingPeriodStart,
      listItem.BillingPeriodEnd,
      listItem.DueDate,
      listItem.Amount,
      listItem.PaidAmount,
      listItem.Balance,
      listItem.PaidOn,
      listItem.PaymentMethod,
      listItem.BankReference,
      utilityAccount.Notes,
      billDocuments,
      receiptDocuments,
      $"/timeline?entityType=utilityAccount&entityId={id}",
      $"/auditoria?entityType=utilityAccount&entityId={id}",
      utilityAccount.CreatedAt,
      utilityAccount.UpdatedAt,
      utilityAccount.DeletedAt,
      utilityAccount.ConcurrencyToken.Value);
  }

  private static UtilityDocumentDto ToDocumentDto(UtilityDocumentSnapshot document, string? locale, string id) =>
    new(
      document.DocumentId.Value,
      document.Kind == UtilityDocumentKind.Receipt ? "receipt" : "bill",
      UtilityAccountCatalog.GetDocumentKindLabel(document.Kind, locale),
      document.Label,
      $"/documentos?entityType=utility-account&entityId={id}");

  private static UtilityEntitySummaryDto? ToPropertySummary(UtilityPropertySnapshot? property) =>
    property is null
      ? null
      : new UtilityEntitySummaryDto(
        property.PropertyId.Value,
        property.Name,
        property.Location,
        $"/imoveis?id={property.PropertyId.Value:D}");

  private static UtilityEntitySummaryDto? ToContractSummary(UtilityContractSnapshot? contract) =>
    contract is null
      ? null
      : new UtilityEntitySummaryDto(
        contract.ContractId.Value,
        contract.DisplayName,
        $"{contract.PropertyName} - {contract.ResidentName}",
        $"/contratos?id={contract.ContractId.Value:D}");

  private static UtilityEntitySummaryDto? ToResidentSummary(UtilityResidentSnapshot? resident) =>
    resident is null
      ? null
      : new UtilityEntitySummaryDto(
        resident.ResidentId.Value,
        resident.Name,
        Route: $"/moradores?id={resident.ResidentId.Value:D}");

  private static IEnumerable<ValidationFailure> ValidateUtilityRequest(UtilityAccountCreateRequestDto request) =>
    ValidateUtilityFields(
      request.Title,
      request.Type,
      request.Responsibility,
      request.PropertyId,
      request.ContractId,
      request.ResidentId,
      request.BillingPeriodStart,
      request.BillingPeriodEnd,
      request.DueDate,
      request.Amount);

  private static IEnumerable<ValidationFailure> ValidateUtilityRequest(UtilityAccountUpdateRequestDto request) =>
    ValidateUtilityFields(
      request.Title,
      request.Type,
      request.Responsibility,
      request.PropertyId,
      request.ContractId,
      request.ResidentId,
      request.BillingPeriodStart,
      request.BillingPeriodEnd,
      request.DueDate,
      request.Amount);

  private static IEnumerable<ValidationFailure> ValidateUtilityFields(
    string title,
    string type,
    string responsibility,
    Guid? propertyId,
    Guid? contractId,
    Guid? residentId,
    DateOnly billingPeriodStart,
    DateOnly billingPeriodEnd,
    DateOnly dueDate,
    UtilityMoneyDto amount)
  {
    var hasContractId = HasNonEmptyId(contractId);
    var hasPropertyId = HasNonEmptyId(propertyId);
    var hasResidentId = HasNonEmptyId(residentId);

    if (string.IsNullOrWhiteSpace(title))
    {
      yield return new ValidationFailure(nameof(title), ValidationMessageKeys.Required);
    }

    if (!UtilityAccountCatalog.TryParseType(type, out _))
    {
      yield return new ValidationFailure(nameof(type), "validation.utilityType");
    }

    if (!UtilityAccountCatalog.TryParseResponsibility(responsibility, out var parsedResponsibility))
    {
      yield return new ValidationFailure(nameof(responsibility), "validation.utilityResponsibility");
    }

    if (billingPeriodStart == default)
    {
      yield return new ValidationFailure(nameof(billingPeriodStart), ValidationMessageKeys.Required);
    }

    if (billingPeriodEnd == default || billingPeriodEnd < billingPeriodStart)
    {
      yield return new ValidationFailure(nameof(billingPeriodEnd), "validation.billingPeriod");
    }

    if (dueDate == default)
    {
      yield return new ValidationFailure(nameof(dueDate), ValidationMessageKeys.Required);
    }

    foreach (var failure in ValidateMoney(amount, nameof(amount), requirePositive: true))
    {
      yield return failure;
    }

    if (parsedResponsibility == UtilityResponsibility.Contract && !hasContractId)
    {
      yield return new ValidationFailure(nameof(contractId), "validation.contract");
    }

    if (parsedResponsibility == UtilityResponsibility.Property && !hasPropertyId && !hasContractId)
    {
      yield return new ValidationFailure(nameof(propertyId), "validation.property");
    }

    if (parsedResponsibility == UtilityResponsibility.Resident && !hasResidentId && !hasContractId)
    {
      yield return new ValidationFailure(nameof(residentId), "validation.resident");
    }
  }

  private static IEnumerable<ValidationFailure> ValidateMarkPaidRequest(UtilityMarkPaidRequestDto request)
  {
    foreach (var failure in ValidateMoney(request.Amount, nameof(request.Amount), requirePositive: true))
    {
      yield return failure;
    }

    if (request.PaidOn == default)
    {
      yield return new ValidationFailure(nameof(request.PaidOn), ValidationMessageKeys.Required);
    }
  }

  private static IEnumerable<ValidationFailure> ValidateMoney(
    UtilityMoneyDto? money,
    string propertyName,
    bool requirePositive)
  {
    if (money is null)
    {
      yield return new ValidationFailure(propertyName, ValidationMessageKeys.Required);
      yield break;
    }

    if (requirePositive && money.Amount <= 0)
    {
      yield return new ValidationFailure(propertyName, "validation.moneyPositive");
    }

    if (string.IsNullOrWhiteSpace(money.Currency) || money.Currency.Trim().Length != 3)
    {
      yield return new ValidationFailure($"{propertyName}.currency", "validation.currency");
    }
  }

  private static IEnumerable<ValidationFailure> ValidateId(Guid id, string propertyName = "id")
  {
    if (id == Guid.Empty)
    {
      yield return new ValidationFailure(propertyName, ValidationMessageKeys.Required);
    }
  }

  private static Money ToMoney(UtilityMoneyDto money) => new(money.Amount, money.Currency);

  private static EntityId? ToEntityIdOrNull(Guid? id) =>
    id.HasValue && id.Value != Guid.Empty ? new EntityId(id.Value) : null;

  private static bool HasNonEmptyId(Guid? id) => id.HasValue && id.Value != Guid.Empty;

  private static UtilityMoneyDto ToMoneyDto(Money money) => new(money.Amount, money.Currency);

  private DateOnly Today() => DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);

  private sealed record ResolvedUtilityEntities(
    IReadOnlyList<ValidationFailure> Errors,
    EntityId? ContractId,
    EntityId? PropertyId,
    EntityId? ResidentId,
    UtilityPropertySnapshot? Property,
    UtilityContractSnapshot? Contract,
    UtilityResidentSnapshot? Resident);
}
