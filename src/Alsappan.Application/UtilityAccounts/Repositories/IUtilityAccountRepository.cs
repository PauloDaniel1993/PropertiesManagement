using Alsappan.Application.Common.Contracts;
using Alsappan.Domain.Common.Identifiers;
using Alsappan.Domain.Common.ValueObjects;
using Alsappan.Domain.UtilityAccounts;

namespace Alsappan.Application.UtilityAccounts.Repositories;

public sealed record UtilityAccountSnapshot(
  UtilityAccount UtilityAccount,
  UtilityPropertySnapshot? Property,
  UtilityContractSnapshot? Contract,
  UtilityResidentSnapshot? Resident,
  IReadOnlyList<UtilityDocumentSnapshot> Documents);

public sealed record UtilityPropertySnapshot(
  EntityId PropertyId,
  string Name,
  string? Location);

public sealed record UtilityContractSnapshot(
  EntityId ContractId,
  EntityId PropertyId,
  EntityId PrimaryResidentId,
  string DisplayName,
  string PropertyName,
  string ResidentName,
  bool IsActive);

public sealed record UtilityResidentSnapshot(
  EntityId ResidentId,
  string Name);

public sealed record UtilityDocumentSnapshot(
  EntityId DocumentId,
  UtilityDocumentKind Kind,
  string? Label);

public interface IUtilityAccountRepository
{
  Task<PagedResultDto<UtilityAccountSnapshot>> ListAsync(
    UtilityAccountListRequestDto request,
    OrganizationId organizationId,
    DateOnly today,
    CancellationToken cancellationToken = default);

  Task<UtilityAccount?> FindAsync(
    EntityId utilityAccountId,
    OrganizationId organizationId,
    bool includeArchived = false,
    CancellationToken cancellationToken = default);

  Task<UtilityAccountSnapshot?> FindSnapshotAsync(
    EntityId utilityAccountId,
    OrganizationId organizationId,
    bool includeArchived = false,
    CancellationToken cancellationToken = default);

  Task<UtilityContractSnapshot?> GetContractSnapshotAsync(
    EntityId contractId,
    OrganizationId organizationId,
    CancellationToken cancellationToken = default);

  Task<UtilityPropertySnapshot?> GetPropertySnapshotAsync(
    EntityId propertyId,
    OrganizationId organizationId,
    CancellationToken cancellationToken = default);

  Task<UtilityResidentSnapshot?> GetResidentSnapshotAsync(
    EntityId residentId,
    OrganizationId organizationId,
    CancellationToken cancellationToken = default);

  Task<bool> DocumentExistsAsync(
    EntityId documentId,
    OrganizationId organizationId,
    CancellationToken cancellationToken = default);

  Task AddAsync(UtilityAccount utilityAccount, CancellationToken cancellationToken = default);

  Task UpdateAsync(UtilityAccount utilityAccount, CancellationToken cancellationToken = default);
}
