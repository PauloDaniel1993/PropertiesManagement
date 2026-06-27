using Alsappan.Application.Common.Audit;
using Alsappan.Application.Common.Authorization;
using Alsappan.Application.Common.Contracts;
using Alsappan.Application.Common.Events;
using Alsappan.Application.Common.Results;
using Alsappan.Application.Common.Tenancy;
using Alsappan.Application.Common.Validation;
using Alsappan.Application.Vehicles.Repositories;
using Alsappan.Domain.Common.Events;
using Alsappan.Domain.Common.Identifiers;
using Alsappan.Domain.Vehicles;

namespace Alsappan.Application.Vehicles;

public sealed class VehicleService : IVehicleService
{
  private static readonly char[] ParkingIdentifierSeparators = [',', ';', '|', '\r', '\n'];

  private readonly IVehicleRepository vehicleRepository;
  private readonly IPermissionService permissionService;
  private readonly IActiveOrganizationContextResolver activeOrganizationContextResolver;
  private readonly IAuditWriter auditWriter;
  private readonly IModuleEventOutboxWriter outboxWriter;
  private readonly TimeProvider timeProvider;

  public VehicleService(
    IVehicleRepository vehicleRepository,
    IPermissionService permissionService,
    IActiveOrganizationContextResolver activeOrganizationContextResolver,
    IAuditWriter auditWriter,
    IModuleEventOutboxWriter outboxWriter,
    TimeProvider timeProvider)
  {
    this.vehicleRepository = vehicleRepository ?? throw new ArgumentNullException(nameof(vehicleRepository));
    this.permissionService = permissionService ?? throw new ArgumentNullException(nameof(permissionService));
    this.activeOrganizationContextResolver = activeOrganizationContextResolver ??
      throw new ArgumentNullException(nameof(activeOrganizationContextResolver));
    this.auditWriter = auditWriter ?? throw new ArgumentNullException(nameof(auditWriter));
    this.outboxWriter = outboxWriter ?? throw new ArgumentNullException(nameof(outboxWriter));
    this.timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
  }

  public async Task<ApplicationOperationResult<PagedResultDto<VehicleListItemDto>>> ListAsync(
    VehicleListRequestDto request,
    CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(request);

    var context = await AuthorizeContextAsync(PermissionCodes.Read(PermissionModules.Vehicles), cancellationToken)
      .ConfigureAwait(false);
    if (context is null)
    {
      return ApplicationOperationResult<PagedResultDto<VehicleListItemDto>>.Failed(
        ApplicationOperationFailure.Forbidden);
    }

    var page = await vehicleRepository.ListAsync(request, context.OrganizationId, cancellationToken)
      .ConfigureAwait(false);
    var items = page.Items.Select(snapshot => ToListItem(snapshot, request.Locale)).ToArray();

    return ApplicationOperationResult<PagedResultDto<VehicleListItemDto>>.Success(
      new PagedResultDto<VehicleListItemDto>(items, page.Page, page.PageSize, page.TotalItems));
  }

  public async Task<ApplicationOperationResult<VehicleDetailDto>> GetAsync(
    Guid id,
    string? locale = null,
    CancellationToken cancellationToken = default)
  {
    var validation = ValidateId(id).ToList();
    if (validation.Count > 0)
    {
      return ApplicationOperationResult<VehicleDetailDto>.Invalid(validation);
    }

    var context = await AuthorizeContextAsync(PermissionCodes.Read(PermissionModules.Vehicles), cancellationToken)
      .ConfigureAwait(false);
    if (context is null)
    {
      return ApplicationOperationResult<VehicleDetailDto>.Failed(ApplicationOperationFailure.Forbidden);
    }

    var snapshot = await vehicleRepository.FindSnapshotAsync(
        new EntityId(id),
        context.OrganizationId,
        includeArchived: true,
        cancellationToken)
      .ConfigureAwait(false);

    return snapshot is null
      ? ApplicationOperationResult<VehicleDetailDto>.Failed(ApplicationOperationFailure.NotFound)
      : ApplicationOperationResult<VehicleDetailDto>.Success(ToDetail(snapshot, locale));
  }

