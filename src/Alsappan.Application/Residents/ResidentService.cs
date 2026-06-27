using Alsappan.Application.Common.Audit;
using Alsappan.Application.Common.Authorization;
using Alsappan.Application.Common.Contracts;
using Alsappan.Application.Common.Events;
using Alsappan.Application.Common.Results;
using Alsappan.Application.Common.Tenancy;
using Alsappan.Application.Common.Validation;
using Alsappan.Application.Residents.Repositories;
using Alsappan.Domain.Common.Events;
using Alsappan.Domain.Common.Identifiers;
using Alsappan.Domain.Residents;

namespace Alsappan.Application.Residents;

public sealed class ResidentService : IResidentService
{
  private readonly IResidentRepository residentRepository;
  private readonly IPermissionService permissionService;
  private readonly IActiveOrganizationContextResolver activeOrganizationContextResolver;
  private readonly IAuditWriter auditWriter;
  private readonly IModuleEventOutboxWriter outboxWriter;
  private readonly TimeProvider timeProvider;

  public ResidentService(
    IResidentRepository residentRepository,
    IPermissionService permissionService,
    IActiveOrganizationContextResolver activeOrganizationContextResolver,
    IAuditWriter auditWriter,
    IModuleEventOutboxWriter outboxWriter,
    TimeProvider? timeProvider = null)
  {
    this.residentRepository = residentRepository ?? throw new ArgumentNullException(nameof(residentRepository));
    this.permissionService = permissionService ?? throw new ArgumentNullException(nameof(permissionService));
    this.activeOrganizationContextResolver = activeOrganizationContextResolver
      ?? throw new ArgumentNullException(nameof(activeOrganizationContextResolver));
    this.auditWriter = auditWriter ?? throw new ArgumentNullException(nameof(auditWriter));
    this.outboxWriter = outboxWriter ?? throw new ArgumentNullException(nameof(outboxWriter));
    this.timeProvider = timeProvider ?? TimeProvider.System;
  }

  public async Task<ApplicationOperationResult<PagedResultDto<ResidentListItemDto>>> ListAsync(
    ResidentListRequestDto request,
    CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(request);

    var context = await AuthorizeContextAsync(PermissionCodes.Read(PermissionModules.Residents), cancellationToken)
      .ConfigureAwait(false);
    if (context is null)
    {
      return ApplicationOperationResult<PagedResultDto<ResidentListItemDto>>.Failed(
        ApplicationOperationFailure.Forbidden);
    }

    var canViewSensitive = await CanViewSensitiveFieldsAsync(cancellationToken).ConfigureAwait(false);
    var page = await residentRepository.ListAsync(request, context.OrganizationId, cancellationToken)
      .ConfigureAwait(false);
    var items = page.Items.Select(resident => ToListItem(resident, request.Locale, canViewSensitive)).ToArray();

    return ApplicationOperationResult<PagedResultDto<ResidentListItemDto>>.Success(
      new PagedResultDto<ResidentListItemDto>(items, page.Page, page.PageSize, page.TotalItems));
  }

  public async Task<ApplicationOperationResult<ResidentDetailDto>> GetAsync(
    Guid id,
    string? locale = null,
    CancellationToken cancellationToken = default)
  {
    var validation = ValidateId(id).ToList();
    if (validation.Count > 0)
    {
      return ApplicationOperationResult<ResidentDetailDto>.Invalid(validation);
    }

    var context = await AuthorizeContextAsync(PermissionCodes.Read(PermissionModules.Residents), cancellationToken)
      .ConfigureAwait(false);
    if (context is null)
    {
      return ApplicationOperationResult<ResidentDetailDto>.Failed(ApplicationOperationFailure.Forbidden);
    }

    var resident = await residentRepository.FindAsync(
        new EntityId(id),
        context.OrganizationId,
        includeArchived: true,
        cancellationToken)
      .ConfigureAwait(false);
    if (resident is null)
    {
      return ApplicationOperationResult<ResidentDetailDto>.Failed(ApplicationOperationFailure.NotFound);
    }

    var canViewSensitive = await CanViewSensitiveFieldsAsync(cancellationToken).ConfigureAwait(false);
    return ApplicationOperationResult<ResidentDetailDto>.Success(ToDetail(resident, locale, canViewSensitive));
  }

