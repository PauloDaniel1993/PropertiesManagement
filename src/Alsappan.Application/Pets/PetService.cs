using Alsappan.Application.Common.Audit;
using Alsappan.Application.Common.Authorization;
using Alsappan.Application.Common.Contracts;
using Alsappan.Application.Common.Events;
using Alsappan.Application.Common.Results;
using Alsappan.Application.Common.Tenancy;
using Alsappan.Application.Common.Validation;
using Alsappan.Application.Pets.Repositories;
using Alsappan.Domain.Common.Events;
using Alsappan.Domain.Common.Identifiers;
using Alsappan.Domain.Pets;

namespace Alsappan.Application.Pets;

public sealed class PetService : IPetService
{
  private readonly IPetRepository petRepository;
  private readonly IPermissionService permissionService;
  private readonly IActiveOrganizationContextResolver activeOrganizationContextResolver;
  private readonly IAuditWriter auditWriter;
  private readonly IModuleEventOutboxWriter outboxWriter;
  private readonly TimeProvider timeProvider;

  public PetService(
    IPetRepository petRepository,
    IPermissionService permissionService,
    IActiveOrganizationContextResolver activeOrganizationContextResolver,
    IAuditWriter auditWriter,
    IModuleEventOutboxWriter outboxWriter,
    TimeProvider timeProvider)
  {
    this.petRepository = petRepository ?? throw new ArgumentNullException(nameof(petRepository));
    this.permissionService = permissionService ?? throw new ArgumentNullException(nameof(permissionService));
    this.activeOrganizationContextResolver = activeOrganizationContextResolver ??
      throw new ArgumentNullException(nameof(activeOrganizationContextResolver));
    this.auditWriter = auditWriter ?? throw new ArgumentNullException(nameof(auditWriter));
    this.outboxWriter = outboxWriter ?? throw new ArgumentNullException(nameof(outboxWriter));
    this.timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
  }

  public async Task<ApplicationOperationResult<PagedResultDto<PetListItemDto>>> ListAsync(
    PetListRequestDto request,
    CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(request);

    var context = await AuthorizeContextAsync(PermissionCodes.Read(PermissionModules.Pets), cancellationToken)
      .ConfigureAwait(false);
    if (context is null)
    {
      return ApplicationOperationResult<PagedResultDto<PetListItemDto>>.Failed(
        ApplicationOperationFailure.Forbidden);
    }

    var page = await petRepository.ListAsync(request, context.OrganizationId, cancellationToken)
      .ConfigureAwait(false);
    var items = page.Items.Select(snapshot => ToListItem(snapshot, request.Locale)).ToArray();

    return ApplicationOperationResult<PagedResultDto<PetListItemDto>>.Success(
      new PagedResultDto<PetListItemDto>(items, page.Page, page.PageSize, page.TotalItems));
  }

  public async Task<ApplicationOperationResult<PetDetailDto>> GetAsync(
    Guid id,
    string? locale = null,
    CancellationToken cancellationToken = default)
  {
    var validation = ValidateId(id).ToList();
    if (validation.Count > 0)
    {
      return ApplicationOperationResult<PetDetailDto>.Invalid(validation);
    }

    var context = await AuthorizeContextAsync(PermissionCodes.Read(PermissionModules.Pets), cancellationToken)
      .ConfigureAwait(false);
    if (context is null)
    {
      return ApplicationOperationResult<PetDetailDto>.Failed(ApplicationOperationFailure.Forbidden);
    }

    var snapshot = await petRepository.FindSnapshotAsync(
        new EntityId(id),
        context.OrganizationId,
        includeArchived: true,
        cancellationToken)
      .ConfigureAwait(false);

    return snapshot is null
      ? ApplicationOperationResult<PetDetailDto>.Failed(ApplicationOperationFailure.NotFound)
      : ApplicationOperationResult<PetDetailDto>.Success(ToDetail(snapshot, locale));
  }