  public async Task<ApplicationOperationResult<VehicleDetailDto>> CreateAsync(
    VehicleCreateRequestDto request,
    string? locale = null,
    CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(request);

    var validation = ValidateVehicleRequest(request).ToList();
    if (validation.Count > 0)
    {
      return ApplicationOperationResult<VehicleDetailDto>.Invalid(validation);
    }

    var context = await AuthorizeContextAsync(PermissionCodes.Write(PermissionModules.Vehicles), cancellationToken)
      .ConfigureAwait(false);
    if (context is null)
    {
      return ApplicationOperationResult<VehicleDetailDto>.Failed(ApplicationOperationFailure.Forbidden);
    }

    var related = await ResolveRelatedEntitiesAsync(request, context.OrganizationId, cancellationToken)
      .ConfigureAwait(false);
    validation.AddRange(related.Errors);

    if (!VehicleCatalog.TryParseType(request.Type, out var type) ||
      !VehicleCatalog.TryParseAuthorizationStatus(request.AuthorizationStatus, out var status))
    {
      return ApplicationOperationResult<VehicleDetailDto>.Invalid(ValidateVehicleRequest(request).ToList());
    }

    if (status != VehicleAuthorizationStatus.Pending &&
      !await HasPermissionAsync(PermissionCodes.Manage(PermissionModules.Vehicles), cancellationToken)
        .ConfigureAwait(false))
    {
      return ApplicationOperationResult<VehicleDetailDto>.Failed(ApplicationOperationFailure.Forbidden);
    }

    validation.AddRange(await ValidateParkingAllocationAsync(
        related.Property,
        context.OrganizationId,
        status,
        request.ParkingSpaceIdentifier,
        ignoredVehicleId: null,
        cancellationToken)
      .ConfigureAwait(false));

    if (validation.Count > 0)
    {
      return ApplicationOperationResult<VehicleDetailDto>.Invalid(validation);
    }

    var now = timeProvider.GetUtcNow();
    var vehicle = Vehicle.Create(
      EntityId.New(),
      context.OrganizationId,
      related.ResidentId!.Value,
      related.PropertyId,
      related.ContractId,
      request.Plate,
      type,
      request.Color,
      request.Brand,
      request.Model,
      request.Year,
      status,
      request.ParkingSpaceIdentifier,
      request.ParkingAllocationNotes,
      request.Notes,
      related.Resident?.Name,
      related.Property?.Name,
      related.Contract?.DisplayName,
      now,
      context.UserId);

    await vehicleRepository.AddAsync(vehicle, cancellationToken).ConfigureAwait(false);
    var snapshot = await vehicleRepository.FindSnapshotAsync(
        vehicle.Id,
        context.OrganizationId,
        includeArchived: true,
        cancellationToken)
      .ConfigureAwait(false);
    await WriteMutationSideEffectsAsync("vehicle.created", vehicle, context, cancellationToken)
      .ConfigureAwait(false);

    return ApplicationOperationResult<VehicleDetailDto>.Success(ToDetail(snapshot!, locale));
  }