  public async Task<ApplicationOperationResult<ResidentDetailDto>> CreateAsync(
    ResidentCreateRequestDto request,
    string? locale = null,
    CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(request);

    var context = await AuthorizeContextAsync(PermissionCodes.Write(PermissionModules.Residents), cancellationToken)
      .ConfigureAwait(false);
    if (context is null)
    {
      return ApplicationOperationResult<ResidentDetailDto>.Failed(ApplicationOperationFailure.Forbidden);
    }

    var validation = ValidateMutationRequest(request).ToList();
    if (validation.Count > 0)
    {
      return ApplicationOperationResult<ResidentDetailDto>.Invalid(validation);
    }

    var now = timeProvider.GetUtcNow();
    var resident = Resident.Create(
      EntityId.New(),
      context.OrganizationId,
      request.FullName,
      request.PreferredName,
      request.Email,
      request.Phone,
      request.SecondaryPhone,
      request.DocumentType,
      request.DocumentIdentifier,
      request.BirthDate,
      request.EmergencyContact?.Name,
      request.EmergencyContact?.Relationship,
      request.EmergencyContact?.Phone,
      ParseMutableStatus(request.Status),
      ParsePortalStatus(request.PortalStatus),
      ParsePrivacyFlags(request.PrivacyFlags),
      request.Notes,
      ToUserId(request.LinkedUserId),
      now,
      context.UserId);

    await residentRepository.AddAsync(resident, cancellationToken).ConfigureAwait(false);
    await WriteMutationSideEffectsAsync("resident.created", resident, context, cancellationToken)
      .ConfigureAwait(false);

    return ApplicationOperationResult<ResidentDetailDto>.Success(ToDetail(resident, locale, canViewSensitive: true));
  }

  public async Task<ApplicationOperationResult<ResidentDetailDto>> UpdateAsync(
    Guid id,
    ResidentUpdateRequestDto request,
    string? locale = null,
    CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(request);

    var validation = ValidateId(id).Concat(ValidateMutationRequest(request)).ToList();
    if (validation.Count > 0)
    {
      return ApplicationOperationResult<ResidentDetailDto>.Invalid(validation);
    }

    var context = await AuthorizeContextAsync(PermissionCodes.Write(PermissionModules.Residents), cancellationToken)
      .ConfigureAwait(false);
    if (context is null)
    {
      return ApplicationOperationResult<ResidentDetailDto>.Failed(ApplicationOperationFailure.Forbidden);
    }

    var resident = await residentRepository.FindAsync(
        new EntityId(id),
        context.OrganizationId,
        includeArchived: false,
        cancellationToken)
      .ConfigureAwait(false);
    if (resident is null)
    {
      return ApplicationOperationResult<ResidentDetailDto>.Failed(ApplicationOperationFailure.NotFound);
    }

    resident.Update(
      request.FullName,
      request.PreferredName,
      request.Email,
      request.Phone,
      request.SecondaryPhone,
      request.DocumentType,
      request.DocumentIdentifier,
      request.BirthDate,
      request.EmergencyContact?.Name,
      request.EmergencyContact?.Relationship,
      request.EmergencyContact?.Phone,
      ParseMutableStatus(request.Status),
      ParsePortalStatus(request.PortalStatus),
      ParsePrivacyFlags(request.PrivacyFlags),
      request.Notes,
      ToUserId(request.LinkedUserId),
      timeProvider.GetUtcNow(),
      context.UserId);

    await residentRepository.UpdateAsync(resident, cancellationToken).ConfigureAwait(false);
    await WriteMutationSideEffectsAsync("resident.updated", resident, context, cancellationToken)
      .ConfigureAwait(false);

    return ApplicationOperationResult<ResidentDetailDto>.Success(ToDetail(resident, locale, canViewSensitive: true));
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

    var context = await AuthorizeContextAsync(PermissionCodes.Archive(PermissionModules.Residents), cancellationToken)
      .ConfigureAwait(false);
    if (context is null)
    {
      return ApplicationOperationResult.Failed(ApplicationOperationFailure.Forbidden);
    }

    var resident = await residentRepository.FindAsync(
        new EntityId(id),
        context.OrganizationId,
        includeArchived: false,
        cancellationToken)
      .ConfigureAwait(false);
    if (resident is null)
    {
      return ApplicationOperationResult.Failed(ApplicationOperationFailure.NotFound);
    }

    resident.Archive(timeProvider.GetUtcNow(), context.UserId);
    await residentRepository.UpdateAsync(resident, cancellationToken).ConfigureAwait(false);
    await WriteMutationSideEffectsAsync("resident.archived", resident, context, cancellationToken)
      .ConfigureAwait(false);

    return ApplicationOperationResult.Success();
  }