  public async Task<ApplicationOperationResult<PetDetailDto>> CreateAsync(
    PetCreateRequestDto request,
    string? locale = null,
    CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(request);

    var validation = ValidatePetRequest(request).ToList();
    if (validation.Count > 0)
    {
      return ApplicationOperationResult<PetDetailDto>.Invalid(validation);
    }

    var context = await AuthorizeContextAsync(PermissionCodes.Write(PermissionModules.Pets), cancellationToken)
      .ConfigureAwait(false);
    if (context is null)
    {
      return ApplicationOperationResult<PetDetailDto>.Failed(ApplicationOperationFailure.Forbidden);
    }

    var related = await ResolveRelatedEntitiesAsync(request, context.OrganizationId, cancellationToken)
      .ConfigureAwait(false);
    validation.AddRange(related.Errors);
    await ValidateDocumentAsync(request.VaccinationRecordDocumentId, "vaccinationRecordDocumentId", context.OrganizationId, validation, cancellationToken)
      .ConfigureAwait(false);
    await ValidateDocumentAsync(request.AuthorizationFormDocumentId, "authorizationFormDocumentId", context.OrganizationId, validation, cancellationToken)
      .ConfigureAwait(false);
    if (validation.Count > 0)
    {
      return ApplicationOperationResult<PetDetailDto>.Invalid(validation);
    }

    if (!PetCatalog.TryParseSpecies(request.Species, out var species) ||
      !PetCatalog.TryParseAuthorizationStatus(request.AuthorizationStatus, out var authorizationStatus))
    {
      return ApplicationOperationResult<PetDetailDto>.Invalid(ValidatePetRequest(request).ToList());
    }

    var now = timeProvider.GetUtcNow();
    var pet = Pet.Create(
      EntityId.New(),
      context.OrganizationId,
      related.ResidentId,
      related.PropertyId,
      related.ContractId,
      request.Name,
      species,
      request.Breed,
      authorizationStatus,
      request.AuthorizationNotes,
      request.Notes,
      related.Resident.Name,
      related.Property?.Name,
      related.Contract?.DisplayName,
      now,
      context.UserId);
    LinkSubmittedDocuments(
      pet,
      request.VaccinationRecordDocumentId,
      request.AuthorizationFormDocumentId,
      now,
      context.UserId);

    await petRepository.AddAsync(pet, cancellationToken).ConfigureAwait(false);
    var snapshot = await petRepository.FindSnapshotAsync(pet.Id, context.OrganizationId, true, cancellationToken)
      .ConfigureAwait(false);
    await WriteMutationSideEffectsAsync("pet.created", pet, context, cancellationToken).ConfigureAwait(false);

    return ApplicationOperationResult<PetDetailDto>.Success(ToDetail(snapshot!, locale));
  }

