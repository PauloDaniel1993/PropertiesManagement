using Alsappan.Application.Common.Contracts;
using Alsappan.Domain.Common.Identifiers;
using Alsappan.Domain.Inspections;

namespace Alsappan.Application.Inspections.Repositories;

public sealed record InspectionSnapshot(
  Inspection Inspection,
  InspectionPropertySnapshot Property,
  InspectionContractSnapshot? Contract,
  InspectionResidentSnapshot? Resident,
  InspectionUserSnapshot Assignee,
  IReadOnlyList<InspectionDocumentSnapshot> Documents);

public sealed record InspectionPropertySnapshot(
  EntityId PropertyId,
  string Name,
  string? Location);

public sealed record InspectionResidentSnapshot(
  EntityId ResidentId,
  string Name);

public sealed record InspectionContractSnapshot(
  EntityId ContractId,
  EntityId PropertyId,
  EntityId PrimaryResidentId,
  IReadOnlyList<EntityId> ResidentIds,
  string DisplayName,
  string PropertyName,
  string ResidentName,
  bool IsArchived);

public sealed record InspectionUserSnapshot(
  UserId UserId,
  string Name,
  string? Email);

public sealed record InspectionDocumentSnapshot(
  EntityId DocumentId,
  EntityId? ChecklistItemId,
  InspectionDocumentKind Kind,
  string? Label);

public interface IInspectionRepository
{
  Task<PagedResultDto<InspectionSnapshot>> ListAsync(
    InspectionListRequestDto request,
    OrganizationId organizationId,
    DateTimeOffset now,
    CancellationToken cancellationToken = default);

  Task<Inspection?> FindAsync(
    EntityId inspectionId,
    OrganizationId organizationId,
    bool includeArchived = false,
    CancellationToken cancellationToken = default);

  Task<InspectionSnapshot?> FindSnapshotAsync(
    EntityId inspectionId,
    OrganizationId organizationId,
    bool includeArchived = false,
    CancellationToken cancellationToken = default);

  Task<InspectionPropertySnapshot?> GetPropertySnapshotAsync(
    EntityId propertyId,
    OrganizationId organizationId,
    CancellationToken cancellationToken = default);

  Task<InspectionContractSnapshot?> GetContractSnapshotAsync(
    EntityId contractId,
    OrganizationId organizationId,
    CancellationToken cancellationToken = default);

  Task<InspectionResidentSnapshot?> GetResidentSnapshotAsync(
    EntityId residentId,
    OrganizationId organizationId,
    CancellationToken cancellationToken = default);

  Task<InspectionUserSnapshot?> GetAssigneeSnapshotAsync(
    UserId userId,
    OrganizationId organizationId,
    CancellationToken cancellationToken = default);

  Task<bool> DocumentExistsAsync(
    EntityId documentId,
    OrganizationId organizationId,
    CancellationToken cancellationToken = default);

  Task AddAsync(Inspection inspection, CancellationToken cancellationToken = default);

  Task UpdateAsync(Inspection inspection, CancellationToken cancellationToken = default);
}
