using Alsappan.Application.Common.Audit;
using Alsappan.Application.Common.Authorization;
using Alsappan.Application.Common.Contracts;
using Alsappan.Application.Common.Events;
using Alsappan.Application.Common.Files;
using Alsappan.Application.Common.Results;
using Alsappan.Application.Common.Tenancy;
using Alsappan.Application.Common.Validation;
using Alsappan.Application.Contracts;
using Alsappan.Application.Contracts.Repositories;
using Alsappan.Application.Documents;
using Alsappan.Application.Documents.Repositories;
using Alsappan.Application.Identity.Repositories;
using Alsappan.Application.Inspections;
using Alsappan.Application.Inspections.Repositories;
using Alsappan.Application.Notifications;
using Alsappan.Application.Notifications.Repositories;
using Alsappan.Application.Occurrences;
using Alsappan.Application.Occurrences.Repositories;
using Alsappan.Application.Payments;
using Alsappan.Application.Payments.Providers;
using Alsappan.Application.Payments.Repositories;
using Alsappan.Application.Properties;
using Alsappan.Application.Properties.Repositories;
using Alsappan.Application.Residents;
using Alsappan.Application.Residents.Repositories;
using Alsappan.Application.Settings.Repositories;
using Alsappan.Domain.Common.Events;
using Alsappan.Domain.Common.Identifiers;
using Alsappan.Domain.Common.ValueObjects;
using Alsappan.Domain.Contracts;
using Alsappan.Domain.Documents;
using Alsappan.Domain.Identity;
using Alsappan.Domain.Occurrences;
using Alsappan.Domain.Payments;
using Alsappan.Domain.Residents;
using Alsappan.Domain.Settings;

namespace Alsappan.Application.ResidentPortal;

public sealed class ResidentPortalService : IResidentPortalService
{
  private const int PortalPageSize = 100;

  private readonly IActiveOrganizationContextResolver activeOrganizationContextResolver;
  private readonly IIdentityRepository identityRepository;
  private readonly IResidentRepository residentRepository;
  private readonly ISettingsRepository settingsRepository;
  private readonly IPropertyRepository propertyRepository;
  private readonly IContractRepository contractRepository;
  private readonly IPaymentRepository paymentRepository;
  private readonly IDocumentRepository documentRepository;
  private readonly IOccurrenceRepository occurrenceRepository;
  private readonly IInspectionRepository inspectionRepository;
  private readonly INotificationRepository notificationRepository;
  private readonly IFileStorageProvider fileStorageProvider;
  private readonly Dictionary<string, IPaymentInstructionProvider> providers;
  private readonly IAuditWriter auditWriter;
  private readonly IModuleEventOutboxWriter outboxWriter;
  private readonly TimeProvider timeProvider;

  public ResidentPortalService(
    IActiveOrganizationContextResolver activeOrganizationContextResolver,
    IIdentityRepository identityRepository,
    IResidentRepository residentRepository,
    ISettingsRepository settingsRepository,
    IPropertyRepository propertyRepository,
    IContractRepository contractRepository,
    IPaymentRepository paymentRepository,
    IDocumentRepository documentRepository,
    IOccurrenceRepository occurrenceRepository,
    IInspectionRepository inspectionRepository,
    INotificationRepository notificationRepository,
    IFileStorageProvider fileStorageProvider,
    IEnumerable<IPaymentInstructionProvider> providers,
    IAuditWriter auditWriter,
    IModuleEventOutboxWriter outboxWriter,
    TimeProvider? timeProvider = null)
  {
    this.activeOrganizationContextResolver = activeOrganizationContextResolver
      ?? throw new ArgumentNullException(nameof(activeOrganizationContextResolver));
    this.identityRepository = identityRepository ?? throw new ArgumentNullException(nameof(identityRepository));
    this.residentRepository = residentRepository ?? throw new ArgumentNullException(nameof(residentRepository));
    this.settingsRepository = settingsRepository ?? throw new ArgumentNullException(nameof(settingsRepository));
    this.propertyRepository = propertyRepository ?? throw new ArgumentNullException(nameof(propertyRepository));
    this.contractRepository = contractRepository ?? throw new ArgumentNullException(nameof(contractRepository));
    this.paymentRepository = paymentRepository ?? throw new ArgumentNullException(nameof(paymentRepository));
    this.documentRepository = documentRepository ?? throw new ArgumentNullException(nameof(documentRepository));
    this.occurrenceRepository = occurrenceRepository ?? throw new ArgumentNullException(nameof(occurrenceRepository));
    this.inspectionRepository = inspectionRepository ?? throw new ArgumentNullException(nameof(inspectionRepository));
    this.notificationRepository = notificationRepository ?? throw new ArgumentNullException(nameof(notificationRepository));
    this.fileStorageProvider = fileStorageProvider ?? throw new ArgumentNullException(nameof(fileStorageProvider));
    this.providers = (providers ?? throw new ArgumentNullException(nameof(providers)))
      .ToDictionary(provider => PaymentCode.NormalizeCode(provider.ProviderCode), StringComparer.Ordinal);
    this.auditWriter = auditWriter ?? throw new ArgumentNullException(nameof(auditWriter));
    this.outboxWriter = outboxWriter ?? throw new ArgumentNullException(nameof(outboxWriter));
    this.timeProvider = timeProvider ?? TimeProvider.System;
  }

  public async Task<ApplicationOperationResult<ResidentPortalSummaryDto>> GetSummaryAsync(
    string? locale = null,
    CancellationToken cancellationToken = default)
  {
    var portal = await ResolvePortalContextAsync(cancellationToken).ConfigureAwait(false);
    if (!portal.Succeeded)
    {
      return ApplicationOperationResult<ResidentPortalSummaryDto>.Failed(portal.Failure);
    }

    var context = portal.Context!;
    var contracts = await GetContractItemsAsync(context, locale, cancellationToken).ConfigureAwait(false);
    var property = await GetLinkedPropertyCoreAsync(context, contracts, locale, cancellationToken).ConfigureAwait(false);
    var payments = await GetPaymentItemsAsync(context, locale, cancellationToken).ConfigureAwait(false);
    var documents = await GetDocumentItemsAsync(context, contracts, payments, locale, cancellationToken)
      .ConfigureAwait(false);
    var occurrences = await GetOccurrenceItemsAsync(context, locale, cancellationToken).ConfigureAwait(false);
    var inspections = await GetInspectionItemsAsync(context, locale, cancellationToken).ConfigureAwait(false);
    var notifications = await GetNotificationItemsAsync(context, locale, cancellationToken).ConfigureAwait(false);

    return ApplicationOperationResult<ResidentPortalSummaryDto>.Success(new ResidentPortalSummaryDto(
      ToProfile(context.Resident, locale),
      property,
      contracts.Items,
      payments.Items,
      documents,
      occurrences.Items,
      inspections.Items,
      notifications.Items,
      ToSettings(context.Settings)));
  }

