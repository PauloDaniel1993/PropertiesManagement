using Alsappan.Application.Common.Contracts;
using Alsappan.Domain.Common.Identifiers;
using Alsappan.Domain.Contracts;

namespace Alsappan.Application.Contracts.Repositories;

public sealed record ContractResidentSnapshot(
  EntityId ResidentId,
  string ResidentName,
  bool IsPrimary);

public sealed record ContractPropertySnapshot(
  EntityId PropertyId,
  string PropertyName,
  string? Location);

public sealed record ContractSnapshot(
  LeaseContract Contract,
  ContractPropertySnapshot Property,
  IReadOnlyList<ContractResidentSnapshot> Residents);

public interface IContractRepository
{
  Task<ContractSnapshot?> FindSnapshotAsync(
    EntityId contractId,
    OrganizationId organizationId,
    bool includeArchived = false,
    CancellationToken cancellationToken = default);

  Task<LeaseContract?> FindAsync(
    EntityId contractId,
    OrganizationId organizationId,
    bool includeArchived = false,
    CancellationToken cancellationToken = default);

  Task<PagedResultDto<ContractSnapshot>> ListAsync(
    ContractListRequestDto request,
    OrganizationId organizationId,
    DateOnly today,
    CancellationToken cancellationToken = default);

  Task<ContractPropertySnapshot?> GetPropertySnapshotAsync(
    EntityId propertyId,
    OrganizationId organizationId,
    CancellationToken cancellationToken = default);

  Task<IReadOnlyList<ContractResidentSnapshot>> GetResidentSnapshotsAsync(
    IReadOnlyCollection<EntityId> residentIds,
    EntityId primaryResidentId,
    OrganizationId organizationId,
    CancellationToken cancellationToken = default);

  Task<bool> HasOverlappingActiveContractAsync(
    EntityId propertyId,
    OrganizationId organizationId,
    DateOnly startDate,
    DateOnly? endDate,
    EntityId? ignoredContractId = null,
    CancellationToken cancellationToken = default);

  Task<bool> HasAnyActiveContractAsync(
    EntityId propertyId,
    OrganizationId organizationId,
    EntityId? ignoredContractId = null,
    CancellationToken cancellationToken = default);

  Task AddAsync(LeaseContract leaseContract, CancellationToken cancellationToken = default);

  Task UpdateAsync(LeaseContract leaseContract, CancellationToken cancellationToken = default);
}
