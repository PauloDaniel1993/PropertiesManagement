using Alsappan.Domain.Common.Identifiers;
using Alsappan.Domain.Common.Metadata;

namespace Alsappan.Domain.Occurrences;

public sealed class OccurrenceStatusHistory : TenantScopedEntity<EntityId>
{
  private OccurrenceStatusHistory()
  {
  }

  private OccurrenceStatusHistory(
    EntityId id,
    OrganizationId organizationId,
    EntityId occurrenceId,
    OccurrenceStatus? previousStatus,
    OccurrenceStatus newStatus,
    string? notes,
    DateTimeOffset createdAt,
    UserId? createdByUserId)
    : base(id, organizationId, createdAt, createdByUserId)
  {
    OccurrenceId = occurrenceId;
    PreviousStatus = previousStatus;
    NewStatus = newStatus;
    Notes = OccurrenceCode.Optional(notes, 1000, nameof(notes));
  }

  public EntityId OccurrenceId { get; private set; }

  public OccurrenceStatus? PreviousStatus { get; private set; }

  public OccurrenceStatus NewStatus { get; private set; }

  public string? Notes { get; private set; }

  public static OccurrenceStatusHistory Create(
    EntityId id,
    OrganizationId organizationId,
    EntityId occurrenceId,
    OccurrenceStatus? previousStatus,
    OccurrenceStatus newStatus,
    string? notes,
    DateTimeOffset createdAt,
    UserId? createdByUserId = null) =>
    new(id, organizationId, occurrenceId, previousStatus, newStatus, notes, createdAt, createdByUserId);
}
