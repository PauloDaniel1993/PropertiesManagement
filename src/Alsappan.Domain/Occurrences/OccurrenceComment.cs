using Alsappan.Domain.Common.Identifiers;
using Alsappan.Domain.Common.Metadata;

namespace Alsappan.Domain.Occurrences;

public sealed class OccurrenceComment : TenantScopedEntity<EntityId>
{
  private OccurrenceComment()
  {
  }

  private OccurrenceComment(
    EntityId id,
    OrganizationId organizationId,
    EntityId occurrenceId,
    string body,
    bool isInternal,
    DateTimeOffset createdAt,
    UserId? createdByUserId)
    : base(id, organizationId, createdAt, createdByUserId)
  {
    OccurrenceId = occurrenceId;
    Body = OccurrenceCode.Required(body, 4000, nameof(body));
    IsInternal = isInternal;
  }

  public EntityId OccurrenceId { get; private set; }

  public string Body { get; private set; } = string.Empty;

  public bool IsInternal { get; private set; }

  public static OccurrenceComment Create(
    EntityId id,
    OrganizationId organizationId,
    EntityId occurrenceId,
    string body,
    bool isInternal,
    DateTimeOffset createdAt,
    UserId? createdByUserId = null) =>
    new(id, organizationId, occurrenceId, body, isInternal, createdAt, createdByUserId);
}