  public async Task<ApplicationOperationResult<PetDetailDto>> UpdateAsync(
    Guid id,
    PetUpdateRequestDto request,
    string? locale = null,
    CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(request);

    var validation = ValidateId(id).Concat(ValidatePetRequest(request)).ToList();
    if (validation.Count > 0)
    {
      return ApplicationOperationResult<PetDetailDto>.Invalid(validation);
    }

    var context = await AuthorizeContextAsync(PermissionCodes.Write(PermissionModules.Pets), cancellationToken)
      .ConfigureAwait(false);
    if (context is null)
    {
      return ApplicationOperationResult<PetDetailDto>.Failed(ApplicationOperationFailure.Forbidden);
    }

    var pet = await petRepository.FindAsync(new EntityId(id), context.OrganizationId, false, cancellationToken)
      .ConfigureAwait(false);
    if (pet is null)
    {
      return ApplicationOperationResult<PetDetailDto>.Failed(ApplicationOperationFailure.NotFound);
    }

    if (!string.IsNullOrWhiteSpace(request.ConcurrencyToken) &&
      !string.Equals(pet.ConcurrencyToken.Value, request.ConcurrencyToken, StringComparison.Ordinal))
    {
      return ApplicationOperationResult<PetDetailDto>.Failed(
        ApplicationOperationFailure.Conflict,
        new Dictionary<string, string[]> { ["concurrencyToken"] = ["validation.concurrency"] });
    }

    var related = await ResolveRelatedEntitiesAsync(request, context.OrganizationId, cancellationToken)
      .ConfigureAwait(false);
    validation.AddRange(related.Errors);
    await ValidateDocumentAsync(request.VaccinationRecordDocumentId, "vaccinationRecordDocumentId", context.OrganizationId, validation, cancellationToken)
      .ConfigureAwait(false);
    await ValidateDocumentAsync(request.AuthorizationFormDocumentId, "authorizationFormDocumentId", context.OrganizationId, validation, cancellationToken)
      .ConfigureAwait(false);
    if (validation.Count > 0)
    {
      return ApplicationOperationResult<PetDetailDto>.Invalid(validation);
    }

    if (!PetCatalog.TryParseSpecies(request.Species, out var species) ||
      !PetCatalog.TryParseAuthorizationStatus(request.AuthorizationStatus, out var authorizationStatus))
    {
      return ApplicationOperationResult<PetDetailDto>.Invalid(ValidatePetRequest(request).ToList());
    }

    try
    {
      var now = timeProvider.GetUtcNow();
      pet.Update(
        related.ResidentId,
        related.PropertyId,
        related.ContractId,
        request.Name,
        species,
        request.Breed,
        authorizationStatus,
        request.AuthorizationNotes,
        request.Notes,
        related.Resident.Name,
        related.Property?.Name,
        related.Contract?.DisplayName,
        now,
        context.UserId);
      LinkSubmittedDocuments(
        pet,
        request.VaccinationRecordDocumentId,
        request.AuthorizationFormDocumentId,
        now,
        context.UserId);
    }
    catch (InvalidOperationException)
    {
      return ApplicationOperationResult<PetDetailDto>.Failed(ApplicationOperationFailure.Conflict);
    }

    await petRepository.UpdateAsync(pet, cancellationToken).ConfigureAwait(false);
    var snapshot = await petRepository.FindSnapshotAsync(pet.Id, context.OrganizationId, true, cancellationToken)
      .ConfigureAwait(false);
    await WriteMutationSideEffectsAsync("pet.updated", pet, context, cancellationToken).ConfigureAwait(false);

    return ApplicationOperationResult<PetDetailDto>.Success(ToDetail(snapshot!, locale));
  }

  public Task<ApplicationOperationResult<PetDetailDto>> AuthorizeAsync(
    Guid id,
    PetLifecycleRequestDto request,
    string? locale = null,
    CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(request);

    return MutateLifecycleAsync(
      id,
      PermissionCodes.Manage(PermissionModules.Pets),
      "pet.authorized",
      locale,
      (pet, context) => pet.Authorize(request.AuthorizationNotes, timeProvider.GetUtcNow(), context.UserId),
      cancellationToken);
  }

  public Task<ApplicationOperationResult<PetDetailDto>> DenyAsync(
    Guid id,
    PetLifecycleRequestDto request,
    string? locale = null,
    CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(request);

    return MutateLifecycleAsync(
      id,
      PermissionCodes.Manage(PermissionModules.Pets),
      "pet.denied",
      locale,
      (pet, context) => pet.Deny(request.AuthorizationNotes, timeProvider.GetUtcNow(), context.UserId),
      cancellationToken);
  }

