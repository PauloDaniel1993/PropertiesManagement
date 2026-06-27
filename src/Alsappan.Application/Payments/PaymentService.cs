using Alsappan.Application.Common.Audit;
using Alsappan.Application.Common.Authorization;
using Alsappan.Application.Common.Contracts;
using Alsappan.Application.Common.Events;
using Alsappan.Application.Common.Results;
using Alsappan.Application.Common.Tenancy;
using Alsappan.Application.Common.Validation;
using Alsappan.Application.Payments.Providers;
using Alsappan.Application.Payments.Repositories;
using Alsappan.Domain.Common.Events;
using Alsappan.Domain.Common.Identifiers;
using Alsappan.Domain.Common.ValueObjects;
using Alsappan.Domain.Payments;

namespace Alsappan.Application.Payments;

public sealed class PaymentService : IPaymentService
{
  private readonly IPaymentRepository paymentRepository;
  private readonly Dictionary<string, IPaymentInstructionProvider> providers;
  private readonly IPermissionService permissionService;
  private readonly IActiveOrganizationContextResolver activeOrganizationContextResolver;
  private readonly IAuditWriter auditWriter;
  private readonly IModuleEventOutboxWriter outboxWriter;
  private readonly TimeProvider timeProvider;

  public PaymentService(
    IPaymentRepository paymentRepository,
    IEnumerable<IPaymentInstructionProvider> providers,
    IPermissionService permissionService,
    IActiveOrganizationContextResolver activeOrganizationContextResolver,
    IAuditWriter auditWriter,
    IModuleEventOutboxWriter outboxWriter,
    TimeProvider? timeProvider = null)
  {
    this.paymentRepository = paymentRepository ?? throw new ArgumentNullException(nameof(paymentRepository));
    this.providers = (providers ?? throw new ArgumentNullException(nameof(providers)))
      .ToDictionary(provider => PaymentCode.NormalizeCode(provider.ProviderCode), StringComparer.Ordinal);
    this.permissionService = permissionService ?? throw new ArgumentNullException(nameof(permissionService));
    this.activeOrganizationContextResolver = activeOrganizationContextResolver
      ?? throw new ArgumentNullException(nameof(activeOrganizationContextResolver));
    this.auditWriter = auditWriter ?? throw new ArgumentNullException(nameof(auditWriter));
    this.outboxWriter = outboxWriter ?? throw new ArgumentNullException(nameof(outboxWriter));
    this.timeProvider = timeProvider ?? TimeProvider.System;
  }

  public async Task<ApplicationOperationResult<PagedResultDto<PaymentListItemDto>>> ListAsync(
    PaymentListRequestDto request,
    CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(request);

    var context = await AuthorizeContextAsync(PermissionCodes.Read(PermissionModules.Payments), cancellationToken)
      .ConfigureAwait(false);
    if (context is null)
    {
      return ApplicationOperationResult<PagedResultDto<PaymentListItemDto>>.Failed(ApplicationOperationFailure.Forbidden);
    }

    var today = Today();
    var page = await paymentRepository.ListAsync(request, context.OrganizationId, today, cancellationToken)
      .ConfigureAwait(false);
    var items = page.Items.Select(snapshot => ToListItem(snapshot, request.Locale, today)).ToArray();

    return ApplicationOperationResult<PagedResultDto<PaymentListItemDto>>.Success(
      new PagedResultDto<PaymentListItemDto>(items, page.Page, page.PageSize, page.TotalItems));
  }

  public async Task<ApplicationOperationResult<PaymentDetailDto>> GetAsync(
    Guid id,
    string? locale = null,
    CancellationToken cancellationToken = default)
  {
    var validation = ValidateId(id).ToList();
    if (validation.Count > 0)
    {
      return ApplicationOperationResult<PaymentDetailDto>.Invalid(validation);
    }

    var context = await AuthorizeContextAsync(PermissionCodes.Read(PermissionModules.Payments), cancellationToken)
      .ConfigureAwait(false);
    if (context is null)
    {
      return ApplicationOperationResult<PaymentDetailDto>.Failed(ApplicationOperationFailure.Forbidden);
    }

    var snapshot = await paymentRepository.FindSnapshotAsync(
        new EntityId(id),
        context.OrganizationId,
        includeArchived: true,
        cancellationToken)
      .ConfigureAwait(false);

    return snapshot is null
      ? ApplicationOperationResult<PaymentDetailDto>.Failed(ApplicationOperationFailure.NotFound)
      : ApplicationOperationResult<PaymentDetailDto>.Success(ToDetail(snapshot, locale, Today()));
  }

