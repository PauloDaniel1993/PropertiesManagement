using Alsappan.Application.Common.Audit;
using Alsappan.Application.Common.Auth;
using Alsappan.Application.Common.Authorization;
using Alsappan.Application.Common.Contracts;
using Alsappan.Application.Common.Events;
using Alsappan.Application.Common.Results;
using Alsappan.Application.Common.Tenancy;
using Alsappan.Application.Vehicles;
using Alsappan.Application.Vehicles.Repositories;
using Alsappan.Domain.Common.Events;
using Alsappan.Domain.Common.Identifiers;
using Alsappan.Domain.Vehicles;

namespace Alsappan.Application.Tests.Vehicles;

public sealed class VehicleServiceTests
{
  [Fact]
  public async Task CreateAsyncResolvesActiveContractParkingAndWritesSideEffects()
  {
    var organizationId = OrganizationId.New();
    var repository = new FakeVehicleRepository(organizationId);
    var audit = new RecordingAuditWriter();
    var outbox = new RecordingOutboxWriter();
    var service = CreateService(
      organizationId,
      repository,
      audit,
      outbox,
      [PermissionCodes.Write(PermissionModules.Vehicles)]);

    var result = await service.CreateAsync(CreateRequest(
      repository.Contract.ContractId.Value,
      repository.Contract.PropertyId.Value,
      repository.Contract.PrimaryResidentId.Value,
      "áb-c 1234",
      "vaga-á1"));

    Assert.True(result.Succeeded);
    Assert.Equal("ABC1234", result.Value!.NormalizedPlate);
    Assert.Equal(repository.Property.PropertyId.Value, result.Value.Property!.Id);
    Assert.Equal(repository.Resident.ResidentId.Value, result.Value.Resident.Id);
    Assert.Equal("VAGAA1", result.Value.NormalizedParkingSpaceIdentifier);
    Assert.Single(repository.Vehicles);
    Assert.Single(audit.Entries);
    Assert.Equal("vehicle.created", Assert.Single(outbox.Envelopes).EventName);
  }

  [Fact]
  public async Task CreateAsyncRejectsDuplicateActiveParkingAllocation()
  {
    var organizationId = OrganizationId.New();
    var repository = new FakeVehicleRepository(organizationId);
    repository.Vehicles.Add(CreateVehicle(
      organizationId,
      repository.Property.PropertyId,
      repository.Resident.ResidentId,
      "XYZ-9876",
      "A1",
      VehicleAuthorizationStatus.Pending));
    var service = CreateService(
      organizationId,
      repository,
      new RecordingAuditWriter(),
      new RecordingOutboxWriter(),
      [PermissionCodes.Write(PermissionModules.Vehicles)]);

    var result = await service.CreateAsync(CreateRequest(
      null,
      repository.Property.PropertyId.Value,
      repository.Resident.ResidentId.Value,
      "ABC-1234",
      "a-1"));

    Assert.False(result.Succeeded);
    Assert.Equal(ApplicationOperationFailure.Validation, result.Failure);
    Assert.Contains("parkingSpaceIdentifier", result.Errors!.Keys);
  }

  [Fact]
  public async Task CreateAsyncRejectsPropertyOrResidentThatDoesNotBelongToContract()
  {
    var organizationId = OrganizationId.New();
    var repository = new FakeVehicleRepository(organizationId);
    var service = CreateService(
      organizationId,
      repository,
      new RecordingAuditWriter(),
      new RecordingOutboxWriter(),
      [PermissionCodes.Write(PermissionModules.Vehicles)]);

    var result = await service.CreateAsync(CreateRequest(
      repository.Contract.ContractId.Value,
      repository.OtherProperty.PropertyId.Value,
      repository.OtherResident.ResidentId.Value,
      "ABC-1234",
      null));

    Assert.False(result.Succeeded);
    Assert.Equal(ApplicationOperationFailure.Validation, result.Failure);
    Assert.Contains("propertyId", result.Errors!.Keys);
    Assert.Contains("residentId", result.Errors.Keys);
  }

