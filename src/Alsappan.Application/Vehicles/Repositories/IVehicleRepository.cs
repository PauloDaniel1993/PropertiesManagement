using Alsappan.Application.Common.Contracts;
using Alsappan.Domain.Common.Identifiers;
using Alsappan.Domain.Vehicles;

namespace Alsappan.Application.Vehicles.Repositories;

public sealed record VehicleSnapshot(
  Vehicle Vehicle,
  VehicleResidentSnapshot Resident,
  VehiclePropertySnapshot? Property,
  VehicleContractSnapshot? Contract);

public sealed record VehicleResidentSnapshot(
  EntityId ResidentId,
  string Name);

public sealed record VehiclePropertySnapshot(
  EntityId PropertyId,
  string Name,
  string? Location,
  int GarageSpaceCount,
  string? GarageSpaceIdentifiers);

public sealed record VehicleContractSnapshot(
  EntityId ContractId,
  EntityId PropertyId,
  EntityId PrimaryResidentId,
  IReadOnlyList<EntityId> ResidentIds,
  string DisplayName,
  string PropertyName,
  string ResidentName,
  bool IsActive);

public interface IVehicleRepository
{
  Task<PagedResultDto<VehicleSnapshot>> ListAsync(
    VehicleListRequestDto request,
    OrganizationId organizationId,
    CancellationToken cancellationToken = default);

  Task<Vehicle?> FindAsync(
    EntityId vehicleId,
    OrganizationId organizationId,
    bool includeArchived = false,
    CancellationToken cancellationToken = default);

  Task<VehicleSnapshot?> FindSnapshotAsync(
    EntityId vehicleId,
    OrganizationId organizationId,
    bool includeArchived = false,
    CancellationToken cancellationToken = default);

  Task<VehicleContractSnapshot?> GetContractSnapshotAsync(
    EntityId contractId,
    OrganizationId organizationId,
    CancellationToken cancellationToken = default);

  Task<VehiclePropertySnapshot?> GetPropertySnapshotAsync(
    EntityId propertyId,
    OrganizationId organizationId,
    CancellationToken cancellationToken = default);

  Task<VehicleResidentSnapshot?> GetResidentSnapshotAsync(
    EntityId residentId,
    OrganizationId organizationId,
    CancellationToken cancellationToken = default);

  Task<bool> HasActiveParkingAllocationAsync(
    OrganizationId organizationId,
    EntityId propertyId,
    string normalizedParkingSpaceIdentifier,
    EntityId? ignoredVehicleId = null,
    CancellationToken cancellationToken = default);

  Task AddAsync(Vehicle vehicle, CancellationToken cancellationToken = default);

  Task UpdateAsync(Vehicle vehicle, CancellationToken cancellationToken = default);
}
