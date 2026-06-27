using Alsappan.Application.Common.Contracts;
using Alsappan.Application.Vehicles;
using Alsappan.Application.Vehicles.Repositories;
using Alsappan.Domain.Common.Identifiers;
using Alsappan.Domain.Contracts;
using Alsappan.Domain.Properties;
using Alsappan.Domain.Residents;
using Alsappan.Domain.Vehicles;
using Alsappan.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Alsappan.Infrastructure.Vehicles;

public sealed class EfVehicleRepository : IVehicleRepository
{
  private readonly AlsappanDbContext dbContext;

  public EfVehicleRepository(AlsappanDbContext dbContext)
  {
    this.dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
  }

  public async Task<PagedResultDto<VehicleSnapshot>> ListAsync(
    VehicleListRequestDto request,
    OrganizationId organizationId,
    CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(request);

    var listFilter = new ListFilterDto(
      request.Page,
      request.PageSize,
      request.Search,
      request.Sort,
      request.IncludeArchived);
    var query = BuildListQuery(request, organizationId);
    var total = await query.CountAsync(cancellationToken).ConfigureAwait(false);
    var rows = await ApplySort(query, request.Sort)
      .Skip(listFilter.Offset)
      .Take(listFilter.PageSize)
      .ToListAsync(cancellationToken)
      .ConfigureAwait(false);
    var snapshots = new List<VehicleSnapshot>(rows.Count);

    foreach (var vehicle in rows)
    {
      snapshots.Add(await BuildSnapshotAsync(vehicle, organizationId, cancellationToken).ConfigureAwait(false));
    }

    return new PagedResultDto<VehicleSnapshot>(snapshots, listFilter.Page, listFilter.PageSize, total);
  }

  public Task<Vehicle?> FindAsync(
    EntityId vehicleId,
    OrganizationId organizationId,
    bool includeArchived = false,
    CancellationToken cancellationToken = default)
  {
    var query = dbContext.Vehicles.IgnoreQueryFilters()
      .Where(vehicle => vehicle.Id == vehicleId && vehicle.OrganizationId == organizationId);

    if (!includeArchived)
    {
      query = query.Where(vehicle =>
        vehicle.DeletedAt == null && vehicle.AuthorizationStatus != VehicleAuthorizationStatus.Archived);
    }

    return query.FirstOrDefaultAsync(cancellationToken);
  }

  public async Task<VehicleSnapshot?> FindSnapshotAsync(
    EntityId vehicleId,
    OrganizationId organizationId,
    bool includeArchived = false,
    CancellationToken cancellationToken = default)
  {
    var vehicle = await FindAsync(vehicleId, organizationId, includeArchived, cancellationToken)
      .ConfigureAwait(false);

    return vehicle is null
      ? null
      : await BuildSnapshotAsync(vehicle, organizationId, cancellationToken).ConfigureAwait(false);
  }

  public async Task<VehicleContractSnapshot?> GetContractSnapshotAsync(
    EntityId contractId,
    OrganizationId organizationId,
    CancellationToken cancellationToken = default)
  {
    var contract = await dbContext.Contracts.IgnoreQueryFilters()
      .Include(candidate => candidate.Residents)
      .AsNoTracking()
      .Where(candidate => candidate.Id == contractId && candidate.OrganizationId == organizationId)
      .FirstOrDefaultAsync(cancellationToken)
      .ConfigureAwait(false);

    if (contract is null)
    {
      return null;
    }

    var property = await GetPropertySnapshotAsync(contract.PropertyId, organizationId, cancellationToken)
      .ConfigureAwait(false);
    var resident = await GetResidentSnapshotAsync(contract.PrimaryResidentId, organizationId, cancellationToken)
      .ConfigureAwait(false);
    var propertyName = property?.Name ?? contract.PropertyId.Value.ToString("D");
    var residentName = resident?.Name ?? contract.PrimaryResidentId.Value.ToString("D");
    var displayName = $"Contrato {contract.StartDate:yyyy-MM} - {propertyName}";
    var residentIds = contract.Residents
      .Where(contractResident => contractResident.DeletedAt is null)
      .Select(contractResident => contractResident.ResidentId)
      .DefaultIfEmpty(contract.PrimaryResidentId)
      .Distinct()
      .ToArray();

    return new VehicleContractSnapshot(
      contract.Id,
      contract.PropertyId,
      contract.PrimaryResidentId,
      residentIds,
      displayName,
      propertyName,
      residentName,
      contract.DeletedAt is null && contract.Status == ContractStatus.Active);
  }