  public async Task<ApplicationOperationResult<VehicleDetailDto>> UpdateAsync(
    Guid id,
    VehicleUpdateRequestDto request,
    string? locale = null,
    CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(request);

    var validation = ValidateId(id).Concat(ValidateVehicleRequest(request)).ToList();
    if (validation.Count > 0)
    {
      return ApplicationOperationResult<VehicleDetailDto>.Invalid(validation);
    }

    var context = await AuthorizeContextAsync(PermissionCodes.Write(PermissionModules.Vehicles), cancellationToken)
      .ConfigureAwait(false);
    if (context is null)
    {
      return ApplicationOperationResult<VehicleDetailDto>.Failed(ApplicationOperationFailure.Forbidden);
    }

    var vehicle = await vehicleRepository.FindAsync(new EntityId(id), context.OrganizationId, false, cancellationToken)
      .ConfigureAwait(false);
    if (vehicle is null)
    {
      return ApplicationOperationResult<VehicleDetailDto>.Failed(ApplicationOperationFailure.NotFound);
    }

    if (!string.IsNullOrWhiteSpace(request.ConcurrencyToken) &&
      !string.Equals(vehicle.ConcurrencyToken.Value, request.ConcurrencyToken, StringComparison.Ordinal))
    {
      return ApplicationOperationResult<VehicleDetailDto>.Failed(ApplicationOperationFailure.Conflict);
    }

    var related = await ResolveRelatedEntitiesAsync(request, context.OrganizationId, cancellationToken)
      .ConfigureAwait(false);
    validation.AddRange(related.Errors);

    if (!VehicleCatalog.TryParseType(request.Type, out var type) ||
      !VehicleCatalog.TryParseAuthorizationStatus(request.AuthorizationStatus, out var status))
    {
      return ApplicationOperationResult<VehicleDetailDto>.Invalid(ValidateVehicleRequest(request).ToList());
    }

    if (status != vehicle.AuthorizationStatus &&
      !await HasPermissionAsync(PermissionCodes.Manage(PermissionModules.Vehicles), cancellationToken)
        .ConfigureAwait(false))
    {
      return ApplicationOperationResult<VehicleDetailDto>.Failed(ApplicationOperationFailure.Forbidden);
    }

    validation.AddRange(await ValidateParkingAllocationAsync(
        related.Property,
        context.OrganizationId,
        status,
        request.ParkingSpaceIdentifier,
        vehicle.Id,
        cancellationToken)
      .ConfigureAwait(false));

    if (validation.Count > 0)
    {
      return ApplicationOperationResult<VehicleDetailDto>.Invalid(validation);
    }

    var beforePropertyId = vehicle.PropertyId;
    var beforeParkingIdentifier = vehicle.NormalizedParkingSpaceIdentifier;
    var beforeParkingNotes = vehicle.ParkingAllocationNotes;

    try
    {
      vehicle.Update(
        related.ResidentId!.Value,
        related.PropertyId,
        related.ContractId,
        request.Plate,
        type,
        request.Color,
        request.Brand,
        request.Model,
        request.Year,
        status,
        request.ParkingSpaceIdentifier,
        request.ParkingAllocationNotes,
        request.Notes,
        related.Resident?.Name,
        related.Property?.Name,
        related.Contract?.DisplayName,
        timeProvider.GetUtcNow(),
        context.UserId);
    }
    catch (ArgumentException)
    {
      return ApplicationOperationResult<VehicleDetailDto>.Failed(ApplicationOperationFailure.Conflict);
    }
    catch (InvalidOperationException)
    {
      return ApplicationOperationResult<VehicleDetailDto>.Failed(ApplicationOperationFailure.Conflict);
    }

    await vehicleRepository.UpdateAsync(vehicle, cancellationToken).ConfigureAwait(false);
    var snapshot = await vehicleRepository.FindSnapshotAsync(vehicle.Id, context.OrganizationId, true, cancellationToken)
      .ConfigureAwait(false);
    var parkingChanged = beforePropertyId != vehicle.PropertyId ||
      !string.Equals(beforeParkingIdentifier, vehicle.NormalizedParkingSpaceIdentifier, StringComparison.Ordinal) ||
      !string.Equals(beforeParkingNotes, vehicle.ParkingAllocationNotes, StringComparison.Ordinal);
    await WriteMutationSideEffectsAsync(
        parkingChanged ? "vehicle.parking-updated" : "vehicle.updated",
        vehicle,
        context,
        cancellationToken)
      .ConfigureAwait(false);

    return ApplicationOperationResult<VehicleDetailDto>.Success(ToDetail(snapshot!, locale));
  }

