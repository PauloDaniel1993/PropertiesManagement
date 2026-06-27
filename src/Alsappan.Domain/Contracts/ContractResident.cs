using Alsappan.Domain.Common.Identifiers;
using Alsappan.Domain.Common.Metadata;

namespace Alsappan.Domain.Contracts;

public sealed class ContractResident : TenantScopedEntity<EntityId>
{
  private ContractResident()
  {
  }

  private ContractResident(
    EntityId id,
    OrganizationId organizationId,
    EntityId contractId,
    EntityId residentId,
    bool isPrimary,
    DateTimeOffset createdAt,
    UserId? createdByUserId)
    : base(id, organizationId, createdAt, createdByUserId)
  {
    ContractId = contractId;
    ResidentId = residentId;
    IsPrimary = isPrimary;
  }

  public EntityId ContractId { get; private set; }

  public EntityId ResidentId { get; private set; }

  public bool IsPrimary { get; private set; }

  public static ContractResident Create(
    EntityId id,
    OrganizationId organizationId,
    EntityId contractId,
    EntityId residentId,
    bool isPrimary,
    DateTimeOffset createdAt,
    UserId? createdByUserId = null) =>
    new(id, organizationId, contractId, residentId, isPrimary, createdAt, createdByUserId);

  internal void SetPrimary(bool isPrimary, DateTimeOffset updatedAt, UserId? updatedByUserId)
  {
    IsPrimary = isPrimary;
    MarkUpdated(updatedAt, updatedByUserId);
  }
}
