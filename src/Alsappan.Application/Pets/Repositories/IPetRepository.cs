using Alsappan.Application.Common.Contracts;
using Alsappan.Domain.Common.Identifiers;
using Alsappan.Domain.Pets;

namespace Alsappan.Application.Pets.Repositories;

public sealed record PetSnapshot(
  Pet Pet,
  PetResidentSnapshot Resident,
  PetPropertySnapshot? Property,
  PetContractSnapshot? Contract,
  IReadOnlyList<PetDocumentSnapshot> Documents);

public sealed record PetResidentSnapshot(
  EntityId ResidentId,
  string Name);

public sealed record PetPropertySnapshot(
  EntityId PropertyId,
  string Name,
  string? Location);

public sealed record PetContractSnapshot(
  EntityId ContractId,
  EntityId PropertyId,
  EntityId PrimaryResidentId,
  IReadOnlyList<EntityId> ResidentIds,
  string DisplayName,
  string PropertyName,
  string ResidentName,
  bool IsActive);

public sealed record PetDocumentSnapshot(
  EntityId DocumentId,
  PetDocumentKind Kind,
  string? Label);

public interface IPetRepository
{
  Task<PagedResultDto<PetSnapshot>> ListAsync(
    PetListRequestDto request,
    OrganizationId organizationId,
    CancellationToken cancellationToken = default);

  Task<Pet?> FindAsync(
    EntityId petId,
    OrganizationId organizationId,
    bool includeArchived = false,
    CancellationToken cancellationToken = default);

  Task<PetSnapshot?> FindSnapshotAsync(
    EntityId petId,
    OrganizationId organizationId,
    bool includeArchived = false,
    CancellationToken cancellationToken = default);

  Task<PetResidentSnapshot?> GetResidentSnapshotAsync(
    EntityId residentId,
    OrganizationId organizationId,
    CancellationToken cancellationToken = default);

  Task<PetPropertySnapshot?> GetPropertySnapshotAsync(
    EntityId propertyId,
    OrganizationId organizationId,
    CancellationToken cancellationToken = default);

  Task<PetContractSnapshot?> GetContractSnapshotAsync(
    EntityId contractId,
    OrganizationId organizationId,
    CancellationToken cancellationToken = default);

  Task<bool> DocumentExistsAsync(
    EntityId documentId,
    OrganizationId organizationId,
    CancellationToken cancellationToken = default);

  Task AddAsync(Pet pet, CancellationToken cancellationToken = default);

  Task UpdateAsync(Pet pet, CancellationToken cancellationToken = default);
}