  public async Task<ApplicationOperationResult<ResidentPortalProfileDto>> GetProfileAsync(
    string? locale = null,
    CancellationToken cancellationToken = default)
  {
    var portal = await ResolvePortalContextAsync(cancellationToken).ConfigureAwait(false);
    return portal.Succeeded
      ? ApplicationOperationResult<ResidentPortalProfileDto>.Success(ToProfile(portal.Context!.Resident, locale))
      : ApplicationOperationResult<ResidentPortalProfileDto>.Failed(portal.Failure);
  }

  public async Task<ApplicationOperationResult<ResidentPortalPropertyDto?>> GetLinkedPropertyAsync(
    string? locale = null,
    CancellationToken cancellationToken = default)
  {
    var portal = await ResolvePortalContextAsync(cancellationToken).ConfigureAwait(false);
    if (!portal.Succeeded)
    {
      return ApplicationOperationResult<ResidentPortalPropertyDto?>.Failed(portal.Failure);
    }

    var contracts = await GetContractItemsAsync(portal.Context!, locale, cancellationToken).ConfigureAwait(false);
    return ApplicationOperationResult<ResidentPortalPropertyDto?>.Success(
      await GetLinkedPropertyCoreAsync(portal.Context!, contracts, locale, cancellationToken).ConfigureAwait(false));
  }

  public async Task<ApplicationOperationResult<IReadOnlyList<ResidentPortalContractDto>>> ListContractsAsync(
    string? locale = null,
    CancellationToken cancellationToken = default)
  {
    var portal = await ResolvePortalContextAsync(cancellationToken).ConfigureAwait(false);
    if (!portal.Succeeded)
    {
      return ApplicationOperationResult<IReadOnlyList<ResidentPortalContractDto>>.Failed(portal.Failure);
    }

    var contracts = await GetContractItemsAsync(portal.Context!, locale, cancellationToken).ConfigureAwait(false);
    return ApplicationOperationResult<IReadOnlyList<ResidentPortalContractDto>>.Success(contracts.Items);
  }

  public async Task<ApplicationOperationResult<IReadOnlyList<ResidentPortalPaymentDto>>> ListPaymentsAsync(
    string? locale = null,
    CancellationToken cancellationToken = default)
  {
    var portal = await ResolvePortalContextAsync(cancellationToken).ConfigureAwait(false);
    if (!portal.Succeeded)
    {
      return ApplicationOperationResult<IReadOnlyList<ResidentPortalPaymentDto>>.Failed(portal.Failure);
    }

    var payments = await GetPaymentItemsAsync(portal.Context!, locale, cancellationToken).ConfigureAwait(false);
    return ApplicationOperationResult<IReadOnlyList<ResidentPortalPaymentDto>>.Success(payments.Items);
  }

  public async Task<ApplicationOperationResult<IReadOnlyList<ResidentPortalDocumentDto>>> ListDocumentsAsync(
    string? locale = null,
    CancellationToken cancellationToken = default)
  {
    var portal = await ResolvePortalContextAsync(cancellationToken).ConfigureAwait(false);
    if (!portal.Succeeded)
    {
      return ApplicationOperationResult<IReadOnlyList<ResidentPortalDocumentDto>>.Failed(portal.Failure);
    }

    var contracts = await GetContractItemsAsync(portal.Context!, locale, cancellationToken).ConfigureAwait(false);
    var payments = await GetPaymentItemsAsync(portal.Context!, locale, cancellationToken).ConfigureAwait(false);
    var documents = await GetDocumentItemsAsync(portal.Context!, contracts, payments, locale, cancellationToken)
      .ConfigureAwait(false);
    return ApplicationOperationResult<IReadOnlyList<ResidentPortalDocumentDto>>.Success(documents);
  }

  public async Task<ApplicationOperationResult<DocumentDownloadDto>> DownloadDocumentAsync(
    Guid documentId,
    CancellationToken cancellationToken = default)
  {
    var validation = ValidateId(documentId, nameof(documentId)).ToList();
    if (validation.Count > 0)
    {
      return ApplicationOperationResult<DocumentDownloadDto>.Invalid(validation);
    }

    var portal = await ResolvePortalContextAsync(cancellationToken).ConfigureAwait(false);
    if (!portal.Succeeded)
    {
      return ApplicationOperationResult<DocumentDownloadDto>.Failed(portal.Failure);
    }

    var context = portal.Context!;
    var document = await documentRepository.FindAsync(
        new EntityId(documentId),
        context.OrganizationId,
        includeArchived: false,
        cancellationToken)
      .ConfigureAwait(false);
    if (document is null)
    {
      return ApplicationOperationResult<DocumentDownloadDto>.Failed(ApplicationOperationFailure.NotFound);
    }

    var contracts = await GetContractItemsAsync(context, locale: null, cancellationToken).ConfigureAwait(false);
    var payments = await GetPaymentItemsAsync(context, locale: null, cancellationToken).ConfigureAwait(false);
    if (!IsDocumentVisibleToResident(document, context.Resident.Id, contracts, payments))
    {
      return ApplicationOperationResult<DocumentDownloadDto>.Failed(ApplicationOperationFailure.NotFound);
    }

    var stream = await fileStorageProvider.OpenReadAsync(
        context.OrganizationId,
        document.CurrentStorageKey,
        cancellationToken)
      .ConfigureAwait(false);

    return ApplicationOperationResult<DocumentDownloadDto>.Success(new DocumentDownloadDto(
      document.CurrentFileName,
      document.CurrentContentType,
      document.CurrentSizeBytes,
      stream));
  }

