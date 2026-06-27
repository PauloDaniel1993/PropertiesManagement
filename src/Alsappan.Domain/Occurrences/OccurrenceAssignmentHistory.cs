using Alsappan.Domain.Common.Identifiers;
using Alsappan.Domain.Common.Metadata;

namespace Alsappan.Domain.Occurrences;

public sealed class OccurrenceAssignmentHistory : TenantScopedEntity<EntityId>
{
  private OccurrenceAssignmentHistory()
  {
  }

  private OccurrenceAssignmentHistory(
    EntityId id,
    OrganizationId organizationId,
    EntityId occurrenceId,
    UserId? previousAssignedUserId,
    UserId? newAssignedUserId,
    string? notes,
    DateTimeOffset createdAt,
    UserId? createdByUserId)
    : base(id, organizationId, createdAt, createdByUserId)
  {
    OccurrenceId = occurrenceId;
    PreviousAssignedUserId = previousAssignedUserId;
    NewAssignedUserId = newAssignedUserId;
    Notes = OccurrenceCode.Optional(notes, 1000, nameof(notes));
  }

  public EntityId OccurrenceId { get; private set; }

  public UserId? PreviousAssignedUserId { get; private set; }

  public UserId? NewAssignedUserId { get; private set; }

  public string? Notes { get; private set; }

  public static OccurrenceAssignmentHistory Create(
    EntityId id,
    OrganizationId organizationId,
    EntityId occurrenceId,
    UserId? previousAssignedUserId,
    UserId? newAssignedUserId,
    string? notes,
    DateTimeOffset createdAt,
    UserId? createdByUserId = null) =>
    new(
      id,
      organizationId,
      occurrenceId,
      previousAssignedUserId,
      newAssignedUserId,
      notes,
      createdAt,
      createdByUserId);
}