  public async Task<ApplicationOperationResult<PaymentDetailDto>> CreateAsync(
    PaymentCreateRequestDto request,
    string? locale = null,
    CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(request);

    var context = await AuthorizeContextAsync(PermissionCodes.Write(PermissionModules.Payments), cancellationToken)
      .ConfigureAwait(false);
    if (context is null)
    {
      return ApplicationOperationResult<PaymentDetailDto>.Failed(ApplicationOperationFailure.Forbidden);
    }

    var validation = ValidateChargeRequest(request).ToList();
    if (validation.Count > 0)
    {
      return ApplicationOperationResult<PaymentDetailDto>.Invalid(validation);
    }

    var related = await ResolveRelatedEntitiesAsync(request, context.OrganizationId, cancellationToken)
      .ConfigureAwait(false);
    if (related.Errors.Count > 0)
    {
      return ApplicationOperationResult<PaymentDetailDto>.Invalid(related.Errors);
    }

    var now = timeProvider.GetUtcNow();
    var charge = PaymentCharge.Create(
      EntityId.New(),
      context.OrganizationId,
      related.ContractId,
      related.PropertyId,
      related.ResidentId,
      ToEntityIdOrNull(request.UtilityAccountId),
      request.Title,
      request.Description,
      request.DueDate,
      ToMoney(request.Amount),
      ToMoneyOrNull(request.DiscountAmount),
      ToMoneyOrNull(request.PenaltyAmount),
      ParseMethod(request.PreferredMethod),
      ParseReconciliationStatus(request.ReconciliationStatus),
      request.Notes,
      related.Contract?.DisplayName,
      related.Property?.Name,
      related.Resident?.Name,
      now,
      context.UserId);

    await paymentRepository.AddAsync(charge, cancellationToken).ConfigureAwait(false);
    var snapshot = await paymentRepository.FindSnapshotAsync(charge.Id, context.OrganizationId, true, cancellationToken)
      .ConfigureAwait(false);
    await WriteMutationSideEffectsAsync("payment.created", charge, context, cancellationToken).ConfigureAwait(false);

    return ApplicationOperationResult<PaymentDetailDto>.Success(ToDetail(snapshot!, locale, Today()));
  }

  public async Task<ApplicationOperationResult<PaymentDetailDto>> UpdateAsync(
    Guid id,
    PaymentUpdateRequestDto request,
    string? locale = null,
    CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(request);

    var validation = ValidateId(id).Concat(ValidateChargeRequest(request)).ToList();
    if (validation.Count > 0)
    {
      return ApplicationOperationResult<PaymentDetailDto>.Invalid(validation);
    }

    var context = await AuthorizeContextAsync(PermissionCodes.Write(PermissionModules.Payments), cancellationToken)
      .ConfigureAwait(false);
    if (context is null)
    {
      return ApplicationOperationResult<PaymentDetailDto>.Failed(ApplicationOperationFailure.Forbidden);
    }

    var charge = await paymentRepository.FindAsync(
        new EntityId(id),
        context.OrganizationId,
        includeArchived: false,
        cancellationToken)
      .ConfigureAwait(false);
    if (charge is null)
    {
      return ApplicationOperationResult<PaymentDetailDto>.Failed(ApplicationOperationFailure.NotFound);
    }

    var concurrency = EnsureCurrentConcurrencyToken(charge, request.ConcurrencyToken);
    if (concurrency is not null)
    {
      return concurrency;
    }

    if (CalculateGrossAmount(request.Amount, request.DiscountAmount, request.PenaltyAmount).Amount <
      charge.SettledAmount().Amount)
    {
      return ApplicationOperationResult<PaymentDetailDto>.Failed(
        ApplicationOperationFailure.Conflict,
        new Dictionary<string, string[]> { ["amount"] = ["validation.paymentAmountBelowSettled"] });
    }

    var related = await ResolveRelatedEntitiesAsync(request, context.OrganizationId, cancellationToken)
      .ConfigureAwait(false);
    if (related.Errors.Count > 0)
    {
      return ApplicationOperationResult<PaymentDetailDto>.Invalid(related.Errors);
    }

    charge.Update(
      related.ContractId,
      related.PropertyId,
      related.ResidentId,
      ToEntityIdOrNull(request.UtilityAccountId),
      request.Title,
      request.Description,
      request.DueDate,
      ToMoney(request.Amount),
      ToMoneyOrNull(request.DiscountAmount),
      ToMoneyOrNull(request.PenaltyAmount),
      ParseMethod(request.PreferredMethod),
      ParseReconciliationStatus(request.ReconciliationStatus),
      request.Notes,
      related.Contract?.DisplayName,
      related.Property?.Name,
      related.Resident?.Name,
      timeProvider.GetUtcNow(),
      context.UserId);

    await paymentRepository.UpdateAsync(charge, cancellationToken).ConfigureAwait(false);
    var snapshot = await paymentRepository.FindSnapshotAsync(charge.Id, context.OrganizationId, true, cancellationToken)
      .ConfigureAwait(false);
    await WriteMutationSideEffectsAsync("payment.updated", charge, context, cancellationToken).ConfigureAwait(false);

    return ApplicationOperationResult<PaymentDetailDto>.Success(ToDetail(snapshot!, locale, Today()));
  }

