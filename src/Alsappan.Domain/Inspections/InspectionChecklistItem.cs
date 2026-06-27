using Alsappan.Domain.Common.Identifiers;
using Alsappan.Domain.Common.Metadata;

namespace Alsappan.Domain.Inspections;

public sealed class InspectionChecklistItem : TenantScopedEntity<EntityId>
{
  private InspectionChecklistItem()
  {
  }

  private InspectionChecklistItem(
    EntityId id,
    OrganizationId organizationId,
    EntityId inspectionId,
    string areaName,
    string itemName,
    bool isRequired,
    InspectionConditionRating conditionRating,
    string? observations,
    int sortOrder,
    DateTimeOffset createdAt,
    UserId? createdByUserId)
    : base(id, organizationId, createdAt, createdByUserId)
  {
    InspectionId = inspectionId;
    Apply(areaName, itemName, isRequired, conditionRating, observations, sortOrder);
  }

  public EntityId InspectionId { get; private set; }

  public string AreaName { get; private set; } = string.Empty;

  public string ItemName { get; private set; } = string.Empty;

  public bool IsRequired { get; private set; }

  public InspectionConditionRating ConditionRating { get; private set; }

  public string? Observations { get; private set; }

  public int SortOrder { get; private set; }

  public bool IsComplete =>
    !IsRequired || ConditionRating != InspectionConditionRating.Pending;

  public static InspectionChecklistItem Create(
    EntityId id,
    OrganizationId organizationId,
    EntityId inspectionId,
    string areaName,
    string itemName,
    bool isRequired,
    InspectionConditionRating conditionRating,
    string? observations,
    int sortOrder,
    DateTimeOffset createdAt,
    UserId? createdByUserId = null) =>
    new(
      id,
      organizationId,
      inspectionId,
      areaName,
      itemName,
      isRequired,
      conditionRating,
      observations,
      sortOrder,
      createdAt,
      createdByUserId);

  public void Update(
    string areaName,
    string itemName,
    bool isRequired,
    InspectionConditionRating conditionRating,
    string? observations,
    int sortOrder,
    DateTimeOffset updatedAt,
    UserId? updatedByUserId)
  {
    Apply(areaName, itemName, isRequired, conditionRating, observations, sortOrder);
    MarkUpdated(updatedAt, updatedByUserId);
  }

  public void Remove(DateTimeOffset deletedAt, UserId? deletedByUserId)
  {
    if (IsDeleted)
    {
      return;
    }

    MarkDeleted(deletedAt, deletedByUserId);
  }

  private void Apply(
    string areaName,
    string itemName,
    bool isRequired,
    InspectionConditionRating conditionRating,
    string? observations,
    int sortOrder)
  {
    AreaName = InspectionCode.Required(areaName, 160, nameof(areaName));
    ItemName = InspectionCode.Required(itemName, 200, nameof(itemName));
    IsRequired = isRequired;
    ConditionRating = conditionRating;
    Observations = InspectionCode.Optional(observations, 2000, nameof(observations));
    SortOrder = sortOrder < 0
      ? throw new ArgumentOutOfRangeException(nameof(sortOrder), "Sort order cannot be negative.")
      : sortOrder;
  }
}