  public async Task<ApplicationOperationResult<VehicleDetailDto>> AuthorizeAsync(
    Guid id,
    string? locale = null,
    CancellationToken cancellationToken = default) =>
    await MutateLifecycleAsync(
        id,
        PermissionCodes.Manage(PermissionModules.Vehicles),
        "vehicle.authorized",
        locale,
        async (vehicle, context, token) =>
        {
          if (vehicle.HasParkingAllocation)
          {
            var property = vehicle.PropertyId.HasValue
              ? await vehicleRepository.GetPropertySnapshotAsync(vehicle.PropertyId.Value, context.OrganizationId, token)
                .ConfigureAwait(false)
              : null;
            var validation = await ValidateParkingAllocationAsync(
                property,
                context.OrganizationId,
                VehicleAuthorizationStatus.Authorized,
                vehicle.ParkingSpaceIdentifier,
                vehicle.Id,
                token)
              .ConfigureAwait(false);
            if (validation.Count > 0)
            {
              return ApplicationOperationResult<VehicleDetailDto>.Invalid(validation);
            }
          }

          vehicle.Authorize(timeProvider.GetUtcNow(), context.UserId);
          return null;
        },
        cancellationToken)
      .ConfigureAwait(false);

  public async Task<ApplicationOperationResult<VehicleDetailDto>> DenyAsync(
    Guid id,
    VehicleLifecycleRequestDto request,
    string? locale = null,
    CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(request);

    return await MutateLifecycleAsync(
        id,
        PermissionCodes.Manage(PermissionModules.Vehicles),
        "vehicle.denied",
        locale,
        (vehicle, context, token) =>
        {
          token.ThrowIfCancellationRequested();
          vehicle.Deny(request.Notes, timeProvider.GetUtcNow(), context.UserId);
          return Task.FromResult<ApplicationOperationResult<VehicleDetailDto>?>(null);
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

    var context = await AuthorizeContextAsync(PermissionCodes.Archive(PermissionModules.Vehicles), cancellationToken)
      .ConfigureAwait(false);
    if (context is null)
    {
      return ApplicationOperationResult.Failed(ApplicationOperationFailure.Forbidden);
    }

    var vehicle = await vehicleRepository.FindAsync(new EntityId(id), context.OrganizationId, false, cancellationToken)
      .ConfigureAwait(false);
    if (vehicle is null)
    {
      return ApplicationOperationResult.Failed(ApplicationOperationFailure.NotFound);
    }

    vehicle.Archive(timeProvider.GetUtcNow(), context.UserId);
    await vehicleRepository.UpdateAsync(vehicle, cancellationToken).ConfigureAwait(false);
    await WriteMutationSideEffectsAsync("vehicle.archived", vehicle, context, cancellationToken)
      .ConfigureAwait(false);

    return ApplicationOperationResult.Success();
  }

  public async Task<ApplicationOperationResult<VehicleDetailDto>> RestoreAsync(
    Guid id,
    string? locale = null,
    CancellationToken cancellationToken = default) =>
    await MutateLifecycleAsync(
        id,
        PermissionCodes.Archive(PermissionModules.Vehicles),
        "vehicle.restored",
        locale,
        (vehicle, context, token) =>
        {
          token.ThrowIfCancellationRequested();
          vehicle.Restore(timeProvider.GetUtcNow(), context.UserId);
          return Task.FromResult<ApplicationOperationResult<VehicleDetailDto>?>(null);
        },
        cancellationToken,
        includeArchived: true)
      .ConfigureAwait(false);

  public Task<IReadOnlyList<StatusLabelDto>> GetTypeOptionsAsync(
    string? locale = null,
    CancellationToken cancellationToken = default)
  {
    cancellationToken.ThrowIfCancellationRequested();
    return Task.FromResult(VehicleCatalog.GetTypeOptions(locale));
  }

  public Task<IReadOnlyList<StatusLabelDto>> GetAuthorizationStatusOptionsAsync(
    string? locale = null,
    CancellationToken cancellationToken = default)
  {
    cancellationToken.ThrowIfCancellationRequested();
    return Task.FromResult(VehicleCatalog.GetAuthorizationStatusOptions(locale));
  }

  private async Task<ApplicationOperationResult<VehicleDetailDto>> MutateLifecycleAsync(
    Guid id,
    string permissionCode,
    string eventName,
    string? locale,
    Func<Vehicle, ActiveOrganizationContext, CancellationToken, Task<ApplicationOperationResult<VehicleDetailDto>?>> mutate,
    CancellationToken cancellationToken,
    bool includeArchived = false)
  {
    var validation = ValidateId(id).ToList();
    if (validation.Count > 0)
    {
      return ApplicationOperationResult<VehicleDetailDto>.Invalid(validation);
    }

    var context = await AuthorizeContextAsync(permissionCode, cancellationToken).ConfigureAwait(false);
    if (context is null)
    {
      return ApplicationOperationResult<VehicleDetailDto>.Failed(ApplicationOperationFailure.Forbidden);
    }

    var vehicle = await vehicleRepository.FindAsync(new EntityId(id), context.OrganizationId, includeArchived, cancellationToken)
      .ConfigureAwait(false);
    if (vehicle is null)
    {
      return ApplicationOperationResult<VehicleDetailDto>.Failed(ApplicationOperationFailure.NotFound);
    }

    try
    {
      var mutationResult = await mutate(vehicle, context, cancellationToken).ConfigureAwait(false);
      if (mutationResult is not null)
      {
        return mutationResult;
      }
    }
    catch (InvalidOperationException)
    {
      return ApplicationOperationResult<VehicleDetailDto>.Failed(ApplicationOperationFailure.Conflict);
    }

    await vehicleRepository.UpdateAsync(vehicle, cancellationToken).ConfigureAwait(false);
    var snapshot = await vehicleRepository.FindSnapshotAsync(vehicle.Id, context.OrganizationId, true, cancellationToken)
      .ConfigureAwait(false);
    await WriteMutationSideEffectsAsync(eventName, vehicle, context, cancellationToken).ConfigureAwait(false);

    return ApplicationOperationResult<VehicleDetailDto>.Success(ToDetail(snapshot!, locale));
  }

  private async Task<ResolvedVehicleEntities> ResolveRelatedEntitiesAsync(
    VehicleCreateRequestDto request,
    OrganizationId organizationId,
    CancellationToken cancellationToken) =>
    await ResolveRelatedEntitiesAsync(
        request.ContractId,
        request.PropertyId,
        request.ResidentId,
        organizationId,
        cancellationToken)
      .ConfigureAwait(false);

  private async Task<ResolvedVehicleEntities> ResolveRelatedEntitiesAsync(
    VehicleUpdateRequestDto request,
    OrganizationId organizationId,
    CancellationToken cancellationToken) =>
    await ResolveRelatedEntitiesAsync(
        request.ContractId,
        request.PropertyId,
        request.ResidentId,
        organizationId,
        cancellationToken)
      .ConfigureAwait(false);

  private async Task<ResolvedVehicleEntities> ResolveRelatedEntitiesAsync(
    Guid? contractIdValue,
    Guid? propertyIdValue,
    Guid? residentIdValue,
    OrganizationId organizationId,
    CancellationToken cancellationToken)
  {
    var errors = new List<ValidationFailure>();
    VehicleContractSnapshot? contract = null;
    VehiclePropertySnapshot? property = null;
    VehicleResidentSnapshot? resident = null;
    var contractId = ToEntityIdOrNull(contractIdValue);
    var propertyId = ToEntityIdOrNull(propertyIdValue);
    var residentId = ToEntityIdOrNull(residentIdValue);

    if (contractId.HasValue)
    {
      contract = await vehicleRepository.GetContractSnapshotAsync(contractId.Value, organizationId, cancellationToken)
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
      property = await vehicleRepository.GetPropertySnapshotAsync(propertyId.Value, organizationId, cancellationToken)
        .ConfigureAwait(false);
      if (property is null)
      {
        errors.Add(new ValidationFailure("propertyId", "validation.property"));
      }
    }

    if (residentId.HasValue)
    {
      resident = await vehicleRepository.GetResidentSnapshotAsync(residentId.Value, organizationId, cancellationToken)
        .ConfigureAwait(false);
      if (resident is null)
      {
        errors.Add(new ValidationFailure("residentId", "validation.resident"));
      }
    }

    return new ResolvedVehicleEntities(errors, contractId, propertyId, residentId, property, contract, resident);
  }

  private async Task<IReadOnlyList<ValidationFailure>> ValidateParkingAllocationAsync(
    VehiclePropertySnapshot? property,
    OrganizationId organizationId,
    VehicleAuthorizationStatus targetStatus,
    string? parkingSpaceIdentifier,
    EntityId? ignoredVehicleId,
    CancellationToken cancellationToken)
  {
    var normalizedParking = VehicleCode.NormalizeIdentifier(parkingSpaceIdentifier);
    if (normalizedParking is null)
    {
      return [];
    }

    var validation = new List<ValidationFailure>();
    if (property is null)
    {
      validation.Add(new ValidationFailure(nameof(parkingSpaceIdentifier), "validation.property"));
      return validation;
    }

    if (property.GarageSpaceCount <= 0)
    {
      validation.Add(new ValidationFailure(nameof(parkingSpaceIdentifier), "validation.parkingGarage"));
    }

    var configuredIdentifiers = NormalizeConfiguredParkingIdentifiers(property.GarageSpaceIdentifiers);
    if (configuredIdentifiers.Count > 0 && !configuredIdentifiers.Contains(normalizedParking))
    {
      validation.Add(new ValidationFailure(nameof(parkingSpaceIdentifier), "validation.parkingSpaceIdentifier"));
    }

    if (validation.Count == 0 &&
      IsActiveParkingStatus(targetStatus) &&
      await vehicleRepository.HasActiveParkingAllocationAsync(
          organizationId,
          property.PropertyId,
          normalizedParking,
          ignoredVehicleId,
          cancellationToken)
        .ConfigureAwait(false))
    {
      validation.Add(new ValidationFailure(nameof(parkingSpaceIdentifier), "validation.parkingSpaceDuplicate"));
    }

    return validation;
  }

  private async Task<bool> HasPermissionAsync(string permissionCode, CancellationToken cancellationToken)
  {
    var permission = await permissionService.AuthorizeAsync(permissionCode, cancellationToken)
      .ConfigureAwait(false);
    return permission.IsGranted;
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
    Vehicle vehicle,
    ActiveOrganizationContext context,
    CancellationToken cancellationToken)
  {
    var subject = EntityReference.FromGuid("vehicle", vehicle.Id.Value, vehicle.Plate);
    var actor = EventActor.User(context.UserId, context.User.DisplayName);
    var data = new Dictionary<string, string>(StringComparer.Ordinal)
    {
      ["plate"] = vehicle.Plate,
      ["normalizedPlate"] = vehicle.NormalizedPlate,
      ["type"] = VehicleCatalog.ToTypeLabel(vehicle.Type).Code,
      ["authorizationStatus"] = VehicleCatalog.ToAuthorizationStatusLabel(vehicle.AuthorizationStatus).Code
    };

    if (!string.IsNullOrWhiteSpace(vehicle.ParkingSpaceIdentifier))
    {
      data["parkingSpaceIdentifier"] = vehicle.ParkingSpaceIdentifier;
    }

    var related = new List<EntityReference>
    {
      EntityReference.FromGuid("resident", vehicle.ResidentId.Value)
    };
    if (vehicle.PropertyId.HasValue)
    {
      related.Add(EntityReference.FromGuid("property", vehicle.PropertyId.Value.Value));
    }

    if (vehicle.ContractId.HasValue)
    {
      related.Add(EntityReference.FromGuid("contract", vehicle.ContractId.Value.Value));
    }

    var envelope = ModuleEventEnvelope.Create(
      context.OrganizationId,
      "vehicles",
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

  private static VehicleListItemDto ToListItem(VehicleSnapshot snapshot, string? locale)
  {
    var vehicle = snapshot.Vehicle;
    var effectiveStatus = vehicle.IsDeleted
      ? VehicleAuthorizationStatus.Archived
      : vehicle.AuthorizationStatus;

    return new VehicleListItemDto(
      vehicle.Id.Value,
      ToResidentSummary(snapshot.Resident),
      ToPropertySummary(snapshot.Property),
      ToContractSummary(snapshot.Contract),
      vehicle.Plate,
      vehicle.NormalizedPlate,
      VehicleCatalog.ToTypeLabel(vehicle.Type, locale),
      vehicle.Color,
      vehicle.Brand,
      vehicle.Model,
      vehicle.Year,
      VehicleCatalog.ToAuthorizationStatusLabel(effectiveStatus, locale),
      vehicle.ParkingSpaceIdentifier,
      vehicle.NormalizedParkingSpaceIdentifier,
      vehicle.ParkingAllocationNotes,
      vehicle.HasParkingAllocation,
      vehicle.IsDeleted || effectiveStatus == VehicleAuthorizationStatus.Archived,
      vehicle.CreatedAt,
      vehicle.UpdatedAt,
      vehicle.ConcurrencyToken.Value);
  }

  private static VehicleDetailDto ToDetail(VehicleSnapshot snapshot, string? locale)
  {
    var listItem = ToListItem(snapshot, locale);
    var vehicle = snapshot.Vehicle;
    var id = Uri.EscapeDataString(vehicle.Id.Value.ToString("D"));

    return new VehicleDetailDto(
      listItem.Id,
      listItem.Resident,
      listItem.Property,
      listItem.Contract,
      listItem.Plate,
      listItem.NormalizedPlate,
      listItem.Type,
      listItem.Color,
      listItem.Brand,
      listItem.Model,
      listItem.Year,
      listItem.AuthorizationStatus,
      listItem.ParkingSpaceIdentifier,
      listItem.NormalizedParkingSpaceIdentifier,
      listItem.ParkingAllocationNotes,
      listItem.HasParkingAllocation,
      vehicle.Notes,
      $"/timeline?entityType=vehicle&entityId={id}",
      $"/auditoria?entityType=vehicle&entityId={id}",
      vehicle.CreatedAt,
      vehicle.UpdatedAt,
      vehicle.DeletedAt,
      vehicle.ConcurrencyToken.Value);
  }

  private static VehicleEntitySummaryDto ToResidentSummary(VehicleResidentSnapshot resident) =>
    new(resident.ResidentId.Value, resident.Name, Route: $"/moradores?id={resident.ResidentId.Value:D}");

  private static VehicleEntitySummaryDto? ToPropertySummary(VehiclePropertySnapshot? property) =>
    property is null
      ? null
      : new VehicleEntitySummaryDto(
        property.PropertyId.Value,
        property.Name,
        property.Location,
        $"/imoveis?id={property.PropertyId.Value:D}");

  private static VehicleEntitySummaryDto? ToContractSummary(VehicleContractSnapshot? contract) =>
    contract is null
      ? null
      : new VehicleEntitySummaryDto(
        contract.ContractId.Value,
        contract.DisplayName,
        $"{contract.PropertyName} - {contract.ResidentName}",
        $"/contratos?id={contract.ContractId.Value:D}");

  private static IEnumerable<ValidationFailure> ValidateVehicleRequest(VehicleCreateRequestDto request) =>
    ValidateVehicleFields(
      request.Plate,
      request.Type,
      request.AuthorizationStatus,
      request.ResidentId,
      request.ContractId,
      request.Year,
      request.ParkingSpaceIdentifier);

  private static IEnumerable<ValidationFailure> ValidateVehicleRequest(VehicleUpdateRequestDto request) =>
    ValidateVehicleFields(
      request.Plate,
      request.Type,
      request.AuthorizationStatus,
      request.ResidentId,
      request.ContractId,
      request.Year,
      request.ParkingSpaceIdentifier);

  private static IEnumerable<ValidationFailure> ValidateVehicleFields(
    string plate,
    string type,
    string authorizationStatus,
    Guid? residentId,
    Guid? contractId,
    int? year,
    string? parkingSpaceIdentifier)
  {
    if (string.IsNullOrWhiteSpace(plate) || VehicleCode.NormalizePlate(plate) is null)
    {
      yield return new ValidationFailure(nameof(plate), ValidationMessageKeys.Required);
    }

    if (!VehicleCatalog.TryParseType(type, out _))
    {
      yield return new ValidationFailure(nameof(type), "validation.vehicleType");
    }

    if (!VehicleCatalog.TryParseAuthorizationStatus(authorizationStatus, out var parsedStatus) ||
      parsedStatus == VehicleAuthorizationStatus.Archived)
    {
      yield return new ValidationFailure(nameof(authorizationStatus), "validation.vehicleAuthorizationStatus");
    }

    if (!HasNonEmptyId(residentId) && !HasNonEmptyId(contractId))
    {
      yield return new ValidationFailure(nameof(residentId), "validation.resident");
    }

    if (year.HasValue && year.Value is < 1886 or > 9999)
    {
      yield return new ValidationFailure(nameof(year), "validation.vehicleYear");
    }

    if (!string.IsNullOrWhiteSpace(parkingSpaceIdentifier) &&
      VehicleCode.NormalizeIdentifier(parkingSpaceIdentifier) is null)
    {
      yield return new ValidationFailure(nameof(parkingSpaceIdentifier), "validation.parkingSpaceIdentifier");
    }
  }

  private static IEnumerable<ValidationFailure> ValidateId(Guid id, string propertyName = "id")
  {
    if (id == Guid.Empty)
    {
      yield return new ValidationFailure(propertyName, ValidationMessageKeys.Required);
    }
  }

  private static HashSet<string> NormalizeConfiguredParkingIdentifiers(string? identifiers)
  {
    var values = new HashSet<string>(StringComparer.Ordinal);
    if (string.IsNullOrWhiteSpace(identifiers))
    {
      return values;
    }

    foreach (var token in identifiers.Split(ParkingIdentifierSeparators, StringSplitOptions.RemoveEmptyEntries))
    {
      var normalized = VehicleCode.NormalizeIdentifier(token);
      if (normalized is not null)
      {
        values.Add(normalized);
      }
    }

    return values;
  }

  private static EntityId? ToEntityIdOrNull(Guid? id) =>
    id.HasValue && id.Value != Guid.Empty ? new EntityId(id.Value) : null;

  private static bool HasNonEmptyId(Guid? id) => id.HasValue && id.Value != Guid.Empty;

  private static bool IsActiveParkingStatus(VehicleAuthorizationStatus status) =>
    status is VehicleAuthorizationStatus.Pending or VehicleAuthorizationStatus.Authorized;

  private sealed record ResolvedVehicleEntities(
    IReadOnlyList<ValidationFailure> Errors,
    EntityId? ContractId,
    EntityId? PropertyId,
    EntityId? ResidentId,
    VehiclePropertySnapshot? Property,
    VehicleContractSnapshot? Contract,
    VehicleResidentSnapshot? Resident);
}