  [Fact]
  public async Task CreateAsyncRejectsAuthorizationStatusWithoutManagePermission()
  {
    var organizationId = OrganizationId.New();
    var repository = new FakeVehicleRepository(organizationId);
    var service = CreateService(
      organizationId,
      repository,
      new RecordingAuditWriter(),
      new RecordingOutboxWriter(),
      [PermissionCodes.Write(PermissionModules.Vehicles)]);

    var request = CreateRequest(
      repository.Contract.ContractId.Value,
      repository.Contract.PropertyId.Value,
      repository.Contract.PrimaryResidentId.Value,
      "ABC-1234",
      null) with
    {
      AuthorizationStatus = "authorized"
    };

    var result = await service.CreateAsync(request);

    Assert.False(result.Succeeded);
    Assert.Equal(ApplicationOperationFailure.Forbidden, result.Failure);
    Assert.Empty(repository.Vehicles);
  }

  [Fact]
  public async Task CreateAsyncRejectsOversizedFieldsAsValidation()
  {
    var organizationId = OrganizationId.New();
    var repository = new FakeVehicleRepository(organizationId);
    var service = CreateService(
      organizationId,
      repository,
      new RecordingAuditWriter(),
      new RecordingOutboxWriter(),
      [PermissionCodes.Write(PermissionModules.Vehicles)]);

    var request = CreateRequest(
      repository.Contract.ContractId.Value,
      repository.Contract.PropertyId.Value,
      repository.Contract.PrimaryResidentId.Value,
      new string('A', 21),
      new string('B', 81)) with
    {
      Brand = new string('C', 121),
      Color = new string('D', 81),
      Model = new string('E', 121),
      Notes = new string('F', 2001),
      ParkingAllocationNotes = new string('G', 501)
    };

    var result = await service.CreateAsync(request);

    Assert.False(result.Succeeded);
    Assert.Equal(ApplicationOperationFailure.Validation, result.Failure);
    Assert.Contains("plate", result.Errors!.Keys);
    Assert.Contains("brand", result.Errors.Keys);
    Assert.Contains("color", result.Errors.Keys);
    Assert.Contains("model", result.Errors.Keys);
    Assert.Contains("notes", result.Errors.Keys);
    Assert.Contains("parkingAllocationNotes", result.Errors.Keys);
    Assert.Contains("parkingSpaceIdentifier", result.Errors.Keys);
    Assert.Empty(repository.Vehicles);
  }

  [Fact]
  public async Task CreateAsyncMapsParkingAllocationConflictToValidation()
  {
    var organizationId = OrganizationId.New();
    var repository = new FakeVehicleRepository(organizationId)
    {
      ThrowParkingConflictOnAdd = true
    };
    var service = CreateService(
      organizationId,
      repository,
      new RecordingAuditWriter(),
      new RecordingOutboxWriter(),
      [PermissionCodes.Write(PermissionModules.Vehicles)]);

    var result = await service.CreateAsync(CreateRequest(
      repository.Contract.ContractId.Value,
      repository.Contract.PropertyId.Value,
      repository.Contract.PrimaryResidentId.Value,
      "ABC-1234",
      "A1"));

    Assert.False(result.Succeeded);
    Assert.Equal(ApplicationOperationFailure.Validation, result.Failure);
    Assert.Contains("parkingSpaceIdentifier", result.Errors!.Keys);
    Assert.Empty(repository.Vehicles);
  }

  [Fact]
  public async Task CreateAsyncRejectsInactiveContractContext()
  {
    var organizationId = OrganizationId.New();
    var repository = new FakeVehicleRepository(organizationId);
    var service = CreateService(
      organizationId,
      repository,
      new RecordingAuditWriter(),
      new RecordingOutboxWriter(),
      [PermissionCodes.Write(PermissionModules.Vehicles)]);

    var result = await service.CreateAsync(CreateRequest(
      repository.InactiveContract.ContractId.Value,
      null,
      null,
      "ABC-1234",
      null));

    Assert.False(result.Succeeded);
    Assert.Equal(ApplicationOperationFailure.Validation, result.Failure);
    Assert.Contains("contractId", result.Errors!.Keys);
    Assert.Empty(repository.Vehicles);
  }