  public async Task<ApplicationOperationResult<ResidentDetailDto>> RestoreAsync(
    Guid id,
    string? locale = null,
    CancellationToken cancellationToken = default)
  {
    var validation = ValidateId(id).ToList();
    if (validation.Count > 0)
    {
      return ApplicationOperationResult<ResidentDetailDto>.Invalid(validation);
    }

    var context = await AuthorizeContextAsync(PermissionCodes.Archive(PermissionModules.Residents), cancellationToken)
      .ConfigureAwait(false);
    if (context is null)
    {
      return ApplicationOperationResult<ResidentDetailDto>.Failed(ApplicationOperationFailure.Forbidden);
    }

    var resident = await residentRepository.FindAsync(
        new EntityId(id),
        context.OrganizationId,
        includeArchived: true,
        cancellationToken)
      .ConfigureAwait(false);
    if (resident is null)
    {
      return ApplicationOperationResult<ResidentDetailDto>.Failed(ApplicationOperationFailure.NotFound);
    }

    resident.Restore(timeProvider.GetUtcNow(), context.UserId);
    await residentRepository.UpdateAsync(resident, cancellationToken).ConfigureAwait(false);
    await WriteMutationSideEffectsAsync("resident.restored", resident, context, cancellationToken)
      .ConfigureAwait(false);

    return ApplicationOperationResult<ResidentDetailDto>.Success(ToDetail(resident, locale, canViewSensitive: true));
  }

  public async Task<ApplicationOperationResult<IReadOnlyList<ResidentDuplicateWarningDto>>> GetDuplicateWarningsAsync(
    ResidentDuplicateWarningRequestDto request,
    CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(request);

    var validation = ValidateDuplicateRequest(request).ToList();
    if (validation.Count > 0)
    {
      return ApplicationOperationResult<IReadOnlyList<ResidentDuplicateWarningDto>>.Invalid(validation);
    }

    var context = await AuthorizeContextAsync(PermissionCodes.Write(PermissionModules.Residents), cancellationToken)
      .ConfigureAwait(false);
    if (context is null)
    {
      return ApplicationOperationResult<IReadOnlyList<ResidentDuplicateWarningDto>>.Failed(
        ApplicationOperationFailure.Forbidden);
    }

    var candidates = await residentRepository.FindPotentialDuplicatesAsync(
        request,
        context.OrganizationId,
        cancellationToken)
      .ConfigureAwait(false);
    var warnings = BuildDuplicateWarnings(request, candidates);

    return ApplicationOperationResult<IReadOnlyList<ResidentDuplicateWarningDto>>.Success(warnings);
  }

  public Task<IReadOnlyList<StatusLabelDto>> GetStatusOptionsAsync(
    string? locale = null,
    CancellationToken cancellationToken = default)
  {
    cancellationToken.ThrowIfCancellationRequested();
    return Task.FromResult(ResidentCatalog.GetStatusOptions(locale));
  }