  public async Task<ApplicationOperationResult> ArchiveAsync(Guid id, CancellationToken cancellationToken = default)
  {
    var validation = ValidateId(id).ToList();
    if (validation.Count > 0)
    {
      return ApplicationOperationResult.Invalid(validation);
    }

    var context = await AuthorizeContextAsync(PermissionCodes.Archive(PermissionModules.Pets), cancellationToken)
      .ConfigureAwait(false);
    if (context is null)
    {
      return ApplicationOperationResult.Failed(ApplicationOperationFailure.Forbidden);
    }

    var pet = await petRepository.FindAsync(new EntityId(id), context.OrganizationId, false, cancellationToken)
      .ConfigureAwait(false);
    if (pet is null)
    {
      return ApplicationOperationResult.Failed(ApplicationOperationFailure.NotFound);
    }

    pet.Archive(timeProvider.GetUtcNow(), context.UserId);
    await petRepository.UpdateAsync(pet, cancellationToken).ConfigureAwait(false);
    await WriteMutationSideEffectsAsync("pet.archived", pet, context, cancellationToken).ConfigureAwait(false);

    return ApplicationOperationResult.Success();
  }

  public Task<ApplicationOperationResult<PetDetailDto>> RestoreAsync(
    Guid id,
    string? locale = null,
    CancellationToken cancellationToken = default) =>
    MutateLifecycleAsync(
      id,
      PermissionCodes.Archive(PermissionModules.Pets),
      "pet.restored",
      locale,
      (pet, context) => pet.Restore(timeProvider.GetUtcNow(), context.UserId),
      cancellationToken,
      includeArchived: true);

  public Task<PetOptionsDto> GetOptionsAsync(string? locale = null, CancellationToken cancellationToken = default)
  {
    cancellationToken.ThrowIfCancellationRequested();
    return Task.FromResult(new PetOptionsDto(
      PetCatalog.GetSpeciesOptions(locale),
      PetCatalog.GetAuthorizationStatusOptions(locale),
      PetCatalog.GetDocumentKindOptions(locale)));
  }

  private async Task<ApplicationOperationResult<PetDetailDto>> MutateLifecycleAsync(
    Guid id,
    string permissionCode,
    string eventName,
    string? locale,
    Action<Pet, ActiveOrganizationContext> mutate,
    CancellationToken cancellationToken,
    bool includeArchived = false)
  {
    var validation = ValidateId(id).ToList();
    if (validation.Count > 0)
    {
      return ApplicationOperationResult<PetDetailDto>.Invalid(validation);
    }

    var context = await AuthorizeContextAsync(permissionCode, cancellationToken).ConfigureAwait(false);
    if (context is null)
    {
      return ApplicationOperationResult<PetDetailDto>.Failed(ApplicationOperationFailure.Forbidden);
    }

    var pet = await petRepository.FindAsync(new EntityId(id), context.OrganizationId, includeArchived, cancellationToken)
      .ConfigureAwait(false);
    if (pet is null)
    {
      return ApplicationOperationResult<PetDetailDto>.Failed(ApplicationOperationFailure.NotFound);
    }

    try
    {
      mutate(pet, context);
    }
    catch (InvalidOperationException)
    {
      return ApplicationOperationResult<PetDetailDto>.Failed(ApplicationOperationFailure.Conflict);
    }

    await petRepository.UpdateAsync(pet, cancellationToken).ConfigureAwait(false);
    var snapshot = await petRepository.FindSnapshotAsync(pet.Id, context.OrganizationId, true, cancellationToken)
      .ConfigureAwait(false);
    await WriteMutationSideEffectsAsync(eventName, pet, context, cancellationToken).ConfigureAwait(false);

    return ApplicationOperationResult<PetDetailDto>.Success(ToDetail(snapshot!, locale));
  }

  private async Task<ResolvedPetEntities> ResolveRelatedEntitiesAsync(
    PetCreateRequestDto request,
    OrganizationId organizationId,
    CancellationToken cancellationToken) =>
    await ResolveRelatedEntitiesAsync(
        request.ResidentId,
        request.PropertyId,
        request.ContractId,
        organizationId,
        cancellationToken)
      .ConfigureAwait(false);