  [Fact]
  public async Task UpdateAsyncRejectsAuthorizationStatusChangeWithoutManagePermission()
  {
    var organizationId = OrganizationId.New();
    var repository = new FakeVehicleRepository(organizationId);
    var vehicle = CreateVehicle(
      organizationId,
      repository.Property.PropertyId,
      repository.Resident.ResidentId,
      "ABC-1234",
      null,
      VehicleAuthorizationStatus.Pending);
    repository.Vehicles.Add(vehicle);
    var service = CreateService(
      organizationId,
      repository,
      new RecordingAuditWriter(),
      new RecordingOutboxWriter(),
      [PermissionCodes.Write(PermissionModules.Vehicles)]);

    var result = await service.UpdateAsync(
      vehicle.Id.Value,
      UpdateRequest(repository.Property.PropertyId.Value, repository.Resident.ResidentId.Value) with
      {
        AuthorizationStatus = "authorized"
      });

    Assert.False(result.Succeeded);
    Assert.Equal(ApplicationOperationFailure.Forbidden, result.Failure);
    Assert.Equal(VehicleAuthorizationStatus.Pending, vehicle.AuthorizationStatus);
  }

  [Fact]
  public async Task AuthorizeAsyncRequiresManagePermission()
  {
    var organizationId = OrganizationId.New();
    var repository = new FakeVehicleRepository(organizationId);
    repository.Vehicles.Add(CreateVehicle(
      organizationId,
      repository.Property.PropertyId,
      repository.Resident.ResidentId,
      "ABC-1234",
      "A1",
      VehicleAuthorizationStatus.Pending));
    var service = CreateService(
      organizationId,
      repository,
      new RecordingAuditWriter(),
      new RecordingOutboxWriter(),
      [PermissionCodes.Write(PermissionModules.Vehicles)]);

    var result = await service.AuthorizeAsync(repository.Vehicles[0].Id.Value);

    Assert.False(result.Succeeded);
    Assert.Equal(ApplicationOperationFailure.Forbidden, result.Failure);
  }

  [Fact]
  public async Task AuthorizeAsyncMapsParkingAllocationConflictToValidation()
  {
    var organizationId = OrganizationId.New();
    var repository = new FakeVehicleRepository(organizationId)
    {
      ThrowParkingConflictOnUpdate = true
    };
    repository.Vehicles.Add(CreateVehicle(
      organizationId,
      repository.Property.PropertyId,
      repository.Resident.ResidentId,
      "ABC-1234",
      "A1",
      VehicleAuthorizationStatus.Pending));
    var service = CreateService(
      organizationId,
      repository,
      new RecordingAuditWriter(),
      new RecordingOutboxWriter(),
      [PermissionCodes.Manage(PermissionModules.Vehicles)]);

    var result = await service.AuthorizeAsync(repository.Vehicles[0].Id.Value);

    Assert.False(result.Succeeded);
    Assert.Equal(ApplicationOperationFailure.Validation, result.Failure);
    Assert.Contains("parkingSpaceIdentifier", result.Errors!.Keys);
  }

  [Fact]
  public async Task OptionsUseRequestedLocale()
  {
    var service = CreateService(
      OrganizationId.New(),
      new FakeVehicleRepository(OrganizationId.New()),
      new RecordingAuditWriter(),
      new RecordingOutboxWriter(),
      [PermissionCodes.Read(PermissionModules.Vehicles)]);

    var englishTypes = await service.GetTypeOptionsAsync("en-US");
    var portugueseStatuses = await service.GetAuthorizationStatusOptionsAsync("pt-BR");

    Assert.Contains(englishTypes, option => option.Code == "car" && option.Label == "Car");
    Assert.Contains(portugueseStatuses, option => option.Code == "authorized" && option.Label == "Autorizado");
  }