  public async Task<ApplicationOperationResult<IReadOnlyList<ResidentPortalOccurrenceDto>>> ListOccurrencesAsync(
    string? locale = null,
    CancellationToken cancellationToken = default)
  {
    var portal = await ResolvePortalContextAsync(cancellationToken).ConfigureAwait(false);
    if (!portal.Succeeded)
    {
      return ApplicationOperationResult<IReadOnlyList<ResidentPortalOccurrenceDto>>.Failed(portal.Failure);
    }

    var occurrences = await GetOccurrenceItemsAsync(portal.Context!, locale, cancellationToken).ConfigureAwait(false);
    return ApplicationOperationResult<IReadOnlyList<ResidentPortalOccurrenceDto>>.Success(occurrences.Items);
  }

  public async Task<ApplicationOperationResult<IReadOnlyList<ResidentPortalInspectionDto>>> ListInspectionsAsync(
    string? locale = null,
    CancellationToken cancellationToken = default)
  {
    var portal = await ResolvePortalContextAsync(cancellationToken).ConfigureAwait(false);
    if (!portal.Succeeded)
    {
      return ApplicationOperationResult<IReadOnlyList<ResidentPortalInspectionDto>>.Failed(portal.Failure);
    }

    var inspections = await GetInspectionItemsAsync(portal.Context!, locale, cancellationToken).ConfigureAwait(false);
    return ApplicationOperationResult<IReadOnlyList<ResidentPortalInspectionDto>>.Success(inspections.Items);
  }

  public async Task<ApplicationOperationResult<IReadOnlyList<ResidentPortalNotificationDto>>> ListNotificationsAsync(
    string? locale = null,
    CancellationToken cancellationToken = default)
  {
    var portal = await ResolvePortalContextAsync(cancellationToken).ConfigureAwait(false);
    if (!portal.Succeeded)
    {
      return ApplicationOperationResult<IReadOnlyList<ResidentPortalNotificationDto>>.Failed(portal.Failure);
    }

    var notifications = await GetNotificationItemsAsync(portal.Context!, locale, cancellationToken).ConfigureAwait(false);
    return ApplicationOperationResult<IReadOnlyList<ResidentPortalNotificationDto>>.Success(notifications.Items);
  }

  public async Task<ApplicationOperationResult<ResidentPortalOccurrenceDto>> CreateOccurrenceAsync(
    ResidentPortalOccurrenceCreateRequestDto request,
    string? locale = null,
    CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(request);

    var portal = await ResolvePortalContextAsync(cancellationToken).ConfigureAwait(false);
    if (!portal.Succeeded)
    {
      return ApplicationOperationResult<ResidentPortalOccurrenceDto>.Failed(portal.Failure);
    }

    var context = portal.Context!;
    if (!context.Settings.ResidentOccurrenceCreationEnabled)
    {
      return ApplicationOperationResult<ResidentPortalOccurrenceDto>.Failed(ApplicationOperationFailure.Forbidden);
    }

    var validation = ValidateOccurrenceRequest(request).ToList();
    if (validation.Count > 0)
    {
      return ApplicationOperationResult<ResidentPortalOccurrenceDto>.Invalid(validation);
    }

    var related = await ResolveOccurrenceContextAsync(context, request, cancellationToken).ConfigureAwait(false);
    if (related.Errors.Count > 0)
    {
      return ApplicationOperationResult<ResidentPortalOccurrenceDto>.Invalid(related.Errors);
    }

    var now = timeProvider.GetUtcNow();
    var occurrence = Occurrence.Create(
      EntityId.New(),
      context.OrganizationId,
      request.Title,
      request.Description,
      ParseOccurrenceType(request.Type),
      ParseOccurrencePriority(request.Priority),
      related.PropertyId,
      context.Resident.Id,
      related.ContractId,
      assignedUserId: null,
      request.DueDate,
      related.Property?.Name,
      context.Resident.FullName,
      related.Contract?.DisplayName,
      assigneeSearchText: null,
      now,
      context.UserId);

    await occurrenceRepository.AddAsync(occurrence, cancellationToken).ConfigureAwait(false);
    var snapshot = await occurrenceRepository.FindSnapshotAsync(
        occurrence.Id,
        context.OrganizationId,
        includeArchived: true,
        cancellationToken)
      .ConfigureAwait(false);
    await WriteResidentEventAsync(
        "occurrence.created-by-resident",
        "occurrences",
        EntityReference.FromGuid("occurrence", occurrence.Id.Value, occurrence.Title),
        context,
        [
          EntityReference.FromGuid("resident", context.Resident.Id.Value, context.Resident.FullName)
        ],
        cancellationToken)
      .ConfigureAwait(false);

    return ApplicationOperationResult<ResidentPortalOccurrenceDto>.Success(
      ToOccurrence(snapshot ?? new OccurrenceSnapshot(
        occurrence,
        related.Property,
        new OccurrenceResidentSnapshot(context.Resident.Id, context.Resident.FullName),
        related.Contract,
        null,
        [],
        [],
        [],
        [],
        []), locale));
  }