  private async Task<ResolvedPetEntities> ResolveRelatedEntitiesAsync(
    PetUpdateRequestDto request,
    OrganizationId organizationId,
    CancellationToken cancellationToken) =>
    await ResolveRelatedEntitiesAsync(
        request.ResidentId,
        request.PropertyId,
        request.ContractId,
        organizationId,
        cancellationToken)
      .ConfigureAwait(false);

  private async Task<ResolvedPetEntities> ResolveRelatedEntitiesAsync(
    Guid residentIdValue,
    Guid? propertyIdValue,
    Guid? contractIdValue,
    OrganizationId organizationId,
    CancellationToken cancellationToken)
  {
    var errors = new List<ValidationFailure>();
    var residentId = new EntityId(residentIdValue);
    var propertyId = ToEntityIdOrNull(propertyIdValue);
    var contractId = ToEntityIdOrNull(contractIdValue);
    PetResidentSnapshot? resident = null;
    PetPropertySnapshot? property = null;
    PetContractSnapshot? contract = null;

    resident = await petRepository.GetResidentSnapshotAsync(residentId, organizationId, cancellationToken)
      .ConfigureAwait(false);
    if (resident is null)
    {
      errors.Add(new ValidationFailure("residentId", "validation.resident"));
      resident = new PetResidentSnapshot(residentId, residentId.Value.ToString("D"));
    }

    if (contractId.HasValue)
    {
      contract = await petRepository.GetContractSnapshotAsync(contractId.Value, organizationId, cancellationToken)
        .ConfigureAwait(false);
      if (contract is null)
      {
        errors.Add(new ValidationFailure("contractId", "validation.contract"));
      }
      else
      {
        if (!contract.ResidentIds.Contains(residentId))
        {
          errors.Add(new ValidationFailure("residentId", "validation.residentContractMismatch"));
        }

        if (propertyId.HasValue && propertyId.Value != contract.PropertyId)
        {
          errors.Add(new ValidationFailure("propertyId", "validation.propertyContractMismatch"));
        }

        propertyId ??= contract.PropertyId;
      }
    }

    if (propertyId.HasValue)
    {
      property = await petRepository.GetPropertySnapshotAsync(propertyId.Value, organizationId, cancellationToken)
        .ConfigureAwait(false);
      if (property is null)
      {
        errors.Add(new ValidationFailure("propertyId", "validation.property"));
      }
    }

    return new ResolvedPetEntities(errors, residentId, propertyId, contractId, resident, property, contract);
  }

  private async Task ValidateDocumentAsync(
    Guid? documentId,
    string propertyName,
    OrganizationId organizationId,
    List<ValidationFailure> validation,
    CancellationToken cancellationToken)
  {
    var id = ToEntityIdOrNull(documentId);
    if (!id.HasValue)
    {
      return;
    }

    if (!await petRepository.DocumentExistsAsync(id.Value, organizationId, cancellationToken).ConfigureAwait(false))
    {
      validation.Add(new ValidationFailure(propertyName, "validation.document"));
    }
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
    Pet pet,
    ActiveOrganizationContext context,
    CancellationToken cancellationToken)
  {
    var subject = EntityReference.FromGuid("pet", pet.Id.Value, pet.Name);
    var actor = EventActor.User(context.UserId, context.User.DisplayName);
    var data = new Dictionary<string, string>(StringComparer.Ordinal)
    {
      ["status"] = PetCatalog.ToAuthorizationStatusLabel(pet.EffectiveStatus).Code,
      ["species"] = PetCatalog.ToSpeciesLabel(pet.Species).Code,
      ["residentId"] = pet.ResidentId.Value.ToString("D")
    };

    if (!string.IsNullOrWhiteSpace(pet.Breed))
    {
      data["breed"] = pet.Breed!;
    }

    var related = new List<EntityReference>
    {
      EntityReference.FromGuid("resident", pet.ResidentId.Value)
    };
    if (pet.PropertyId.HasValue)
    {
      related.Add(EntityReference.FromGuid("property", pet.PropertyId.Value.Value));
    }

    if (pet.ContractId.HasValue)
    {
      related.Add(EntityReference.FromGuid("contract", pet.ContractId.Value.Value));
    }

    related.AddRange(pet.DocumentLinks
      .Where(link => link.DeletedAt is null)
      .Select(link => EntityReference.FromGuid("document", link.DocumentId.Value, link.Label)));

    var envelope = ModuleEventEnvelope.Create(
      context.OrganizationId,
      "pets",
      eventName,
      timeProvider.GetUtcNow(),
      actor,
      subject,
      ModuleEventConsumer.Audit | ModuleEventConsumer.Timeline | ModuleEventConsumer.Notifications,
      data,
      related);

    await auditWriter.WriteAsync(AuditEntryDraft.FromModuleEvent(envelope), cancellationToken)
      .ConfigureAwait(false);
    await outboxWriter.EnqueueAsync(envelope, cancellationToken).ConfigureAwait(false);
  }