  public async Task<ApplicationOperationResult<PaymentDetailDto>> RecordTransactionAsync(
    Guid id,
    PaymentTransactionRequestDto request,
    string? locale = null,
    CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(request);

    var validation = ValidateId(id).Concat(ValidateTransactionRequest(request)).ToList();
    if (validation.Count > 0)
    {
      return ApplicationOperationResult<PaymentDetailDto>.Invalid(validation);
    }

    var context = await AuthorizeContextAsync(PermissionCodes.Manage(PermissionModules.Payments), cancellationToken)
      .ConfigureAwait(false);
    if (context is null)
    {
      return ApplicationOperationResult<PaymentDetailDto>.Failed(ApplicationOperationFailure.Forbidden);
    }

    var charge = await paymentRepository.FindAsync(
        new EntityId(id),
        context.OrganizationId,
        includeArchived: false,
        cancellationToken)
      .ConfigureAwait(false);
    if (charge is null)
    {
      return ApplicationOperationResult<PaymentDetailDto>.Failed(ApplicationOperationFailure.NotFound);
    }

    if (request.ReceiptDocumentId.HasValue && request.ReceiptDocumentId.Value != Guid.Empty)
    {
      var exists = await paymentRepository.ReceiptDocumentExistsAsync(
          new EntityId(request.ReceiptDocumentId.Value),
          context.OrganizationId,
          cancellationToken)
        .ConfigureAwait(false);
      if (!exists)
      {
        return ApplicationOperationResult<PaymentDetailDto>.Invalid(
          [new ValidationFailure(nameof(request.ReceiptDocumentId), "validation.receiptDocument")]);
      }
    }

    var amount = ToMoney(request.Amount);
    if (!StringComparer.Ordinal.Equals(amount.Currency, charge.Amount.Currency) ||
      amount.Amount > charge.Balance().Amount)
    {
      return ApplicationOperationResult<PaymentDetailDto>.Failed(
        ApplicationOperationFailure.Conflict,
        new Dictionary<string, string[]> { ["amount"] = ["validation.paymentBalance"] });
    }

    var now = timeProvider.GetUtcNow();
    charge.RecordTransaction(
      EntityId.New(),
      amount,
      ParseMethod(request.Method),
      request.SettledOn,
      request.BankReference,
      request.ProviderCode,
      request.ProviderReference,
      ToEntityIdOrNull(request.ReceiptDocumentId),
      request.Notes,
      now,
      context.UserId);

    await paymentRepository.UpdateAsync(charge, cancellationToken).ConfigureAwait(false);
    var snapshot = await paymentRepository.FindSnapshotAsync(charge.Id, context.OrganizationId, true, cancellationToken)
      .ConfigureAwait(false);
    await WriteMutationSideEffectsAsync("payment.transaction-recorded", charge, context, cancellationToken)
      .ConfigureAwait(false);

    return ApplicationOperationResult<PaymentDetailDto>.Success(ToDetail(snapshot!, locale, Today()));
  }

  public async Task<ApplicationOperationResult<PaymentDetailDto>> ReverseTransactionAsync(
    Guid id,
    PaymentTransactionReversalRequestDto request,
    string? locale = null,
    CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(request);

    var validation = ValidateId(id).Concat(ValidateId(request.TransactionId, nameof(request.TransactionId))).ToList();
    if (validation.Count > 0)
    {
      return ApplicationOperationResult<PaymentDetailDto>.Invalid(validation);
    }

    var context = await AuthorizeContextAsync(PermissionCodes.Manage(PermissionModules.Payments), cancellationToken)
      .ConfigureAwait(false);
    if (context is null)
    {
      return ApplicationOperationResult<PaymentDetailDto>.Failed(ApplicationOperationFailure.Forbidden);
    }

    var charge = await paymentRepository.FindAsync(new EntityId(id), context.OrganizationId, false, cancellationToken)
      .ConfigureAwait(false);
    if (charge is null)
    {
      return ApplicationOperationResult<PaymentDetailDto>.Failed(ApplicationOperationFailure.NotFound);
    }

    charge.ReverseTransaction(
      new EntityId(request.TransactionId),
      timeProvider.GetUtcNow(),
      context.UserId,
      request.Notes);
    await paymentRepository.UpdateAsync(charge, cancellationToken).ConfigureAwait(false);
    var snapshot = await paymentRepository.FindSnapshotAsync(charge.Id, context.OrganizationId, true, cancellationToken)
      .ConfigureAwait(false);
    await WriteMutationSideEffectsAsync("payment.transaction-reversed", charge, context, cancellationToken)
      .ConfigureAwait(false);

    return ApplicationOperationResult<PaymentDetailDto>.Success(ToDetail(snapshot!, locale, Today()));
  }

  public async Task<ApplicationOperationResult<PaymentDetailDto>> CancelAsync(
    Guid id,
    PaymentLifecycleRequestDto request,
    string? locale = null,
    CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(request);
    var result = await MutateLifecycleAsync(
        id,
        PermissionCodes.Manage(PermissionModules.Payments),
        "payment.cancelled",
        locale,
        (charge, context) =>
        {
          charge.Cancel(timeProvider.GetUtcNow(), context.UserId, request.Notes);
          return null;
        },
        cancellationToken)
      .ConfigureAwait(false);

    return result;
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

    var context = await AuthorizeContextAsync(PermissionCodes.Archive(PermissionModules.Payments), cancellationToken)
      .ConfigureAwait(false);
    if (context is null)
    {
      return ApplicationOperationResult.Failed(ApplicationOperationFailure.Forbidden);
    }

    var charge = await paymentRepository.FindAsync(new EntityId(id), context.OrganizationId, false, cancellationToken)
      .ConfigureAwait(false);
    if (charge is null)
    {
      return ApplicationOperationResult.Failed(ApplicationOperationFailure.NotFound);
    }

    charge.Archive(timeProvider.GetUtcNow(), context.UserId);
    await paymentRepository.UpdateAsync(charge, cancellationToken).ConfigureAwait(false);
    await WriteMutationSideEffectsAsync("payment.archived", charge, context, cancellationToken)
      .ConfigureAwait(false);

    return ApplicationOperationResult.Success();
  }

