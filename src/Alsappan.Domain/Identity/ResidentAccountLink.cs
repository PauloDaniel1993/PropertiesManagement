using Alsappan.Domain.Common.Identifiers;
using Alsappan.Domain.Common.Metadata;

namespace Alsappan.Domain.Identity;

public sealed class ResidentAccountLink :
  TenantScopedEntity<EntityId>
{
  private ResidentAccountLink()
  {
  }

  public ResidentAccountLink(
    EntityId id,
    OrganizationId organizationId,
    UserId userId,
    EntityId residentId,
    DateTimeOffset createdAt,
    UserId? createdByUserId = null,
    bool isActive = true)
    : base(id, organizationId, createdAt, createdByUserId)
  {
    UserId = userId;
    ResidentId = residentId;
    IsActive = isActive;
  }

  public UserId UserId { get; private set; }

  public EntityId ResidentId { get; private set; }

  public bool IsActive { get; private set; }

  public void Deactivate(DateTimeOffset updatedAt, UserId? updatedByUserId)
  {
    IsActive = false;
    MarkUpdated(updatedAt, updatedByUserId);
  }

  public void Reactivate(DateTimeOffset updatedAt, UserId? updatedByUserId)
  {
    IsActive = true;
    MarkUpdated(updatedAt, updatedByUserId);
  }
}