  public async Task<VehiclePropertySnapshot?> GetPropertySnapshotAsync(
    EntityId propertyId,
    OrganizationId organizationId,
    CancellationToken cancellationToken = default)
  {
    var property = await dbContext.Properties.IgnoreQueryFilters()
      .Where(candidate => candidate.Id == propertyId &&
        candidate.OrganizationId == organizationId &&
        candidate.DeletedAt == null &&
        candidate.Status != PropertyStatus.Archived)
      .Select(candidate => new
      {
        candidate.Id,
        candidate.Name,
        candidate.GarageSpaceCount,
        candidate.GarageSpaceIdentifiers,
        candidate.Address.StreetLine,
        candidate.Address.Number,
        candidate.Address.Neighborhood,
        candidate.Address.City,
        candidate.Address.StateCode
      })
      .FirstOrDefaultAsync(cancellationToken)
      .ConfigureAwait(false);

    return property is null
      ? null
      : new VehiclePropertySnapshot(
        property.Id,
        property.Name,
        $"{property.StreetLine}, {property.Number} - {property.Neighborhood}, {property.City}/{property.StateCode}",
        property.GarageSpaceCount,
        property.GarageSpaceIdentifiers);
  }

  public async Task<VehicleResidentSnapshot?> GetResidentSnapshotAsync(
    EntityId residentId,
    OrganizationId organizationId,
    CancellationToken cancellationToken = default)
  {
    var resident = await dbContext.Residents.IgnoreQueryFilters()
      .Where(candidate => candidate.Id == residentId &&
        candidate.OrganizationId == organizationId &&
        candidate.DeletedAt == null &&
        candidate.Status != ResidentStatus.Archived)
      .Select(candidate => new { candidate.Id, candidate.FullName })
      .FirstOrDefaultAsync(cancellationToken)
      .ConfigureAwait(false);

    return resident is null
      ? null
      : new VehicleResidentSnapshot(resident.Id, resident.FullName);
  }

  public Task<bool> HasActiveParkingAllocationAsync(
    OrganizationId organizationId,
    EntityId propertyId,
    string normalizedParkingSpaceIdentifier,
    EntityId? ignoredVehicleId = null,
    CancellationToken cancellationToken = default)
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(normalizedParkingSpaceIdentifier);

    var query = dbContext.Vehicles.IgnoreQueryFilters()
      .Where(vehicle => vehicle.OrganizationId == organizationId &&
        vehicle.PropertyId == propertyId &&
        vehicle.NormalizedParkingSpaceIdentifier == normalizedParkingSpaceIdentifier &&
        vehicle.DeletedAt == null &&
        (vehicle.AuthorizationStatus == VehicleAuthorizationStatus.Pending ||
          vehicle.AuthorizationStatus == VehicleAuthorizationStatus.Authorized));

    if (ignoredVehicleId.HasValue)
    {
      query = query.Where(vehicle => vehicle.Id != ignoredVehicleId.Value);
    }