  private static VehicleService CreateService(
    OrganizationId organizationId,
    FakeVehicleRepository repository,
    RecordingAuditWriter auditWriter,
    RecordingOutboxWriter outboxWriter,
    IEnumerable<string> permissions) =>
    new(
      repository,
      new FixedPermissionService(permissions),
      new FixedActiveOrganizationContextResolver(organizationId),
      auditWriter,
      outboxWriter,
      TimeProvider.System);

  private static VehicleCreateRequestDto CreateRequest(
    Guid? contractId,
    Guid? propertyId,
    Guid? residentId,
    string plate,
    string? parkingSpaceIdentifier) =>
    new(
      residentId,
      propertyId,
      contractId,
      plate,
      "car",
      "Prata",
      "Honda",
      "Civic",
      2024,
      "pending",
      parkingSpaceIdentifier,
      "Vaga de garagem",
      "Observacao");

  private static VehicleUpdateRequestDto UpdateRequest(
    Guid propertyId,
    Guid residentId) =>
    new(
      residentId,
      propertyId,
      null,
      "ABC-1234",
      "car",
      "Prata",
      "Honda",
      "Civic",
      2024,
      "pending",
      null,
      null,
      "Observacao");

  private static Vehicle CreateVehicle(
    OrganizationId organizationId,
    EntityId propertyId,
    EntityId residentId,
    string plate,
    string? parkingSpaceIdentifier,
    VehicleAuthorizationStatus status) =>
    Vehicle.Create(
      EntityId.New(),
      organizationId,
      residentId,
      propertyId,
      null,
      plate,
      VehicleType.Car,
      "Prata",
      "Honda",
      "Civic",
      2024,
      status,
      parkingSpaceIdentifier,
      null,
      null,
      "Joao da Silva",
      "Casa Calabria",
      null,
      DateTimeOffset.UtcNow);

  private sealed class FakeVehicleRepository : IVehicleRepository
  {
    public FakeVehicleRepository(OrganizationId organizationId)
    {
      OrganizationId = organizationId;
      Property = new VehiclePropertySnapshot(
        EntityId.New(),
        "Casa Calabria",
        "Rua Calabria, 82",
        2,
        "Vaga A1; Vaga B2");
      OtherProperty = new VehiclePropertySnapshot(
        EntityId.New(),
        "Apartamento Centro",
        "Rua Central, 10",
        1,
        "C1");
      Resident = new VehicleResidentSnapshot(EntityId.New(), "Joao da Silva");
      OtherResident = new VehicleResidentSnapshot(EntityId.New(), "Maria Souza");
      Contract = new VehicleContractSnapshot(
        EntityId.New(),
        Property.PropertyId,
        Resident.ResidentId,
        [Resident.ResidentId],
        "Contrato Casa Calabria",
        Property.Name,
        Resident.Name,
        true);
      InactiveContract = Contract with
      {
        ContractId = EntityId.New(),
        IsActive = false
      };
    }

    public OrganizationId OrganizationId { get; }

    public VehiclePropertySnapshot Property { get; }

    public VehiclePropertySnapshot OtherProperty { get; }

    public VehicleResidentSnapshot Resident { get; }

    public VehicleResidentSnapshot OtherResident { get; }

    public VehicleContractSnapshot Contract { get; }

    public VehicleContractSnapshot InactiveContract { get; }

    public List<Vehicle> Vehicles { get; } = [];

    public bool ThrowParkingConflictOnAdd { get; init; }

    public bool ThrowParkingConflictOnUpdate { get; init; }

    public Task<PagedResultDto<VehicleSnapshot>> ListAsync(
      VehicleListRequestDto request,
      OrganizationId organizationId,
      CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();
      var rows = Vehicles
        .Where(vehicle => vehicle.OrganizationId == organizationId)
        .Select(BuildSnapshot)
        .ToArray();
      return Task.FromResult(new PagedResultDto<VehicleSnapshot>(
        rows,
        request.Page,
        request.PageSize,
        rows.Length));
    }