  public async Task<ApplicationOperationResult<ResidentPortalDocumentDto>> UploadDocumentAsync(
    ResidentPortalDocumentUploadRequestDto request,
    string? locale = null,
    CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(request);

    var portal = await ResolvePortalContextAsync(cancellationToken).ConfigureAwait(false);
    if (!portal.Succeeded)
    {
      return ApplicationOperationResult<ResidentPortalDocumentDto>.Failed(portal.Failure);
    }

    var context = portal.Context!;
    if (!context.Settings.ResidentDocumentUploadEnabled)
    {
      return ApplicationOperationResult<ResidentPortalDocumentDto>.Failed(ApplicationOperationFailure.Forbidden);
    }

    var validation = ValidateDocumentUpload(request).ToList();
    if (validation.Count > 0)
    {
      return ApplicationOperationResult<ResidentPortalDocumentDto>.Invalid(validation);
    }

    var linkValidation = await ValidateResidentDocumentLinksAsync(context, request, cancellationToken)
      .ConfigureAwait(false);
    if (linkValidation.Count > 0)
    {
      return ApplicationOperationResult<ResidentPortalDocumentDto>.Invalid(linkValidation);
    }

    var stored = await fileStorageProvider.SaveAsync(
        new FileStorageRequest(
          context.OrganizationId,
          request.FileName,
          request.ContentType,
          request.Content,
          new Dictionary<string, string>(StringComparer.Ordinal)
          {
            ["module"] = "resident-portal"
          }),
        cancellationToken)
      .ConfigureAwait(false);
    var now = timeProvider.GetUtcNow();
    var document = DocumentRecord.Create(
      EntityId.New(),
      context.OrganizationId,
      ParseDocumentCategory(request.Category),
      request.Title,
      request.Description,
      stored.FileName,
      stored.ContentType,
      stored.Length,
      stored.StorageKey,
      request.VersionNotes,
      BuildResidentDocumentLinks(context, request),
      now,
      context.UserId);

    await documentRepository.AddAsync(document, cancellationToken).ConfigureAwait(false);
    await WriteResidentEventAsync(
        "document.uploaded-by-resident",
        "documents",
        EntityReference.FromGuid("document", document.Id.Value, document.Title),
        context,
        document.Links.Select(link => EntityReference.FromGuid(link.EntityType, link.EntityId.Value, link.Label)).ToArray(),
        cancellationToken)
      .ConfigureAwait(false);

    return ApplicationOperationResult<ResidentPortalDocumentDto>.Success(ToDocument(document, locale));
  }

  public async Task<ApplicationOperationResult<PaymentInstructionDto>> CreatePaymentInstructionAsync(
    Guid paymentId,
    PaymentInstructionRequestDto request,
    string? locale = null,
    CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(request);

    var validation = ValidateId(paymentId, nameof(paymentId)).ToList();
    if (string.IsNullOrWhiteSpace(request.ProviderCode))
    {
      validation.Add(new ValidationFailure(nameof(request.ProviderCode), ValidationMessageKeys.Required));
    }

    if (validation.Count > 0)
    {
      return ApplicationOperationResult<PaymentInstructionDto>.Invalid(validation);
    }

    var providerCode = PaymentCode.NormalizeCode(request.ProviderCode);
    if (!providers.TryGetValue(providerCode, out var provider))
    {
      return ApplicationOperationResult<PaymentInstructionDto>.Invalid(
        [new ValidationFailure(nameof(request.ProviderCode), "validation.paymentProvider")]);
    }

    var portal = await ResolvePortalContextAsync(cancellationToken).ConfigureAwait(false);
    if (!portal.Succeeded)
    {
      return ApplicationOperationResult<PaymentInstructionDto>.Failed(portal.Failure);
    }

    var context = portal.Context!;
    var snapshot = await paymentRepository.FindSnapshotAsync(
        new EntityId(paymentId),
        context.OrganizationId,
        includeArchived: false,
        cancellationToken)
      .ConfigureAwait(false);
    if (snapshot is null || !PaymentBelongsToResident(snapshot, context.Resident.Id))
    {
      return ApplicationOperationResult<PaymentInstructionDto>.Failed(ApplicationOperationFailure.NotFound);
    }

    var balance = snapshot.Charge.Balance();
    if (balance.Amount <= 0)
    {
      return ApplicationOperationResult<PaymentInstructionDto>.Failed(
        ApplicationOperationFailure.Conflict,
        new Dictionary<string, string[]> { ["status"] = ["validation.paymentAlreadySettled"] });
    }

    var instruction = await provider.CreateInstructionAsync(
        new PaymentProviderInstructionRequest(
          snapshot.Charge.Id.Value,
          provider.ProviderCode,
          provider.Kind,
          ToMoneyDto(balance),
          snapshot.Charge.DueDate,
          context.Resident.FullName,
          locale),
        cancellationToken)
      .ConfigureAwait(false);
    snapshot.Charge.SetProviderInstruction(
      instruction.ProviderCode,
      instruction.ProviderReference,
      System.Text.Json.JsonSerializer.Serialize(instruction.Metadata),
      timeProvider.GetUtcNow(),
      context.UserId);
    await paymentRepository.UpdateAsync(snapshot.Charge, cancellationToken).ConfigureAwait(false);
    await WriteResidentEventAsync(
        "payment.instruction-created-by-resident",
        "payments",
        EntityReference.FromGuid("payment", snapshot.Charge.Id.Value, snapshot.Charge.Title),
        context,
        [
          EntityReference.FromGuid("resident", context.Resident.Id.Value, context.Resident.FullName)
        ],
        cancellationToken)
      .ConfigureAwait(false);

    return ApplicationOperationResult<PaymentInstructionDto>.Success(instruction);
  }

  private async Task<PortalContextResult> ResolvePortalContextAsync(CancellationToken cancellationToken)
  {
    var resolution = await activeOrganizationContextResolver.ResolveAsync(cancellationToken).ConfigureAwait(false);
    if (!resolution.Succeeded || resolution.Context is null)
    {
      return PortalContextResult.Failed(ApplicationOperationFailure.Forbidden);
    }

    var context = resolution.Context;
    if (!context.Membership.RoleCodes.Contains(RoleCodes.ResidentUser, StringComparer.OrdinalIgnoreCase))
    {
      return PortalContextResult.Failed(ApplicationOperationFailure.Forbidden);
    }

    var link = await identityRepository.FindResidentAccountLinkAsync(
        context.UserId,
        context.OrganizationId,
        cancellationToken)
      .ConfigureAwait(false);
    if (link is null)
    {
      return PortalContextResult.Failed(ApplicationOperationFailure.Forbidden);
    }

    var resident = await residentRepository.FindAsync(
        link.ResidentId,
        context.OrganizationId,
        includeArchived: false,
        cancellationToken)
      .ConfigureAwait(false);
    if (resident is null ||
      resident.LinkedUserId != context.UserId ||
      resident.PortalStatus != ResidentPortalStatus.Active)
    {
      return PortalContextResult.Failed(ApplicationOperationFailure.Forbidden);
    }

    var settings = await settingsRepository.GetOrCreateSettingsAsync(
        context.OrganizationId,
        timeProvider.GetUtcNow(),
        context.UserId,
        cancellationToken)
      .ConfigureAwait(false);
    if (!settings.ResidentPortalEnabled)
    {
      return PortalContextResult.Failed(ApplicationOperationFailure.Forbidden);
    }

    return PortalContextResult.Success(new PortalContext(context, resident, link, settings));
  }