  private static PetListItemDto ToListItem(PetSnapshot snapshot, string? locale)
  {
    var pet = snapshot.Pet;
    var id = Uri.EscapeDataString(pet.Id.Value.ToString("D"));
    var vaccinationDocuments = snapshot.Documents
      .Where(document => document.Kind == PetDocumentKind.VaccinationRecord)
      .Select(document => ToDocumentDto(document, locale, id))
      .ToArray();
    var authorizationDocuments = snapshot.Documents
      .Where(document => document.Kind == PetDocumentKind.AuthorizationForm)
      .Select(document => ToDocumentDto(document, locale, id))
      .ToArray();

    return new PetListItemDto(
      pet.Id.Value,
      pet.Name,
      PetCatalog.ToSpeciesLabel(pet.Species, locale),
      pet.Breed,
      PetCatalog.ToAuthorizationStatusLabel(pet.EffectiveStatus, locale),
      pet.AuthorizationNotes,
      ToResidentSummary(snapshot.Resident),
      ToPropertySummary(snapshot.Property),
      ToContractSummary(snapshot.Contract),
      vaccinationDocuments,
      authorizationDocuments,
      pet.IsDeleted || pet.EffectiveStatus == PetAuthorizationStatus.Archived,
      pet.CreatedAt,
      pet.UpdatedAt,
      pet.ConcurrencyToken.Value);
  }

  private static PetDetailDto ToDetail(PetSnapshot snapshot, string? locale)
  {
    var listItem = ToListItem(snapshot, locale);
    var pet = snapshot.Pet;
    var id = Uri.EscapeDataString(pet.Id.Value.ToString("D"));
    var status = PetCatalog.ToAuthorizationStatusLabel(pet.EffectiveStatus, locale);

    return new PetDetailDto(
      listItem.Id,
      listItem.Name,
      listItem.Species,
      listItem.Breed,
      listItem.AuthorizationStatus,
      listItem.AuthorizationNotes,
      pet.Notes,
      listItem.Resident,
      listItem.Property,
      listItem.Contract,
      listItem.VaccinationRecordDocuments,
      listItem.AuthorizationFormDocuments,
      [
        new PetAuthorizationHistoryItemDto(
          status.Code,
          status.Label,
          pet.AuthorizationNotes,
          pet.UpdatedAt ?? pet.CreatedAt)
      ],
      $"/timeline?entityType=pet&entityId={id}",
      $"/auditoria?entityType=pet&entityId={id}",
      pet.CreatedAt,
      pet.UpdatedAt,
      pet.DeletedAt,
      pet.ConcurrencyToken.Value);
  }

  private static PetDocumentDto ToDocumentDto(PetDocumentSnapshot document, string? locale, string id)
  {
    var label = PetCatalog.ToDocumentKindLabel(document.Kind, locale);
    return new PetDocumentDto(
      document.DocumentId.Value,
      label.Code,
      label.Label,
      document.Label,
      $"/documentos?entityType=pet&entityId={id}");
  }