  public async Task<ApplicationOperationResult<PaymentDetailDto>> RestoreAsync(
    Guid id,
    string? locale = null,
    CancellationToken cancellationToken = default)
  {
    var result = await MutateLifecycleAsync(
        id,
        PermissionCodes.Archive(PermissionModules.Payments),
        "payment.restored",
        locale,
        (charge, context) =>
        {
          charge.Restore(timeProvider.GetUtcNow(), context.UserId);
          return null;
        },
        cancellationToken,
        includeArchived: true)
      .ConfigureAwait(false);

    return result;
  }

  public async Task<ApplicationOperationResult<PaymentInstructionDto>> CreateInstructionAsync(
    Guid id,
    PaymentInstructionRequestDto request,
    string? locale = null,
    CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(request);

    var validation = ValidateId(id).ToList();
    if (validation.Count > 0)
    {
      return ApplicationOperationResult<PaymentInstructionDto>.Invalid(validation);
    }

    if (string.IsNullOrWhiteSpace(request.ProviderCode))
    {
      return ApplicationOperationResult<PaymentInstructionDto>.Invalid(
        [new ValidationFailure(nameof(request.ProviderCode), ValidationMessageKeys.Required)]);
    }

    var providerCode = PaymentCode.NormalizeCode(request.ProviderCode);
    if (!providers.TryGetValue(providerCode, out var provider))
    {
      return ApplicationOperationResult<PaymentInstructionDto>.Invalid(
        [new ValidationFailure(nameof(request.ProviderCode), "validation.paymentProvider")]);
    }

    var context = await AuthorizeContextAsync(PermissionCodes.Write(PermissionModules.Payments), cancellationToken)
      .ConfigureAwait(false);
    if (context is null)
    {
      return ApplicationOperationResult<PaymentInstructionDto>.Failed(ApplicationOperationFailure.Forbidden);
    }

    var snapshot = await paymentRepository.FindSnapshotAsync(
        new EntityId(id),
        context.OrganizationId,
        includeArchived: false,
        cancellationToken)
      .ConfigureAwait(false);
    if (snapshot is null)
    {
      return ApplicationOperationResult<PaymentInstructionDto>.Failed(ApplicationOperationFailure.NotFound);
    }

    var charge = snapshot.Charge;
    if (charge.Balance().Amount <= 0)
    {
      return ApplicationOperationResult<PaymentInstructionDto>.Failed(
        ApplicationOperationFailure.Conflict,
        new Dictionary<string, string[]> { ["status"] = ["validation.paymentAlreadySettled"] });
    }

    var instruction = await provider.CreateInstructionAsync(
        new PaymentProviderInstructionRequest(
          charge.Id.Value,
          provider.ProviderCode,
          provider.Kind,
          ToMoneyDto(charge.Balance()),
          charge.DueDate,
          BuildPayerSummary(snapshot),
          locale),
        cancellationToken)
      .ConfigureAwait(false);
    charge.SetProviderInstruction(
      instruction.ProviderCode,
      instruction.ProviderReference,
      System.Text.Json.JsonSerializer.Serialize(instruction.Metadata),
      timeProvider.GetUtcNow(),
      context.UserId);
    await paymentRepository.UpdateAsync(charge, cancellationToken).ConfigureAwait(false);
    await WriteMutationSideEffectsAsync("payment.instruction-created", charge, context, cancellationToken)
      .ConfigureAwait(false);

    return ApplicationOperationResult<PaymentInstructionDto>.Success(instruction);
  }

  public async Task<ApplicationOperationResult<PaymentDetailDto>> ApplyProviderEventAsync(
    PaymentProviderEventRequestDto request,
    string? locale = null,
    CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(request);

    var validation = ValidateProviderEvent(request).ToList();
    if (validation.Count > 0)
    {
      return ApplicationOperationResult<PaymentDetailDto>.Invalid(validation);
    }

    var context = await AuthorizeContextAsync(PermissionCodes.Manage(PermissionModules.Payments), cancellationToken)
      .ConfigureAwait(false);
    if (context is null)
    {
      return ApplicationOperationResult<PaymentDetailDto>.Failed(ApplicationOperationFailure.Forbidden);
    }

    var charge = await paymentRepository.FindByProviderReferenceAsync(
        PaymentCode.NormalizeCode(request.ProviderCode),
        request.ProviderReference,
        context.OrganizationId,
        cancellationToken)
      .ConfigureAwait(false);
    if (charge is null)
    {
      return ApplicationOperationResult<PaymentDetailDto>.Failed(ApplicationOperationFailure.NotFound);
    }

    if (!PaymentCatalog.TryGetProviderMethod(request.ProviderCode, out var method))
    {
      return ApplicationOperationResult<PaymentDetailDto>.Invalid(
        [new ValidationFailure(nameof(request.ProviderCode), "validation.paymentProvider")]);
    }

    var amount = request.Amount is null ? charge.Balance() : ToMoney(request.Amount);
    if (!StringComparer.Ordinal.Equals(amount.Currency, charge.Amount.Currency) ||
      amount.Amount <= 0 ||
      amount.Amount > charge.Balance().Amount)
    {
      return ApplicationOperationResult<PaymentDetailDto>.Failed(
        ApplicationOperationFailure.Conflict,
        new Dictionary<string, string[]> { ["amount"] = ["validation.paymentBalance"] });
    }

    charge.RecordTransaction(
      EntityId.New(),
      amount,
      method,
      request.SettledOn,
      bankReference: null,
      PaymentCode.NormalizeCode(request.ProviderCode),
      request.ProviderReference,
      receiptDocumentId: null,
      request.Notes,
      timeProvider.GetUtcNow(),
      context.UserId);
    await paymentRepository.UpdateAsync(charge, cancellationToken).ConfigureAwait(false);
    var snapshot = await paymentRepository.FindSnapshotAsync(charge.Id, context.OrganizationId, true, cancellationToken)
      .ConfigureAwait(false);
    await WriteMutationSideEffectsAsync("payment.provider-event-settled", charge, context, cancellationToken)
      .ConfigureAwait(false);

    return ApplicationOperationResult<PaymentDetailDto>.Success(ToDetail(snapshot!, locale, Today()));
  }