  private async Task<PagedResultDto<ResidentPortalContractDto>> GetContractItemsAsync(
    PortalContext context,
    string? locale,
    CancellationToken cancellationToken)
  {
    var page = await contractRepository.ListAsync(
        new ContractListRequestDto(
          PageSize: PortalPageSize,
          ResidentId: context.Resident.Id.Value,
          IncludeArchived: true,
          Locale: locale),
        context.OrganizationId,
        Today(),
        cancellationToken)
      .ConfigureAwait(false);
    var items = page.Items
      .Where(snapshot => snapshot.Residents.Any(resident => resident.ResidentId == context.Resident.Id))
      .Select(snapshot => ToContract(snapshot, locale))
      .ToArray();

    return new PagedResultDto<ResidentPortalContractDto>(items, page.Page, page.PageSize, items.Length);
  }

  private async Task<ResidentPortalPropertyDto?> GetLinkedPropertyCoreAsync(
    PortalContext context,
    PagedResultDto<ResidentPortalContractDto> contracts,
    string? locale,
    CancellationToken cancellationToken)
  {
    var propertyId = contracts.Items
      .Where(contract => !contract.IsArchived)
      .OrderBy(contract => contract.EndDate ?? DateOnly.MaxValue)
      .ThenByDescending(contract => contract.StartDate)
      .Select(contract => contract.Property.Id)
      .FirstOrDefault();
    if (propertyId == Guid.Empty)
    {
      propertyId = contracts.Items
        .OrderByDescending(contract => contract.StartDate)
        .Select(contract => contract.Property.Id)
        .FirstOrDefault();
    }

    if (propertyId == Guid.Empty)
    {
      return null;
    }

    var property = await propertyRepository.FindAsync(
        new EntityId(propertyId),
        context.OrganizationId,
        includeArchived: false,
        cancellationToken)
      .ConfigureAwait(false);
    return property is null ? null : ToProperty(property, locale);
  }

  private async Task<PagedResultDto<ResidentPortalPaymentDto>> GetPaymentItemsAsync(
    PortalContext context,
    string? locale,
    CancellationToken cancellationToken)
  {
    var today = Today();
    var page = await paymentRepository.ListAsync(
        new PaymentListRequestDto(
          PageSize: PortalPageSize,
          ResidentId: context.Resident.Id.Value,
          Locale: locale),
        context.OrganizationId,
        today,
        cancellationToken)
      .ConfigureAwait(false);
    var items = page.Items
      .Where(snapshot => PaymentBelongsToResident(snapshot, context.Resident.Id))
      .Select(snapshot => ToPayment(snapshot, locale, today))
      .ToArray();

    return new PagedResultDto<ResidentPortalPaymentDto>(items, page.Page, page.PageSize, items.Length);
  }

  private async Task<IReadOnlyList<ResidentPortalDocumentDto>> GetDocumentItemsAsync(
    PortalContext context,
    PagedResultDto<ResidentPortalContractDto> contracts,
    PagedResultDto<ResidentPortalPaymentDto> payments,
    string? locale,
    CancellationToken cancellationToken)
  {
    var readableTypes = new HashSet<string>(StringComparer.Ordinal)
    {
      "contract",
      "document",
      "inspection",
      "occurrence",
      "payment",
      "property",
      "resident"
    };
    var links = new List<(string EntityType, Guid EntityId)>
    {
      ("resident", context.Resident.Id.Value)
    };
    links.AddRange(contracts.Items.Select(contract => ("contract", contract.Id)));
    links.AddRange(contracts.Items.Select(contract => ("property", contract.Property.Id)));
    links.AddRange(payments.Items.Select(payment => ("payment", payment.Id)));

    var documents = new Dictionary<Guid, ResidentPortalDocumentDto>();
    foreach (var (entityType, entityId) in links.Distinct())
    {
      var page = await documentRepository.ListAsync(
          new DocumentListRequestDto(
            PageSize: PortalPageSize,
            LinkedEntityType: entityType,
            LinkedEntityId: entityId,
            Locale: locale),
          context.OrganizationId,
          readableTypes,
          cancellationToken)
        .ConfigureAwait(false);

      foreach (var snapshot in page.Items)
      {
        documents.TryAdd(snapshot.Document.Id.Value, ToDocument(snapshot.Document, locale));
      }
    }

    return documents.Values
      .OrderByDescending(document => document.UploadedAt)
      .ToArray();
  }

  private async Task<PagedResultDto<ResidentPortalOccurrenceDto>> GetOccurrenceItemsAsync(
    PortalContext context,
    string? locale,
    CancellationToken cancellationToken)
  {
    var page = await occurrenceRepository.ListAsync(
        new OccurrenceListRequestDto(
          PageSize: PortalPageSize,
          ResidentId: context.Resident.Id.Value,
          Locale: locale),
        context.OrganizationId,
        cancellationToken)
      .ConfigureAwait(false);
    var items = page.Items
      .Where(snapshot => snapshot.Occurrence.ResidentId == context.Resident.Id)
      .Select(snapshot => ToOccurrence(snapshot, locale))
      .ToArray();

    return new PagedResultDto<ResidentPortalOccurrenceDto>(items, page.Page, page.PageSize, items.Length);
  }

  private async Task<PagedResultDto<ResidentPortalInspectionDto>> GetInspectionItemsAsync(
    PortalContext context,
    string? locale,
    CancellationToken cancellationToken)
  {
    var page = await inspectionRepository.ListAsync(
        new InspectionListRequestDto(
          PageSize: PortalPageSize,
          ResidentId: context.Resident.Id.Value,
          Locale: locale),
        context.OrganizationId,
        timeProvider.GetUtcNow(),
        cancellationToken)
      .ConfigureAwait(false);
    var items = page.Items
      .Where(snapshot => snapshot.Inspection.ResidentId == context.Resident.Id ||
        snapshot.Contract?.ResidentIds.Contains(context.Resident.Id) == true)
      .Select(snapshot => ToInspection(snapshot, locale))
      .ToArray();

    return new PagedResultDto<ResidentPortalInspectionDto>(items, page.Page, page.PageSize, items.Length);
  }

