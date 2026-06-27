using Alsappan.Application.Common.Contracts;
using Alsappan.Domain.Common.Identifiers;
using Alsappan.Domain.Occurrences;

namespace Alsappan.Application.Occurrences.Repositories;

public sealed record OccurrenceSnapshot(
  Occurrence Occurrence,
  OccurrencePropertySnapshot? Property,
  OccurrenceResidentSnapshot? Resident,
  OccurrenceContractSnapshot? Contract,
  OccurrenceUserSnapshot? AssignedUser,
  IReadOnlyList<OccurrenceCommentSnapshot> Comments,
  IReadOnlyList<OccurrenceDocumentSnapshot> Attachments,
  IReadOnlyList<OccurrenceStatusHistorySnapshot> StatusHistory,
  IReadOnlyList<OccurrencePriorityHistorySnapshot> PriorityHistory,
  IReadOnlyList<OccurrenceAssignmentHistorySnapshot> AssignmentHistory);

public sealed record OccurrencePropertySnapshot(
  EntityId PropertyId,
  string Name,
  string? Location);

public sealed record OccurrenceResidentSnapshot(
  EntityId ResidentId,
  string Name);

public sealed record OccurrenceContractSnapshot(
  EntityId ContractId,
  EntityId PropertyId,
  EntityId PrimaryResidentId,
  IReadOnlyList<EntityId> ResidentIds,
  string DisplayName,
  string PropertyName,
  string ResidentName);

public sealed record OccurrenceUserSnapshot(
  UserId UserId,
  string DisplayName,
  string? Email);

public sealed record OccurrenceDocumentSnapshot(
  EntityId DocumentId,
  string? Label,
  DateTimeOffset CreatedAt);

public sealed record OccurrenceCommentSnapshot(
  OccurrenceComment Comment,
  OccurrenceUserSnapshot? Author);

public sealed record OccurrenceStatusHistorySnapshot(
  OccurrenceStatusHistory History,
  OccurrenceUserSnapshot? Actor);

public sealed record OccurrencePriorityHistorySnapshot(
  OccurrencePriorityHistory History,
  OccurrenceUserSnapshot? Actor);

public sealed record OccurrenceAssignmentHistorySnapshot(
  OccurrenceAssignmentHistory History,
  OccurrenceUserSnapshot? PreviousAssignedUser,
  OccurrenceUserSnapshot? NewAssignedUser,
  OccurrenceUserSnapshot? Actor);

public interface IOccurrenceRepository
{
  Task<PagedResultDto<OccurrenceSnapshot>> ListAsync(
    OccurrenceListRequestDto request,
    OrganizationId organizationId,
    CancellationToken cancellationToken = default);

  Task<Occurrence?> FindAsync(
    EntityId occurrenceId,
    OrganizationId organizationId,
    bool includeArchived = false,
    CancellationToken cancellationToken = default);

  Task<OccurrenceSnapshot?> FindSnapshotAsync(
    EntityId occurrenceId,
    OrganizationId organizationId,
    bool includeArchived = false,
    CancellationToken cancellationToken = default);

  Task<OccurrenceContractSnapshot?> GetContractSnapshotAsync(
    EntityId contractId,
    OrganizationId organizationId,
    CancellationToken cancellationToken = default);

  Task<OccurrencePropertySnapshot?> GetPropertySnapshotAsync(
    EntityId propertyId,
    OrganizationId organizationId,
    CancellationToken cancellationToken = default);

  Task<OccurrenceResidentSnapshot?> GetResidentSnapshotAsync(
    EntityId residentId,
    OrganizationId organizationId,
    CancellationToken cancellationToken = default);

  Task<OccurrenceUserSnapshot?> GetAssignableUserSnapshotAsync(
    UserId userId,
    OrganizationId organizationId,
    CancellationToken cancellationToken = default);

  Task<bool> DocumentExistsAsync(
    EntityId documentId,
    OrganizationId organizationId,
    CancellationToken cancellationToken = default);

  Task AddAsync(Occurrence occurrence, CancellationToken cancellationToken = default);

  Task UpdateAsync(Occurrence occurrence, CancellationToken cancellationToken = default);
}