  public Task<IReadOnlyList<StatusLabelDto>> GetPortalStatusOptionsAsync(
    string? locale = null,
    CancellationToken cancellationToken = default)
  {
    cancellationToken.ThrowIfCancellationRequested();
    return Task.FromResult(ResidentCatalog.GetPortalStatusOptions(locale));
  }

  public Task<IReadOnlyList<SelectOptionDto>> GetPrivacyFlagOptionsAsync(
    string? locale = null,
    CancellationToken cancellationToken = default)
  {
    cancellationToken.ThrowIfCancellationRequested();
    return Task.FromResult(ResidentCatalog.GetPrivacyFlagOptions(locale));
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

  private async Task<bool> CanViewSensitiveFieldsAsync(CancellationToken cancellationToken)
  {
    var permissions = await permissionService.GetEffectivePermissionsAsync(cancellationToken).ConfigureAwait(false);
    return permissions.Contains(PermissionCodes.Wildcard) ||
      permissions.Contains(PermissionCodes.Write(PermissionModules.Residents)) ||
      permissions.Contains(PermissionCodes.Manage(PermissionModules.Residents));
  }

  private async Task WriteMutationSideEffectsAsync(
    string action,
    Resident resident,
    ActiveOrganizationContext context,
    CancellationToken cancellationToken)
  {
    var now = timeProvider.GetUtcNow();
    var subject = EntityReference.FromGuid("resident", resident.Id.Value, resident.FullName);
    var actor = EventActor.User(context.UserId, context.User.DisplayName);
    var data = new Dictionary<string, string>(StringComparer.Ordinal)
    {
      ["status"] = ResidentCatalog.ToStatusCode(resident.Status),
      ["portalStatus"] = ResidentCatalog.ToPortalStatusCode(resident.PortalStatus),
      ["hasPortalAccess"] = resident.HasPortalAccess.ToString(System.Globalization.CultureInfo.InvariantCulture)
    };

    var envelope = ModuleEventEnvelope.Create(
      context.OrganizationId,
      "residents",
      action,
      now,
      actor,
      subject,
      ModuleEventConsumer.Audit | ModuleEventConsumer.Timeline | ModuleEventConsumer.Notifications,
      data);

    await auditWriter.WriteAsync(AuditEntryDraft.FromModuleEvent(envelope), cancellationToken)
      .ConfigureAwait(false);
    await outboxWriter.EnqueueAsync(envelope, cancellationToken).ConfigureAwait(false);
  }

  private static ResidentListItemDto ToListItem(Resident resident, string? locale, bool canViewSensitive)
  {
    var masked = !canViewSensitive;
    return new ResidentListItemDto(
      resident.Id.Value,
      resident.FullName,
      resident.PreferredName,
      Mask(resident.Email, masked),
      Mask(resident.Phone, masked),
      Mask(resident.SecondaryPhone, masked),
      Mask(resident.DocumentType, masked),
      Mask(resident.DocumentIdentifier, masked),
      ToEmergencyContactDto(resident, masked),
      ResidentCatalog.GetStatusLabel(resident.Status, locale),
      ResidentCatalog.GetPortalStatusLabel(resident.PortalStatus, locale),
      ResidentCatalog.ToPrivacyFlagCodes(resident.PrivacyFlags),
      ToContactSummary(resident, locale, masked),
      masked,
      resident.IsDeleted,
      resident.CreatedAt,
      resident.UpdatedAt);
  }

  private static ResidentDetailDto ToDetail(Resident resident, string? locale, bool canViewSensitive)
  {
    var listItem = ToListItem(resident, locale, canViewSensitive);
    var masked = !canViewSensitive;

    return new ResidentDetailDto(
      listItem.Id,
      listItem.FullName,
      listItem.PreferredName,
      listItem.Email,
      listItem.Phone,
      listItem.SecondaryPhone,
      listItem.DocumentType,
      listItem.DocumentIdentifier,
      masked ? null : resident.BirthDate,
      listItem.EmergencyContact,
      listItem.Status,
      listItem.PortalStatus,
      listItem.PrivacyFlags,
      Mask(resident.Notes, masked && resident.PrivacyFlags.HasFlag(ResidentPrivacyOptions.Notes)),
      masked ? null : resident.LinkedUserId?.Value,
      listItem.ContactSummary,
      listItem.IsSensitiveMasked,
      RelationshipSummaries(locale),
      resident.CreatedAt,
      resident.UpdatedAt,
      resident.DeletedAt,
      resident.ConcurrencyToken.Value);
  }

  private static IReadOnlyList<ResidentRelationshipSummaryDto> RelationshipSummaries(string? locale)
  {
    var portuguese = IsPortuguese(locale);

    return
    [
      new("contracts", portuguese ? "Contratos" : "Contracts", 0, "/contratos"),
      new("properties", portuguese ? "Imoveis" : "Properties", 0, "/imoveis"),
      new("payments", portuguese ? "Pagamentos" : "Payments", 0, "/pagamentos"),
      new("documents", portuguese ? "Documentos" : "Documents", 0, "/documentos"),
      new("pets", "Pets", 0, "/pets"),
      new("vehicles", portuguese ? "Veiculos" : "Vehicles", 0, "/veiculos"),
      new("occurrences", portuguese ? "Ocorrencias" : "Occurrences", 0, "/ocorrencias"),
      new("timeline", "Timeline", 0, "/timeline"),
      new("audit", portuguese ? "Auditoria" : "Audit", 0, "/auditoria")
    ];
  }

  private static List<ResidentDuplicateWarningDto> BuildDuplicateWarnings(
    ResidentDuplicateWarningRequestDto request,
    IReadOnlyList<Resident> candidates)
  {
    var warnings = new List<ResidentDuplicateWarningDto>();
    var normalizedEmail = ResidentCode.NormalizeEmail(request.Email);
    var normalizedPhone = ResidentCode.NormalizePhone(request.Phone);
    var normalizedDocument = ResidentCode.NormalizeIdentifier(request.DocumentIdentifier);
    var seen = new HashSet<string>(StringComparer.Ordinal);

    foreach (var candidate in candidates)
    {
      AddWarningIfMatches("email", request.Email, normalizedEmail, candidate.NormalizedEmail, candidate);
      AddWarningIfMatches("phone", request.Phone, normalizedPhone, candidate.NormalizedPhone, candidate);
      AddWarningIfMatches("documentIdentifier", request.DocumentIdentifier, normalizedDocument, candidate.NormalizedDocumentIdentifier, candidate);
    }

    return warnings;

    void AddWarningIfMatches(
      string field,
      string? originalValue,
      string? normalizedValue,
      string? candidateValue,
      Resident candidate)
    {
      if (string.IsNullOrWhiteSpace(originalValue) ||
        string.IsNullOrWhiteSpace(normalizedValue) ||
        !string.Equals(normalizedValue, candidateValue, StringComparison.OrdinalIgnoreCase))
      {
        return;
      }

      var key = $"{field}:{candidate.Id.Value:D}";
      if (!seen.Add(key))
      {
        return;
      }

      warnings.Add(new ResidentDuplicateWarningDto(
        field,
        originalValue.Trim(),
        candidate.Id.Value,
        candidate.FullName,
        $"Possivel morador duplicado: {candidate.FullName}."));
    }
  }

  private static IEnumerable<ValidationFailure> ValidateMutationRequest(ResidentCreateRequestDto request)
  {
    if (string.IsNullOrWhiteSpace(request.FullName))
    {
      yield return new ValidationFailure(nameof(request.FullName), ValidationMessageKeys.Required);
    }

    if (!string.IsNullOrWhiteSpace(request.Email) && !request.Email.Contains('@', StringComparison.Ordinal))
    {
      yield return new ValidationFailure(nameof(request.Email), ValidationMessageKeys.Email);
    }

    if (!ResidentCatalog.TryParseMutableStatus(request.Status, out _))
    {
      yield return new ValidationFailure(nameof(request.Status), "validation.status");
    }

    if (!ResidentCatalog.TryParsePortalStatus(request.PortalStatus, out _))
    {
      yield return new ValidationFailure(nameof(request.PortalStatus), "validation.portalStatus");
    }

    if (!ResidentCatalog.TryParsePrivacyFlags(request.PrivacyFlags, out _))
    {
      yield return new ValidationFailure(nameof(request.PrivacyFlags), "validation.privacyFlags");
    }

    if (request.LinkedUserId == Guid.Empty)
    {
      yield return new ValidationFailure(nameof(request.LinkedUserId), ValidationMessageKeys.InvalidId);
    }
  }

  private static IEnumerable<ValidationFailure> ValidateMutationRequest(ResidentUpdateRequestDto request) =>
    ValidateMutationRequest(new ResidentCreateRequestDto(
      request.FullName,
      request.PreferredName,
      request.Email,
      request.Phone,
      request.SecondaryPhone,
      request.DocumentType,
      request.DocumentIdentifier,
      request.BirthDate,
      request.EmergencyContact,
      request.Status,
      request.PortalStatus,
      request.PrivacyFlags,
      request.Notes,
      request.LinkedUserId));

  private static IEnumerable<ValidationFailure> ValidateDuplicateRequest(ResidentDuplicateWarningRequestDto request)
  {
    if (string.IsNullOrWhiteSpace(request.Email) &&
      string.IsNullOrWhiteSpace(request.Phone) &&
      string.IsNullOrWhiteSpace(request.DocumentIdentifier))
    {
      yield return new ValidationFailure("duplicate", ValidationMessageKeys.Required);
    }

    if (request.IgnoreResidentId == Guid.Empty)
    {
      yield return new ValidationFailure(nameof(request.IgnoreResidentId), ValidationMessageKeys.InvalidId);
    }
  }

  private static IEnumerable<ValidationFailure> ValidateId(Guid id)
  {
    if (id == Guid.Empty)
    {
      yield return new ValidationFailure(nameof(id), ValidationMessageKeys.InvalidId);
    }
  }

  private static ResidentStatus ParseMutableStatus(string value) =>
    ResidentCatalog.TryParseMutableStatus(value, out var status)
      ? status
      : throw new ArgumentException("Invalid resident status.", nameof(value));

  private static ResidentPortalStatus ParsePortalStatus(string value) =>
    ResidentCatalog.TryParsePortalStatus(value, out var status)
      ? status
      : throw new ArgumentException("Invalid resident portal status.", nameof(value));

  private static ResidentPrivacyOptions ParsePrivacyFlags(IReadOnlyList<string>? values) =>
    ResidentCatalog.TryParsePrivacyFlags(values, out var flags)
      ? flags
      : throw new ArgumentException("Invalid resident privacy flags.", nameof(values));

  private static UserId? ToUserId(Guid? userId) =>
    userId.HasValue ? new UserId(userId.Value) : null;

  private static ResidentEmergencyContactDto ToEmergencyContactDto(Resident resident, bool masked) =>
    new(
      Mask(resident.EmergencyContactName, masked),
      Mask(resident.EmergencyContactRelationship, masked),
      Mask(resident.EmergencyContactPhone, masked));

  private static string ToContactSummary(Resident resident, string? locale, bool masked)
  {
    if (masked)
    {
      return IsPortuguese(locale) ? "Dados protegidos" : "Protected data";
    }

    var contact = resident.Email ?? resident.Phone ?? resident.SecondaryPhone;
    if (!string.IsNullOrWhiteSpace(contact))
    {
      return contact;
    }

    return IsPortuguese(locale) ? "Sem contato" : "No contact";
  }

  private static string? Mask(string? value, bool masked) =>
    masked && !string.IsNullOrWhiteSpace(value) ? "****" : value;

  private static bool IsPortuguese(string? locale) =>
    string.IsNullOrWhiteSpace(locale) || locale.StartsWith("pt", StringComparison.OrdinalIgnoreCase);
}