  private async Task<PagedResultDto<ResidentPortalNotificationDto>> GetNotificationItemsAsync(
    PortalContext context,
    string? locale,
    CancellationToken cancellationToken)
  {
    var page = await notificationRepository.ListAsync(
        new NotificationListRequestDto(PageSize: 20, Locale: locale),
        context.OrganizationId,
        context.UserId,
        cancellationToken)
      .ConfigureAwait(false);
    var items = page.Items.Select(snapshot => ToNotification(snapshot, locale)).ToArray();

    return new PagedResultDto<ResidentPortalNotificationDto>(items, page.Page, page.PageSize, items.Length);
  }

  private async Task<ResolvedOccurrenceContext> ResolveOccurrenceContextAsync(
    PortalContext context,
    ResidentPortalOccurrenceCreateRequestDto request,
    CancellationToken cancellationToken)
  {
    var errors = new List<ValidationFailure>();
    OccurrencePropertySnapshot? property = null;
    OccurrenceContractSnapshot? contract = null;
    EntityId? propertyId = ToEntityId(request.PropertyId);
    var contractId = ToEntityId(request.ContractId);

    if (contractId.HasValue)
    {
      contract = await occurrenceRepository.GetContractSnapshotAsync(contractId.Value, context.OrganizationId, cancellationToken)
        .ConfigureAwait(false);
      if (contract is null || !contract.ResidentIds.Contains(context.Resident.Id))
      {
        errors.Add(new ValidationFailure(nameof(request.ContractId), "validation.contract"));
      }
      else
      {
        propertyId ??= contract.PropertyId;
      }
    }

    if (propertyId.HasValue)
    {
      property = await occurrenceRepository.GetPropertySnapshotAsync(propertyId.Value, context.OrganizationId, cancellationToken)
        .ConfigureAwait(false);
      if (property is null)
      {
        errors.Add(new ValidationFailure(nameof(request.PropertyId), "validation.property"));
      }
    }

    if (!contractId.HasValue && !propertyId.HasValue)
    {
      errors.Add(new ValidationFailure(nameof(request.PropertyId), "validation.occurrenceLink"));
    }

    return new ResolvedOccurrenceContext(errors, propertyId, contractId, property, contract);
  }

  private async Task<IReadOnlyList<ValidationFailure>> ValidateResidentDocumentLinksAsync(
    PortalContext context,
    ResidentPortalDocumentUploadRequestDto request,
    CancellationToken cancellationToken)
  {
    var errors = new List<ValidationFailure>();

    if (request.ContractId.HasValue)
    {
      var contract = await contractRepository.FindSnapshotAsync(
          new EntityId(request.ContractId.Value),
          context.OrganizationId,
          includeArchived: true,
          cancellationToken)
        .ConfigureAwait(false);
      if (contract is null || !contract.Residents.Any(resident => resident.ResidentId == context.Resident.Id))
      {
        errors.Add(new ValidationFailure(nameof(request.ContractId), "validation.contract"));
      }
    }

    if (request.PropertyId.HasValue)
    {
      var contracts = await GetContractItemsAsync(context, null, cancellationToken).ConfigureAwait(false);
      if (!contracts.Items.Any(contract => contract.Property.Id == request.PropertyId.Value))
      {
        errors.Add(new ValidationFailure(nameof(request.PropertyId), "validation.property"));
      }
    }

    return errors;
  }

  private static DocumentLinkDraft[] BuildResidentDocumentLinks(
    PortalContext context,
    ResidentPortalDocumentUploadRequestDto request)
  {
    var links = new List<DocumentLinkDraft>
    {
      new("resident", context.Resident.Id, context.Resident.FullName)
    };

    if (request.ContractId.HasValue)
    {
      links.Add(new DocumentLinkDraft("contract", new EntityId(request.ContractId.Value), null));
    }

    if (request.PropertyId.HasValue)
    {
      links.Add(new DocumentLinkDraft("property", new EntityId(request.PropertyId.Value), null));
    }

    return links.ToArray();
  }

  private async Task WriteResidentEventAsync(
    string eventName,
    string module,
    EntityReference subject,
    PortalContext context,
    IReadOnlyList<EntityReference> related,
    CancellationToken cancellationToken)
  {
    var envelope = ModuleEventEnvelope.Create(
      context.OrganizationId,
      module,
      eventName,
      timeProvider.GetUtcNow(),
      EventActor.Resident(context.UserId, context.User.DisplayName),
      subject,
      ModuleEventConsumer.Audit |
      ModuleEventConsumer.Timeline |
      ModuleEventConsumer.Notifications |
      ModuleEventConsumer.ResidentPortal,
      new Dictionary<string, string>(StringComparer.Ordinal)
      {
        ["residentId"] = context.Resident.Id.Value.ToString("D")
      },
      related);

    await auditWriter.WriteAsync(AuditEntryDraft.FromModuleEvent(envelope), cancellationToken)
      .ConfigureAwait(false);
    await outboxWriter.EnqueueAsync(envelope, cancellationToken).ConfigureAwait(false);
  }

  private static ResidentPortalProfileDto ToProfile(Resident resident, string? locale) =>
    new(
      resident.Id.Value,
      resident.FullName,
      resident.PreferredName,
      resident.Email,
      resident.Phone,
      resident.SecondaryPhone,
      ResidentCatalog.GetStatusLabel(resident.Status, locale),
      ResidentCatalog.GetPortalStatusLabel(resident.PortalStatus, locale));