  private static PetEntitySummaryDto ToResidentSummary(PetResidentSnapshot resident) =>
    new(resident.ResidentId.Value, resident.Name, Route: $"/moradores?id={resident.ResidentId.Value:D}");

  private static PetEntitySummaryDto? ToPropertySummary(PetPropertySnapshot? property) =>
    property is null
      ? null
      : new PetEntitySummaryDto(
        property.PropertyId.Value,
        property.Name,
        property.Location,
        $"/imoveis?id={property.PropertyId.Value:D}");

  private static PetEntitySummaryDto? ToContractSummary(PetContractSnapshot? contract) =>
    contract is null
      ? null
      : new PetEntitySummaryDto(
        contract.ContractId.Value,
        contract.DisplayName,
        $"{contract.PropertyName} - {contract.ResidentName}",
        $"/contratos?id={contract.ContractId.Value:D}");

  private static void LinkSubmittedDocuments(
    Pet pet,
    Guid? vaccinationRecordDocumentId,
    Guid? authorizationFormDocumentId,
    DateTimeOffset now,
    UserId? userId)
  {
    var vaccinationRecordId = ToEntityIdOrNull(vaccinationRecordDocumentId);
    if (vaccinationRecordId.HasValue)
    {
      pet.LinkDocument(
        vaccinationRecordId.Value,
        PetDocumentKind.VaccinationRecord,
        "Carteira de vacinacao",
        now,
        userId);
    }

    var authorizationFormId = ToEntityIdOrNull(authorizationFormDocumentId);
    if (authorizationFormId.HasValue)
    {
      pet.LinkDocument(
        authorizationFormId.Value,
        PetDocumentKind.AuthorizationForm,
        "Formulario de autorizacao",
        now,
        userId);
    }
  }

  private static IEnumerable<ValidationFailure> ValidatePetRequest(PetCreateRequestDto request) =>
    ValidatePetFields(
      request.ResidentId,
      request.Name,
      request.Species,
      request.AuthorizationStatus);

  private static IEnumerable<ValidationFailure> ValidatePetRequest(PetUpdateRequestDto request) =>
    ValidatePetFields(
      request.ResidentId,
      request.Name,
      request.Species,
      request.AuthorizationStatus);

  private static IEnumerable<ValidationFailure> ValidatePetFields(
    Guid residentId,
    string name,
    string species,
    string authorizationStatus)
  {
    if (residentId == Guid.Empty)
    {
      yield return new ValidationFailure(nameof(residentId), ValidationMessageKeys.Required);
    }

    if (string.IsNullOrWhiteSpace(name))
    {
      yield return new ValidationFailure(nameof(name), ValidationMessageKeys.Required);
    }

    if (!PetCatalog.TryParseSpecies(species, out _))
    {
      yield return new ValidationFailure(nameof(species), "validation.petSpecies");
    }

    if (!PetCatalog.TryParseAuthorizationStatus(authorizationStatus, out var parsedStatus) ||
      parsedStatus == PetAuthorizationStatus.Archived)
    {
      yield return new ValidationFailure(nameof(authorizationStatus), "validation.petAuthorizationStatus");
    }
  }

  private static IEnumerable<ValidationFailure> ValidateId(Guid id, string propertyName = "id")
  {
    if (id == Guid.Empty)
    {
      yield return new ValidationFailure(propertyName, ValidationMessageKeys.Required);
    }
  }

  private static EntityId? ToEntityIdOrNull(Guid? id) =>
    id.HasValue && id.Value != Guid.Empty ? new EntityId(id.Value) : null;

  private sealed record ResolvedPetEntities(
    IReadOnlyList<ValidationFailure> Errors,
    EntityId ResidentId,
    EntityId? PropertyId,
    EntityId? ContractId,
    PetResidentSnapshot Resident,
    PetPropertySnapshot? Property,
    PetContractSnapshot? Contract);
}