    public Task<Vehicle?> FindAsync(
      EntityId vehicleId,
      OrganizationId organizationId,
      bool includeArchived = false,
      CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();
      return Task.FromResult(Vehicles.FirstOrDefault(vehicle =>
        vehicle.Id == vehicleId &&
        vehicle.OrganizationId == organizationId &&
        (includeArchived || !vehicle.IsDeleted)));
    }

    public async Task<VehicleSnapshot?> FindSnapshotAsync(
      EntityId vehicleId,
      OrganizationId organizationId,
      bool includeArchived = false,
      CancellationToken cancellationToken = default)
    {
      var vehicle = await FindAsync(vehicleId, organizationId, includeArchived, cancellationToken)
        .ConfigureAwait(false);
      return vehicle is null ? null : BuildSnapshot(vehicle);
    }

    public Task<VehicleContractSnapshot?> GetContractSnapshotAsync(
      EntityId contractId,
      OrganizationId organizationId,
      CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();
      return Task.FromResult<VehicleContractSnapshot?>(
        organizationId == OrganizationId
          ? contractId == Contract.ContractId ? Contract : contractId == InactiveContract.ContractId ? InactiveContract : null
          : null);
    }

    public Task<VehiclePropertySnapshot?> GetPropertySnapshotAsync(
      EntityId propertyId,
      OrganizationId organizationId,
      CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();
      return Task.FromResult<VehiclePropertySnapshot?>(
        organizationId == OrganizationId
          ? propertyId == Property.PropertyId ? Property : propertyId == OtherProperty.PropertyId ? OtherProperty : null
          : null);
    }

    public Task<VehicleResidentSnapshot?> GetResidentSnapshotAsync(
      EntityId residentId,
      OrganizationId organizationId,
      CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();
      return Task.FromResult<VehicleResidentSnapshot?>(
        organizationId == OrganizationId
          ? residentId == Resident.ResidentId ? Resident : residentId == OtherResident.ResidentId ? OtherResident : null
          : null);
    }

    public Task<bool> HasActiveParkingAllocationAsync(
      OrganizationId organizationId,
      EntityId propertyId,
      string normalizedParkingSpaceIdentifier,
      EntityId? ignoredVehicleId = null,
      CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();
      return Task.FromResult(Vehicles.Any(vehicle =>
        vehicle.OrganizationId == organizationId &&
        vehicle.PropertyId == propertyId &&
        vehicle.NormalizedParkingSpaceIdentifier == normalizedParkingSpaceIdentifier &&
        vehicle.DeletedAt is null &&
        vehicle.AuthorizationStatus is VehicleAuthorizationStatus.Pending or VehicleAuthorizationStatus.Authorized &&
        (!ignoredVehicleId.HasValue || vehicle.Id != ignoredVehicleId.Value)));
    }

    public Task AddAsync(Vehicle vehicle, CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();
      if (ThrowParkingConflictOnAdd)
      {
        throw new VehicleParkingAllocationConflictException();
      }

      Vehicles.Add(vehicle);
      return Task.CompletedTask;
    }

    public Task UpdateAsync(Vehicle vehicle, CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();
      if (ThrowParkingConflictOnUpdate)
      {
        throw new VehicleParkingAllocationConflictException();
      }

      return Task.CompletedTask;
    }

    private VehicleSnapshot BuildSnapshot(Vehicle vehicle) =>
      new(
        vehicle,
        vehicle.ResidentId == Resident.ResidentId ? Resident : OtherResident,
        vehicle.PropertyId == Property.PropertyId ? Property : OtherProperty,
        vehicle.ContractId == Contract.ContractId ? Contract : null);
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

    public FixedActiveOrganizationContextResolver(OrganizationId organizationId)
    {
      var membership = new OrganizationMembership(organizationId, permissionCodes: [PermissionCodes.Wildcard]);
      var user = new AuthenticatedUser(UserId.New(), "admin@alsappan.local", "Paulo", [membership]);
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