  private static ResidentPortalPropertyDto ToProperty(
    Domain.Properties.RentalProperty property,
    string? locale) =>
    new(
      property.Id.Value,
      property.Name,
      property.Description,
      property.Address.StreetLine,
      property.Address.Number,
      property.Address.Complement,
      property.Address.Neighborhood,
      property.Address.City,
      property.Address.StateCode,
      property.Address.PostalCode,
      PropertyCatalog.GetStatusLabel(property.Status, locale),
      ToMoneyDto(property.SuggestedRent),
      property.GarageSpaceCount);

  private static ResidentPortalContractDto ToContract(ContractSnapshot snapshot, string? locale) =>
    new(
      snapshot.Contract.Id.Value,
      new ResidentPortalEntitySummaryDto(
        snapshot.Property.PropertyId.Value,
        snapshot.Property.PropertyName,
        snapshot.Property.Location,
        $"/portal/imovel"),
      ContractCatalog.GetStatusLabel(snapshot.Contract.GetEffectiveStatus(TodayStatic()), locale),
      snapshot.Contract.StartDate,
      snapshot.Contract.EndDate,
      ToMoneyDto(snapshot.Contract.MonthlyRent),
      snapshot.Contract.DueDay,
      snapshot.Contract.IsDeleted);

  private static ResidentPortalPaymentDto ToPayment(
    PaymentSnapshot snapshot,
    string? locale,
    DateOnly today)
  {
    var charge = snapshot.Charge;
    var status = charge.GetEffectiveStatus(today);
    return new ResidentPortalPaymentDto(
      charge.Id.Value,
      charge.Title,
      charge.Description,
      snapshot.Contract is null
        ? null
        : new ResidentPortalEntitySummaryDto(
          snapshot.Contract.ContractId.Value,
          snapshot.Contract.DisplayName,
          snapshot.Contract.PropertyName,
          "/portal/contratos"),
      snapshot.Property is null
        ? null
        : new ResidentPortalEntitySummaryDto(
          snapshot.Property.PropertyId.Value,
          snapshot.Property.Name,
          snapshot.Property.Location,
          "/portal/imovel"),
      charge.DueDate,
      PaymentCatalog.ToStatusLabel(status, locale),
      ToMoneyDto(charge.Amount),
      ToMoneyDto(charge.SettledAmount()),
      ToMoneyDto(charge.Balance()),
      PaymentCatalog.ToMethodCode(charge.PreferredMethod),
      PaymentCatalog.ToMethodLabel(charge.PreferredMethod, locale),
      charge.ProviderCode,
      charge.ProviderReference,
      status == PaymentStatus.Overdue);
  }

  private static ResidentPortalDocumentDto ToDocument(DocumentRecord document, string? locale)
  {
    var id = Uri.EscapeDataString(document.Id.Value.ToString("D"));
    return new ResidentPortalDocumentDto(
      document.Id.Value,
      document.Title,
      document.Description,
      document.CurrentFileName,
      document.CurrentContentType,
      document.CurrentSizeBytes,
      DocumentCatalog.ToCategoryCode(document.Category),
      DocumentCatalog.GetCategoryLabel(document.Category, locale),
      DocumentCatalog.GetStatusLabel(document.Status, locale),
      document.CurrentUploadedAt,
      $"/v1/resident-portal/documents/{id}/download");
  }

  private static ResidentPortalOccurrenceDto ToOccurrence(OccurrenceSnapshot snapshot, string? locale)
  {
    var occurrence = snapshot.Occurrence;
    return new ResidentPortalOccurrenceDto(
      occurrence.Id.Value,
      occurrence.Title,
      occurrence.Description,
      OccurrenceCatalog.ToTypeLabel(occurrence.Type, locale),
      OccurrenceCatalog.ToPriorityLabel(occurrence.Priority, locale),
      OccurrenceCatalog.ToStatusLabel(occurrence.EffectiveStatus, locale),
      snapshot.Property is null
        ? null
        : new ResidentPortalEntitySummaryDto(
          snapshot.Property.PropertyId.Value,
          snapshot.Property.Name,
          snapshot.Property.Location,
          "/portal/imovel"),
      snapshot.Contract is null
        ? null
        : new ResidentPortalEntitySummaryDto(
          snapshot.Contract.ContractId.Value,
          snapshot.Contract.DisplayName,
          snapshot.Contract.PropertyName,
          "/portal/contratos"),
      occurrence.DueDate,
      occurrence.IsUnresolved,
      occurrence.CreatedAt);
  }

  private static ResidentPortalInspectionDto ToInspection(InspectionSnapshot snapshot, string? locale)
  {
    var inspection = snapshot.Inspection;
    var total = inspection.ChecklistItems.Count;
    var completed = inspection.ChecklistItems.Count(item => item.IsComplete);
    return new ResidentPortalInspectionDto(
      inspection.Id.Value,
      inspection.Title,
      InspectionCatalog.ToTypeLabel(inspection.Type, locale),
      InspectionCatalog.ToStatusLabel(inspection.Status, locale),
      new ResidentPortalEntitySummaryDto(
        snapshot.Property.PropertyId.Value,
        snapshot.Property.Name,
        snapshot.Property.Location,
        "/portal/imovel"),
      snapshot.Contract is null
        ? null
        : new ResidentPortalEntitySummaryDto(
          snapshot.Contract.ContractId.Value,
          snapshot.Contract.DisplayName,
          snapshot.Contract.PropertyName,
          "/portal/contratos"),
      inspection.ScheduledAt,
      inspection.CompletedAt,
      total,
      completed,
      total == 0 ? 0m : Math.Round(completed * 100m / total, 2));
  }

  private static ResidentPortalNotificationDto ToNotification(
    NotificationRecordSnapshot snapshot,
    string? locale)
  {
    var category = NotificationCatalog.GetCategoryLabel(snapshot.Category, locale);
    return new ResidentPortalNotificationDto(
      snapshot.Id,
      category,
      snapshot.Payload.TryGetValue("title", out var title) ? title : category.Label,
      snapshot.Payload.TryGetValue("message", out var message)
        ? message
        : snapshot.SubjectDisplayName ?? snapshot.EventName,
      snapshot.Payload.TryGetValue("deepLink", out var deepLink) ? deepLink : null,
      snapshot.IsRead,
      snapshot.CreatedAt);
  }

