using Alsappan.Domain.Common.Identifiers;
using Alsappan.Domain.Common.Metadata;
using Alsappan.Domain.Common.ValueObjects;

namespace Alsappan.Domain.Properties;

public sealed class RentalProperty : TenantScopedEntity<EntityId>
{
  private RentalProperty()
  {
  }

  private RentalProperty(
    EntityId id,
    OrganizationId organizationId,
    string name,
    PropertyType type,
    string? description,
    Address address,
    PropertyStatus status,
    Money suggestedRent,
    int garageSpaceCount,
    string? garageSpaceIdentifiers,
    string? notes,
    DateTimeOffset createdAt,
    UserId? createdByUserId)
    : base(id, organizationId, createdAt, createdByUserId)
  {
    Name = PropertyCode.Required(name, nameof(name), 160);
    Type = RequireType(type);
    Description = PropertyCode.Optional(description, 500, nameof(description));
    Address = address ?? throw new ArgumentNullException(nameof(address));
    Status = RequireMutableStatus(status);
    SuggestedRent = suggestedRent ?? throw new ArgumentNullException(nameof(suggestedRent));
    GarageSpaceCount = RequireGarageSpaceCount(garageSpaceCount);
    GarageSpaceIdentifiers = PropertyCode.Optional(garageSpaceIdentifiers, 250, nameof(garageSpaceIdentifiers));
    Notes = PropertyCode.Optional(notes, 2000, nameof(notes));
    SearchText = BuildSearchText();
  }

  public string Name { get; private set; } = string.Empty;

  public PropertyType Type { get; private set; }

  public string? Description { get; private set; }

  public Address Address { get; private set; } = null!;

  public PropertyStatus Status { get; private set; }

  public Money SuggestedRent { get; private set; } = null!;

  public int GarageSpaceCount { get; private set; }

  public string? GarageSpaceIdentifiers { get; private set; }

  public string? Notes { get; private set; }

  public string SearchText { get; private set; } = string.Empty;

  public bool HasGarage => GarageSpaceCount > 0;

  public static RentalProperty Create(
    EntityId id,
    OrganizationId organizationId,
    string name,
    PropertyType type,
    string? description,
    Address address,
    PropertyStatus status,
    Money suggestedRent,
    int garageSpaceCount,
    string? garageSpaceIdentifiers,
    string? notes,
    DateTimeOffset createdAt,
    UserId? createdByUserId = null) =>
    new(
      id,
      organizationId,
      name,
      type,
      description,
      address,
      status,
      suggestedRent,
      garageSpaceCount,
      garageSpaceIdentifiers,
      notes,
      createdAt,
      createdByUserId);

  public void Update(
    string name,
    PropertyType type,
    string? description,
    Address address,
    PropertyStatus status,
    Money suggestedRent,
    int garageSpaceCount,
    string? garageSpaceIdentifiers,
    string? notes,
    DateTimeOffset updatedAt,
    UserId? updatedByUserId)
  {
    Name = PropertyCode.Required(name, nameof(name), 160);
    Type = RequireType(type);
    Description = PropertyCode.Optional(description, 500, nameof(description));
    Address = address ?? throw new ArgumentNullException(nameof(address));
    Status = RequireMutableStatus(status);
    SuggestedRent = suggestedRent ?? throw new ArgumentNullException(nameof(suggestedRent));
    GarageSpaceCount = RequireGarageSpaceCount(garageSpaceCount);
    GarageSpaceIdentifiers = PropertyCode.Optional(garageSpaceIdentifiers, 250, nameof(garageSpaceIdentifiers));
    Notes = PropertyCode.Optional(notes, 2000, nameof(notes));
    SearchText = BuildSearchText();
    MarkUpdated(updatedAt, updatedByUserId);
  }

  public void ChangeStatus(
    PropertyStatus status,
    DateTimeOffset updatedAt,
    UserId? updatedByUserId)
  {
    Status = RequireMutableStatus(status);
    MarkUpdated(updatedAt, updatedByUserId);
  }

  public void Archive(DateTimeOffset deletedAt, UserId? deletedByUserId)
  {
    if (IsDeleted)
    {
      return;
    }

    Status = PropertyStatus.Archived;
    MarkDeleted(deletedAt, deletedByUserId);
  }

  public void Restore(DateTimeOffset restoredAt, UserId? restoredByUserId)
  {
    if (!IsDeleted)
    {
      return;
    }

    DeletedAt = null;
    DeletedByUserId = null;
    Status = PropertyStatus.Inactive;
    MarkUpdated(restoredAt, restoredByUserId);
  }

  private string BuildSearchText() =>
    PropertyCode.NormalizeSearchText(
      Name,
      Description,
      Address.StreetLine,
      Address.Number,
      Address.Complement,
      Address.Neighborhood,
      Address.City,
      Address.StateCode,
      Status.ToString(),
      Type.ToString());

  private static PropertyType RequireType(PropertyType type) =>
    type == PropertyType.None
      ? throw new ArgumentException("Property type is required.", nameof(type))
      : type;

  private static PropertyStatus RequireMutableStatus(PropertyStatus status) =>
    status is PropertyStatus.None or PropertyStatus.Archived
      ? throw new ArgumentException("Property status is not valid for this operation.", nameof(status))
      : status;

  private static int RequireGarageSpaceCount(int garageSpaceCount) =>
    garageSpaceCount < 0
      ? throw new ArgumentOutOfRangeException(nameof(garageSpaceCount), "Garage spaces cannot be negative.")
      : garageSpaceCount;
}