  public Task<IReadOnlyList<StatusLabelDto>> GetStatusOptionsAsync(
    string? locale = null,
    CancellationToken cancellationToken = default)
  {
    cancellationToken.ThrowIfCancellationRequested();
    return Task.FromResult(PaymentCatalog.GetStatusOptions(locale));
  }

  public Task<IReadOnlyList<SelectOptionDto>> GetMethodOptionsAsync(
    string? locale = null,
    CancellationToken cancellationToken = default)
  {
    cancellationToken.ThrowIfCancellationRequested();
    return Task.FromResult(PaymentCatalog.GetMethodOptions(locale));
  }

  public Task<IReadOnlyList<StatusLabelDto>> GetReconciliationStatusOptionsAsync(
    string? locale = null,
    CancellationToken cancellationToken = default)
  {
    cancellationToken.ThrowIfCancellationRequested();
    return Task.FromResult(PaymentCatalog.GetReconciliationStatusOptions(locale));
  }

  public Task<IReadOnlyList<SelectOptionDto>> GetProviderOptionsAsync(
    string? locale = null,
    CancellationToken cancellationToken = default)
  {
    cancellationToken.ThrowIfCancellationRequested();
    return Task.FromResult(PaymentCatalog.GetProviderOptions(locale));
  }

  private async Task<ApplicationOperationResult<PaymentDetailDto>> MutateLifecycleAsync(
    Guid id,
    string permissionCode,
    string eventName,
    string? locale,
    Func<PaymentCharge, ActiveOrganizationContext, ApplicationOperationResult<PaymentDetailDto>?> mutate,
    CancellationToken cancellationToken,
    bool includeArchived = false)
  {
    var validation = ValidateId(id).ToList();
    if (validation.Count > 0)
    {
      return ApplicationOperationResult<PaymentDetailDto>.Invalid(validation);
    }

    var context = await AuthorizeContextAsync(permissionCode, cancellationToken).ConfigureAwait(false);
    if (context is null)
    {
      return ApplicationOperationResult<PaymentDetailDto>.Failed(ApplicationOperationFailure.Forbidden);
    }

    var charge = await paymentRepository.FindAsync(
        new EntityId(id),
        context.OrganizationId,
        includeArchived,
        cancellationToken)
      .ConfigureAwait(false);
    if (charge is null)
    {
      return ApplicationOperationResult<PaymentDetailDto>.Failed(ApplicationOperationFailure.NotFound);
    }

    var early = mutate(charge, context);
    if (early is not null)
    {
      return early;
    }

    await paymentRepository.UpdateAsync(charge, cancellationToken).ConfigureAwait(false);
    var snapshot = await paymentRepository.FindSnapshotAsync(charge.Id, context.OrganizationId, true, cancellationToken)
      .ConfigureAwait(false);
    await WriteMutationSideEffectsAsync(eventName, charge, context, cancellationToken).ConfigureAwait(false);

    return ApplicationOperationResult<PaymentDetailDto>.Success(ToDetail(snapshot!, locale, Today()));
  }

  private async Task<ResolvedPaymentEntities> ResolveRelatedEntitiesAsync(
    PaymentCreateRequestDto request,
    OrganizationId organizationId,
    CancellationToken cancellationToken) =>
    await ResolveRelatedEntitiesAsync(
        request.ContractId,
        request.PropertyId,
        request.ResidentId,
        request.UtilityAccountId,
        organizationId,
        cancellationToken)
      .ConfigureAwait(false);

  private async Task<ResolvedPaymentEntities> ResolveRelatedEntitiesAsync(
    PaymentUpdateRequestDto request,
    OrganizationId organizationId,
    CancellationToken cancellationToken) =>
    await ResolveRelatedEntitiesAsync(
        request.ContractId,
        request.PropertyId,
        request.ResidentId,
        request.UtilityAccountId,
        organizationId,
        cancellationToken)
      .ConfigureAwait(false);