  private static ResidentPortalSettingsSummaryDto ToSettings(OrganizationSettings settings) =>
    new(
      settings.ResidentPortalEnabled,
      settings.ResidentOccurrenceCreationEnabled,
      settings.ResidentDocumentUploadEnabled,
      settings.ResidentProfileUpdateRequestEnabled);

  private static bool PaymentBelongsToResident(PaymentSnapshot snapshot, EntityId residentId) =>
    snapshot.Charge.ResidentId == residentId ||
    snapshot.Contract?.PrimaryResidentId == residentId;

  private static bool IsDocumentVisibleToResident(
    DocumentRecord document,
    EntityId residentId,
    PagedResultDto<ResidentPortalContractDto> contracts,
    PagedResultDto<ResidentPortalPaymentDto> payments)
  {
    var contractIds = contracts.Items.Select(contract => contract.Id).ToHashSet();
    var propertyIds = contracts.Items.Select(contract => contract.Property.Id).ToHashSet();
    var paymentIds = payments.Items.Select(payment => payment.Id).ToHashSet();

    foreach (var link in document.Links)
    {
      if (link.EntityType == "resident" && link.EntityId == residentId)
      {
        return true;
      }

      if (link.EntityType == "contract" && contractIds.Contains(link.EntityId.Value))
      {
        return true;
      }

      if (link.EntityType == "property" && propertyIds.Contains(link.EntityId.Value))
      {
        return true;
      }

      if (link.EntityType == "payment" && paymentIds.Contains(link.EntityId.Value))
      {
        return true;
      }
    }

    return false;
  }

  private static PaymentMoneyDto ToMoneyDto(Money money) => new(money.Amount, money.Currency);

  private static DateOnly TodayStatic() => DateOnly.FromDateTime(DateTimeOffset.UtcNow.UtcDateTime);

  private DateOnly Today() => DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);

  private static EntityId? ToEntityId(Guid? id) =>
    id.HasValue && id.Value != Guid.Empty ? new EntityId(id.Value) : null;

  private static IEnumerable<ValidationFailure> ValidateOccurrenceRequest(ResidentPortalOccurrenceCreateRequestDto request)
  {
    if (string.IsNullOrWhiteSpace(request.Title))
    {
      yield return new ValidationFailure(nameof(request.Title), ValidationMessageKeys.Required);
    }

    if (string.IsNullOrWhiteSpace(request.Description))
    {
      yield return new ValidationFailure(nameof(request.Description), ValidationMessageKeys.Required);
    }

    if (!OccurrenceCatalog.TryParseType(request.Type, out _))
    {
      yield return new ValidationFailure(nameof(request.Type), "validation.occurrenceType");
    }

    if (!OccurrenceCatalog.TryParsePriority(request.Priority, out _))
    {
      yield return new ValidationFailure(nameof(request.Priority), "validation.occurrencePriority");
    }

    foreach (var failure in ValidateId(request.PropertyId, nameof(request.PropertyId)))
    {
      yield return failure;
    }

    foreach (var failure in ValidateId(request.ContractId, nameof(request.ContractId)))
    {
      yield return failure;
    }
  }

  private static IEnumerable<ValidationFailure> ValidateDocumentUpload(ResidentPortalDocumentUploadRequestDto request)
  {
    if (!DocumentCatalog.TryParseCategory(request.Category, out _))
    {
      yield return new ValidationFailure(nameof(request.Category), "validation.documentCategory");
    }

    if (string.IsNullOrWhiteSpace(request.Title))
    {
      yield return new ValidationFailure(nameof(request.Title), ValidationMessageKeys.Required);
    }

    if (request.SizeBytes <= 0 || request.SizeBytes > DocumentCatalog.MaxFileSizeBytes)
    {
      yield return new ValidationFailure(nameof(request.SizeBytes), "validation.fileSize");
    }

    if (!DocumentCatalog.IsAllowedFile(request.FileName, request.ContentType))
    {
      yield return new ValidationFailure(nameof(request.FileName), "validation.fileType");
    }

    foreach (var failure in ValidateId(request.ContractId, nameof(request.ContractId)))
    {
      yield return failure;
    }

    foreach (var failure in ValidateId(request.PropertyId, nameof(request.PropertyId)))
    {
      yield return failure;
    }
  }

  private static IEnumerable<ValidationFailure> ValidateId(Guid? id, string name)
  {
    if (id == Guid.Empty)
    {
      yield return new ValidationFailure(name, ValidationMessageKeys.InvalidId);
    }
  }

  private static OccurrenceType ParseOccurrenceType(string value) =>
    OccurrenceCatalog.TryParseType(value, out var type)
      ? type
      : throw new ArgumentException("Invalid occurrence type.", nameof(value));

  private static OccurrencePriority ParseOccurrencePriority(string value) =>
    OccurrenceCatalog.TryParsePriority(value, out var priority)
      ? priority
      : throw new ArgumentException("Invalid occurrence priority.", nameof(value));

  private static DocumentCategory ParseDocumentCategory(string value) =>
    DocumentCatalog.TryParseCategory(value, out var category)
      ? category
      : throw new ArgumentException("Invalid document category.", nameof(value));

  private sealed record PortalContext(
    ActiveOrganizationContext ActiveOrganizationContext,
    Resident Resident,
    ResidentAccountLink Link,
    OrganizationSettings Settings)
  {
    public OrganizationId OrganizationId => ActiveOrganizationContext.OrganizationId;

    public UserId UserId => ActiveOrganizationContext.UserId;

    public Alsappan.Application.Common.Auth.AuthenticatedUser User => ActiveOrganizationContext.User;
  }

  private sealed record PortalContextResult(
    bool Succeeded,
    ApplicationOperationFailure Failure,
    PortalContext? Context = null)
  {
    public static PortalContextResult Success(PortalContext context) =>
      new(true, ApplicationOperationFailure.None, context);

    public static PortalContextResult Failed(ApplicationOperationFailure failure) =>
      new(false, failure);
  }

  private sealed record ResolvedOccurrenceContext(
    IReadOnlyList<ValidationFailure> Errors,
    EntityId? PropertyId,
    EntityId? ContractId,
    OccurrencePropertySnapshot? Property,
    OccurrenceContractSnapshot? Contract);
}