    return query.AnyAsync(cancellationToken);
  }

  public async Task AddAsync(Vehicle vehicle, CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(vehicle);

    dbContext.Vehicles.Add(vehicle);
    await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
  }

  public async Task UpdateAsync(Vehicle vehicle, CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(vehicle);

    dbContext.Vehicles.Update(vehicle);
    await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
  }

  private IQueryable<Vehicle> BuildListQuery(
    VehicleListRequestDto request,
    OrganizationId organizationId)
  {
    var query = dbContext.Vehicles.IgnoreQueryFilters()
      .Where(vehicle => vehicle.OrganizationId == organizationId);

    if (!request.IncludeArchived)
    {
      query = query.Where(vehicle =>
        vehicle.DeletedAt == null && vehicle.AuthorizationStatus != VehicleAuthorizationStatus.Archived);
    }

    if (!string.IsNullOrWhiteSpace(request.Search))
    {
      var search = VehicleCode.NormalizeSearchText(request.Search);
      var normalizedPlateSearch = VehicleCode.NormalizePlate(request.Search);
      query = normalizedPlateSearch is null
        ? query.Where(vehicle => vehicle.SearchText.Contains(search))
        : query.Where(vehicle =>
          vehicle.SearchText.Contains(search) || vehicle.NormalizedPlate.Contains(normalizedPlateSearch));
    }

    if (!string.IsNullOrWhiteSpace(request.Plate))
    {
      var normalizedPlate = VehicleCode.NormalizePlate(request.Plate);
      if (normalizedPlate is not null)
      {
        query = query.Where(vehicle => vehicle.NormalizedPlate.Contains(normalizedPlate));
      }
    }

    if (request.ResidentId.HasValue && request.ResidentId.Value != Guid.Empty)
    {
      var residentId = new EntityId(request.ResidentId.Value);
      query = query.Where(vehicle => vehicle.ResidentId == residentId);
    }

    if (request.PropertyId.HasValue && request.PropertyId.Value != Guid.Empty)
    {
      var propertyId = new EntityId(request.PropertyId.Value);
      query = query.Where(vehicle => vehicle.PropertyId == propertyId);
    }

    if (request.ContractId.HasValue && request.ContractId.Value != Guid.Empty)
    {
      var contractId = new EntityId(request.ContractId.Value);
      query = query.Where(vehicle => vehicle.ContractId == contractId);
    }

    if (VehicleCatalog.TryParseType(request.Type, out var type))
    {
      query = query.Where(vehicle => vehicle.Type == type);
    }

    if (VehicleCatalog.TryParseAuthorizationStatus(request.AuthorizationStatus, out var status))
    {
      query = status == VehicleAuthorizationStatus.Archived
        ? query.Where(vehicle =>
          vehicle.DeletedAt != null || vehicle.AuthorizationStatus == VehicleAuthorizationStatus.Archived)
        : query.Where(vehicle => vehicle.DeletedAt == null && vehicle.AuthorizationStatus == status);
    }

    if (request.HasParkingAllocation.HasValue)
    {
      query = request.HasParkingAllocation.Value
        ? query.Where(vehicle => vehicle.NormalizedParkingSpaceIdentifier != null)
        : query.Where(vehicle => vehicle.NormalizedParkingSpaceIdentifier == null);
    }

    if (!string.IsNullOrWhiteSpace(request.ParkingSpaceIdentifier))
    {
      var normalizedParking = VehicleCode.NormalizeIdentifier(request.ParkingSpaceIdentifier);
      if (normalizedParking is not null)
      {
        query = query.Where(vehicle => vehicle.NormalizedParkingSpaceIdentifier == normalizedParking);
      }
    }

    return query;
  }

  private static IQueryable<Vehicle> ApplySort(IQueryable<Vehicle> query, string? sort)
  {
    var descending = sort?.Length > 0 && sort[0] == '-';
    var key = descending ? sort![1..] : sort;

    return VehicleCode.NormalizeCode(string.IsNullOrWhiteSpace(key) ? "plate" : key!) switch
    {
      "status" or "authorization-status" => descending
        ? query.OrderByDescending(vehicle => vehicle.AuthorizationStatus).ThenBy(vehicle => vehicle.NormalizedPlate)
        : query.OrderBy(vehicle => vehicle.AuthorizationStatus).ThenBy(vehicle => vehicle.NormalizedPlate),
      "type" => descending
        ? query.OrderByDescending(vehicle => vehicle.Type).ThenBy(vehicle => vehicle.NormalizedPlate)
        : query.OrderBy(vehicle => vehicle.Type).ThenBy(vehicle => vehicle.NormalizedPlate),
      "parking" or "parking-space" => descending
        ? query.OrderByDescending(vehicle => vehicle.NormalizedParkingSpaceIdentifier).ThenBy(vehicle => vehicle.NormalizedPlate)
        : query.OrderBy(vehicle => vehicle.NormalizedParkingSpaceIdentifier).ThenBy(vehicle => vehicle.NormalizedPlate),
      "created-at" => descending
        ? query.OrderByDescending(vehicle => vehicle.CreatedAt).ThenBy(vehicle => vehicle.NormalizedPlate)
        : query.OrderBy(vehicle => vehicle.CreatedAt).ThenBy(vehicle => vehicle.NormalizedPlate),
      _ => descending
        ? query.OrderByDescending(vehicle => vehicle.NormalizedPlate).ThenBy(vehicle => vehicle.Id.Value)
        : query.OrderBy(vehicle => vehicle.NormalizedPlate).ThenBy(vehicle => vehicle.Id.Value)
    };
  }

  private async Task<VehicleSnapshot> BuildSnapshotAsync(
    Vehicle vehicle,
    OrganizationId organizationId,
    CancellationToken cancellationToken)
  {
    var contract = vehicle.ContractId.HasValue
      ? await GetContractSnapshotAsync(vehicle.ContractId.Value, organizationId, cancellationToken).ConfigureAwait(false)
      : null;
    var propertyId = vehicle.PropertyId ?? contract?.PropertyId;
    var property = propertyId.HasValue
      ? await GetPropertySnapshotAsync(propertyId.Value, organizationId, cancellationToken).ConfigureAwait(false)
      : null;
    var resident = await GetResidentSnapshotAsync(vehicle.ResidentId, organizationId, cancellationToken)
      .ConfigureAwait(false) ??
      new VehicleResidentSnapshot(vehicle.ResidentId, vehicle.ResidentId.Value.ToString("D"));

    return new VehicleSnapshot(vehicle, resident, property, contract);
  }
}