  private async Task<ResolvedPaymentEntities> ResolveRelatedEntitiesAsync(
    Guid? contractIdValue,
    Guid? propertyIdValue,
    Guid? residentIdValue,
    Guid? utilityAccountIdValue,
    OrganizationId organizationId,
    CancellationToken cancellationToken)
  {
    var errors = new List<ValidationFailure>();
    PaymentContractSnapshot? contract = null;
    PaymentPropertySnapshot? property = null;
    PaymentResidentSnapshot? resident = null;
    var propertyId = ToEntityIdOrNull(propertyIdValue);
    var residentId = ToEntityIdOrNull(residentIdValue);

    if (contractIdValue.HasValue && contractIdValue.Value != Guid.Empty)
    {
      contract = await paymentRepository.GetContractSnapshotAsync(
          new EntityId(contractIdValue.Value),
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
        propertyId ??= contract.PropertyId;
        residentId ??= contract.PrimaryResidentId;
      }
    }

    if (propertyId.HasValue)
    {
      property = await paymentRepository.GetPropertySnapshotAsync(propertyId.Value, organizationId, cancellationToken)
        .ConfigureAwait(false);
      if (property is null)
      {
        errors.Add(new ValidationFailure("propertyId", "validation.property"));
      }
    }

    if (residentId.HasValue)
    {
      resident = await paymentRepository.GetResidentSnapshotAsync(residentId.Value, organizationId, cancellationToken)
        .ConfigureAwait(false);
      if (resident is null)
      {
        errors.Add(new ValidationFailure("residentId", "validation.resident"));
      }
    }

    return new ResolvedPaymentEntities(
      errors,
      ToEntityIdOrNull(contractIdValue),
      propertyId,
      residentId,
      contract,
      property,
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
    PaymentCharge charge,
    ActiveOrganizationContext context,
    CancellationToken cancellationToken)
  {
    var subject = EntityReference.FromGuid("payment", charge.Id.Value, charge.Title);
    var actor = EventActor.User(context.UserId, context.User.DisplayName);
    var data = new Dictionary<string, string>(StringComparer.Ordinal)
    {
      ["status"] = PaymentCatalog.ToStatusCode(charge.GetEffectiveStatus(Today())),
      ["amount"] = charge.Amount.Amount.ToString(System.Globalization.CultureInfo.InvariantCulture),
      ["currency"] = charge.Amount.Currency,
      ["dueDate"] = charge.DueDate.ToString("O", System.Globalization.CultureInfo.InvariantCulture),
      ["balance"] = charge.Balance().Amount.ToString(System.Globalization.CultureInfo.InvariantCulture)
    };
    if (!string.IsNullOrWhiteSpace(charge.ProviderCode))
    {
      data["providerCode"] = charge.ProviderCode;
    }

    var related = new List<EntityReference>();
    if (charge.ContractId.HasValue)
    {
      related.Add(EntityReference.FromGuid("contract", charge.ContractId.Value.Value));
    }

    if (charge.PropertyId.HasValue)
    {
      related.Add(EntityReference.FromGuid("property", charge.PropertyId.Value.Value));
    }

    if (charge.ResidentId.HasValue)
    {
      related.Add(EntityReference.FromGuid("resident", charge.ResidentId.Value.Value));
    }

    var envelope = ModuleEventEnvelope.Create(
      context.OrganizationId,
      "payments",
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

  private static PaymentListItemDto ToListItem(PaymentSnapshot snapshot, string? locale, DateOnly today)
  {
    var charge = snapshot.Charge;
    var effectiveStatus = charge.GetEffectiveStatus(today);
    return new PaymentListItemDto(
      charge.Id.Value,
      charge.Title,
      charge.Description,
      ToContractSummary(snapshot.Contract),
      ToPropertySummary(snapshot.Property),
      ToResidentSummary(snapshot.Resident),
      charge.DueDate,
      PaymentCatalog.ToStatusLabel(effectiveStatus, locale),
      ToMoneyDto(charge.Amount),
      ToMoneyDto(charge.DiscountAmount),
      ToMoneyDto(charge.PenaltyAmount),
      ToMoneyDto(charge.GrossAmount()),
      ToMoneyDto(charge.SettledAmount()),
      ToMoneyDto(charge.Balance()),
      PaymentCatalog.ToMethodCode(charge.PreferredMethod),
      PaymentCatalog.ToMethodLabel(charge.PreferredMethod, locale),
      effectiveStatus == PaymentStatus.Overdue,
      charge.IsDeleted || effectiveStatus == PaymentStatus.Archived,
      charge.CreatedAt,
      charge.UpdatedAt,
      charge.ConcurrencyToken.Value);
  }

  private static PaymentDetailDto ToDetail(PaymentSnapshot snapshot, string? locale, DateOnly today)
  {
    var listItem = ToListItem(snapshot, locale, today);
    var charge = snapshot.Charge;
    var id = Uri.EscapeDataString(charge.Id.Value.ToString("D"));
    return new PaymentDetailDto(
      listItem.Id,
      listItem.Title,
      listItem.Description,
      listItem.Contract,
      listItem.Property,
      listItem.Resident,
      charge.UtilityAccountId?.Value,
      listItem.DueDate,
      listItem.Status,
      listItem.Amount,
      listItem.DiscountAmount,
      listItem.PenaltyAmount,
      listItem.GrossAmount,
      listItem.SettledAmount,
      listItem.Balance,
      listItem.PreferredMethod,
      listItem.PreferredMethodLabel,
      PaymentCatalog.ToReconciliationStatusLabel(charge.ReconciliationStatus, locale),
      charge.ProviderCode,
      charge.ProviderReference,
      charge.ProviderMetadataJson,
      charge.Notes,
      charge.Transactions
        .OrderByDescending(transaction => transaction.SettledOn)
        .ThenByDescending(transaction => transaction.CreatedAt)
        .Select(transaction => new PaymentTransactionDto(
          transaction.Id.Value,
          ToMoneyDto(transaction.Amount),
          PaymentCatalog.ToMethodCode(transaction.Method),
          PaymentCatalog.ToMethodLabel(transaction.Method, locale),
          transaction.SettledOn,
          transaction.BankReference,
          transaction.ProviderCode,
          transaction.ProviderReference,
          transaction.ReceiptDocumentId?.Value,
          transaction.Notes,
          transaction.IsReversed,
          transaction.CreatedAt))
        .ToArray(),
      snapshot.Receipts.Select(receipt => new PaymentReceiptDocumentDto(
          receipt.DocumentId.Value,
          receipt.Label,
          $"/documentos?paymentId={id}&category=payment-receipt"))
        .ToArray(),
      $"/timeline?entityType=payment&entityId={id}",
      $"/auditoria?entityType=payment&entityId={id}",
      charge.CreatedAt,
      charge.UpdatedAt,
      charge.DeletedAt,
      charge.ConcurrencyToken.Value);
  }

  private static PaymentEntitySummaryDto? ToContractSummary(PaymentContractSnapshot? contract) =>
    contract is null
      ? null
      : new PaymentEntitySummaryDto(
        contract.ContractId.Value,
        contract.DisplayName,
        $"{contract.PropertyName} - {contract.ResidentName}",
        $"/contratos?id={contract.ContractId.Value:D}");

  private static PaymentEntitySummaryDto? ToPropertySummary(PaymentPropertySnapshot? property) =>
    property is null
      ? null
      : new PaymentEntitySummaryDto(
        property.PropertyId.Value,
        property.Name,
        property.Location,
        $"/imoveis?id={property.PropertyId.Value:D}");

  private static PaymentEntitySummaryDto? ToResidentSummary(PaymentResidentSnapshot? resident) =>
    resident is null
      ? null
      : new PaymentEntitySummaryDto(
        resident.ResidentId.Value,
        resident.Name,
        Route: $"/moradores?id={resident.ResidentId.Value:D}");

  private static string BuildPayerSummary(PaymentSnapshot snapshot) =>
    snapshot.Resident?.Name ??
    snapshot.Contract?.ResidentName ??
    snapshot.Property?.Name ??
    snapshot.Charge.Title;

  private static IEnumerable<ValidationFailure> ValidateChargeRequest(PaymentCreateRequestDto request)
  {
    foreach (var failure in ValidateChargeFields(
      request.Title,
      request.DueDate,
      request.Amount,
      request.DiscountAmount,
      request.PenaltyAmount,
      request.PreferredMethod,
      request.ReconciliationStatus,
      request.ContractId,
      request.PropertyId,
      request.ResidentId,
      request.UtilityAccountId))
    {
      yield return failure;
    }
  }

  private static IEnumerable<ValidationFailure> ValidateChargeRequest(PaymentUpdateRequestDto request)
  {
    foreach (var failure in ValidateChargeFields(
      request.Title,
      request.DueDate,
      request.Amount,
      request.DiscountAmount,
      request.PenaltyAmount,
      request.PreferredMethod,
      request.ReconciliationStatus,
      request.ContractId,
      request.PropertyId,
      request.ResidentId,
      request.UtilityAccountId))
    {
      yield return failure;
    }
  }

  private static IEnumerable<ValidationFailure> ValidateChargeFields(
    string title,
    DateOnly dueDate,
    PaymentMoneyDto amount,
    PaymentMoneyDto? discountAmount,
    PaymentMoneyDto? penaltyAmount,
    string preferredMethod,
    string reconciliationStatus,
    Guid? contractId,
    Guid? propertyId,
    Guid? residentId,
    Guid? utilityAccountId)
  {
    if (string.IsNullOrWhiteSpace(title))
    {
      yield return new ValidationFailure(nameof(title), ValidationMessageKeys.Required);
    }

    if (dueDate == default)
    {
      yield return new ValidationFailure(nameof(dueDate), ValidationMessageKeys.Required);
    }

    foreach (var failure in ValidateMoney(amount, nameof(amount), requirePositive: true))
    {
      yield return failure;
    }

    foreach (var failure in ValidateMoney(discountAmount, nameof(discountAmount), requirePositive: false))
    {
      yield return failure;
    }

    foreach (var failure in ValidateMoney(penaltyAmount, nameof(penaltyAmount), requirePositive: false))
    {
      yield return failure;
    }

    if (amount is not null &&
      string.Equals(discountAmount?.Currency ?? amount.Currency, amount.Currency, StringComparison.OrdinalIgnoreCase) &&
      string.Equals(penaltyAmount?.Currency ?? amount.Currency, amount.Currency, StringComparison.OrdinalIgnoreCase) &&
      amount.Amount + (penaltyAmount?.Amount ?? 0m) - (discountAmount?.Amount ?? 0m) <= 0)
    {
      yield return new ValidationFailure(nameof(discountAmount), "validation.paymentGrossAmount");
    }

    if (!PaymentCatalog.TryParseMethod(preferredMethod, out _))
    {
      yield return new ValidationFailure(nameof(preferredMethod), "validation.paymentMethod");
    }

    if (!PaymentCatalog.TryParseReconciliationStatus(reconciliationStatus, out _))
    {
      yield return new ValidationFailure(nameof(reconciliationStatus), "validation.reconciliationStatus");
    }

    if (utilityAccountId.HasValue)
    {
      yield return new ValidationFailure(nameof(utilityAccountId), "validation.utilityAccount");
    }

    if (!contractId.HasValue &&
      !propertyId.HasValue &&
      !residentId.HasValue)
    {
      yield return new ValidationFailure(nameof(contractId), "validation.paymentLink");
    }
  }

  private static IEnumerable<ValidationFailure> ValidateTransactionRequest(PaymentTransactionRequestDto request)
  {
    foreach (var failure in ValidateMoney(request.Amount, nameof(request.Amount), requirePositive: true))
    {
      yield return failure;
    }

    if (request.SettledOn == default)
    {
      yield return new ValidationFailure(nameof(request.SettledOn), ValidationMessageKeys.Required);
    }

    if (!PaymentCatalog.TryParseMethod(request.Method, out _))
    {
      yield return new ValidationFailure(nameof(request.Method), "validation.paymentMethod");
    }
  }

  private static IEnumerable<ValidationFailure> ValidateProviderEvent(PaymentProviderEventRequestDto request)
  {
    if (string.IsNullOrWhiteSpace(request.ProviderCode))
    {
      yield return new ValidationFailure(nameof(request.ProviderCode), ValidationMessageKeys.Required);
    }

    if (string.IsNullOrWhiteSpace(request.ProviderReference))
    {
      yield return new ValidationFailure(nameof(request.ProviderReference), ValidationMessageKeys.Required);
    }

    var eventType = string.IsNullOrWhiteSpace(request.EventType)
      ? string.Empty
      : PaymentCode.NormalizeCode(request.EventType);
    if (!string.Equals(eventType, "settled", StringComparison.Ordinal) &&
      !string.Equals(eventType, "paid", StringComparison.Ordinal))
    {
      yield return new ValidationFailure(nameof(request.EventType), "validation.providerEvent");
    }

    if (request.SettledOn == default)
    {
      yield return new ValidationFailure(nameof(request.SettledOn), ValidationMessageKeys.Required);
    }

    if (request.Amount is not null)
    {
      foreach (var failure in ValidateMoney(request.Amount, nameof(request.Amount), requirePositive: true))
      {
        yield return failure;
      }
    }
  }

  private static IEnumerable<ValidationFailure> ValidateMoney(
    PaymentMoneyDto? money,
    string fieldName,
    bool requirePositive)
  {
    if (money is null)
    {
      if (requirePositive)
      {
        yield return new ValidationFailure(fieldName, ValidationMessageKeys.Required);
      }

      yield break;
    }

    if (string.IsNullOrWhiteSpace(money.Currency) || money.Currency.Trim().Length != 3)
    {
      yield return new ValidationFailure(fieldName, "validation.money");
    }

    if (requirePositive ? money.Amount <= 0 : money.Amount < 0)
    {
      yield return new ValidationFailure(fieldName, "validation.money");
    }
  }

  private static IEnumerable<ValidationFailure> ValidateId(Guid id, string propertyName = "id")
  {
    if (id == Guid.Empty)
    {
      yield return new ValidationFailure(propertyName, ValidationMessageKeys.InvalidId);
    }
  }

  private static PaymentMethod ParseMethod(string value) =>
    PaymentCatalog.TryParseMethod(value, out var method)
      ? method
      : throw new ArgumentException("Invalid payment method.", nameof(value));

  private static PaymentReconciliationStatus ParseReconciliationStatus(string value) =>
    PaymentCatalog.TryParseReconciliationStatus(value, out var status)
      ? status
      : throw new ArgumentException("Invalid payment reconciliation status.", nameof(value));

  private static EntityId? ToEntityIdOrNull(Guid? id) =>
    id.HasValue && id.Value != Guid.Empty ? new EntityId(id.Value) : null;

  private static Money ToMoney(PaymentMoneyDto money) => new(money.Amount, money.Currency);

  private static Money? ToMoneyOrNull(PaymentMoneyDto? money) => money is null ? null : ToMoney(money);

  private static Money CalculateGrossAmount(
    PaymentMoneyDto amount,
    PaymentMoneyDto? discountAmount,
    PaymentMoneyDto? penaltyAmount)
  {
    var money = ToMoney(amount);
    var discount = ToMoneyOrNull(discountAmount) ?? Money.Zero(money.Currency);
    var penalty = ToMoneyOrNull(penaltyAmount) ?? Money.Zero(money.Currency);
    return money.Add(penalty).Subtract(discount);
  }

  private static PaymentMoneyDto ToMoneyDto(Money money) => new(money.Amount, money.Currency);

  private static ApplicationOperationResult<PaymentDetailDto>? EnsureCurrentConcurrencyToken(
    PaymentCharge charge,
    string? concurrencyToken) =>
    string.IsNullOrWhiteSpace(concurrencyToken) ||
    !string.Equals(concurrencyToken.Trim(), charge.ConcurrencyToken.Value, StringComparison.Ordinal)
      ? ApplicationOperationResult<PaymentDetailDto>.Failed(
        ApplicationOperationFailure.Conflict,
        new Dictionary<string, string[]> { ["concurrencyToken"] = ["validation.concurrency"] })
      : null;

  private DateOnly Today() => DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);

  private sealed record ResolvedPaymentEntities(
    IReadOnlyList<ValidationFailure> Errors,
    EntityId? ContractId,
    EntityId? PropertyId,
    EntityId? ResidentId,
    PaymentContractSnapshot? Contract,
    PaymentPropertySnapshot? Property,
    PaymentResidentSnapshot? Resident);
}
