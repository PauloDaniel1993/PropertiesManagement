using System.Globalization;
using Alsappan.Domain.Common.Identifiers;
using Alsappan.Domain.Common.Metadata;

namespace Alsappan.Domain.Vehicles;

public sealed class Vehicle : TenantScopedEntity<EntityId>
{
  private Vehicle()
  {
  }

  private Vehicle(
    EntityId id,
    OrganizationId organizationId,
    EntityId residentId,
    EntityId? propertyId,
    EntityId? contractId,
    string plate,
    VehicleType type,
    string? color,
    string? brand,
    string? model,
    int? year,
    VehicleAuthorizationStatus authorizationStatus,
    string? parkingSpaceIdentifier,
    string? parkingAllocationNotes,
    string? notes,
    string? residentSearchText,
    string? propertySearchText,
    string? contractSearchText,
    DateTimeOffset createdAt,
    UserId? createdByUserId)
    : base(id, organizationId, createdAt, createdByUserId)
  {
    ApplyDetails(
      residentId,
      propertyId,
      contractId,
      plate,
      type,
      color,
      brand,
      model,
      year,
      authorizationStatus,
      parkingSpaceIdentifier,
      parkingAllocationNotes,
      notes,
      residentSearchText,
      propertySearchText,
      contractSearchText);
  }

  public EntityId ResidentId { get; private set; }

  public EntityId? PropertyId { get; private set; }

  public EntityId? ContractId { get; private set; }

  public string Plate { get; private set; } = string.Empty;

  public string NormalizedPlate { get; private set; } = string.Empty;

  public VehicleType Type { get; private set; }

  public string? Color { get; private set; }

  public string? Brand { get; private set; }

  public string? Model { get; private set; }

  public int? Year { get; private set; }

  public VehicleAuthorizationStatus AuthorizationStatus { get; private set; }

  public string? ParkingSpaceIdentifier { get; private set; }

  public string? NormalizedParkingSpaceIdentifier { get; private set; }

  public string? ParkingAllocationNotes { get; private set; }

  public string? Notes { get; private set; }

  public string SearchText { get; private set; } = string.Empty;

  public bool HasParkingAllocation => NormalizedParkingSpaceIdentifier is not null;

  public static Vehicle Create(
    EntityId id,
    OrganizationId organizationId,
    EntityId residentId,
    EntityId? propertyId,
    EntityId? contractId,
    string plate,
    VehicleType type,
    string? color,
    string? brand,
    string? model,
    int? year,
    VehicleAuthorizationStatus authorizationStatus,
    string? parkingSpaceIdentifier,
    string? parkingAllocationNotes,
    string? notes,
    string? residentSearchText,
    string? propertySearchText,
    string? contractSearchText,
    DateTimeOffset createdAt,
    UserId? createdByUserId = null) =>
    new(
      id,
      organizationId,
      residentId,
      propertyId,
      contractId,
      plate,
      type,
      color,
      brand,
      model,
      year,
      authorizationStatus,
      parkingSpaceIdentifier,
      parkingAllocationNotes,
      notes,
      residentSearchText,
      propertySearchText,
      contractSearchText,
      createdAt,
      createdByUserId);

  public void Update(
    EntityId residentId,
    EntityId? propertyId,
    EntityId? contractId,
    string plate,
    VehicleType type,
    string? color,
    string? brand,
    string? model,
    int? year,
    VehicleAuthorizationStatus authorizationStatus,
    string? parkingSpaceIdentifier,
    string? parkingAllocationNotes,
    string? notes,
    string? residentSearchText,
    string? propertySearchText,
    string? contractSearchText,
    DateTimeOffset updatedAt,
    UserId? updatedByUserId)
  {
    EnsureCanMutate();
    ApplyDetails(
      residentId,
      propertyId,
      contractId,
      plate,
      type,
      color,
      brand,
      model,
      year,
      authorizationStatus,
      parkingSpaceIdentifier,
      parkingAllocationNotes,
      notes,
      residentSearchText,
      propertySearchText,
      contractSearchText);
    MarkUpdated(updatedAt, updatedByUserId);
  }

  public void Authorize(DateTimeOffset updatedAt, UserId? updatedByUserId)
  {
    EnsureCanMutate();
    AuthorizationStatus = VehicleAuthorizationStatus.Authorized;
    SearchText = BuildSearchText(null, null, null);
    MarkUpdated(updatedAt, updatedByUserId);
  }

  public void Deny(string? notes, DateTimeOffset updatedAt, UserId? updatedByUserId)
  {
    EnsureCanMutate();
    AuthorizationStatus = VehicleAuthorizationStatus.Denied;
    Notes = VehicleCode.Optional(notes, 2000, nameof(notes)) ?? Notes;
    SearchText = BuildSearchText(null, null, null);
    MarkUpdated(updatedAt, updatedByUserId);
  }

  public void Archive(DateTimeOffset deletedAt, UserId? deletedByUserId)
  {
    if (IsDeleted)
    {
      return;
    }

    AuthorizationStatus = VehicleAuthorizationStatus.Archived;
    SearchText = BuildSearchText(null, null, null);
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
    if (AuthorizationStatus == VehicleAuthorizationStatus.Archived)
    {
      AuthorizationStatus = VehicleAuthorizationStatus.Pending;
    }

    SearchText = BuildSearchText(null, null, null);
    MarkUpdated(restoredAt, restoredByUserId);
  }

  private void ApplyDetails(
    EntityId residentId,
    EntityId? propertyId,
    EntityId? contractId,
    string plate,
    VehicleType type,
    string? color,
    string? brand,
    string? model,
    int? year,
    VehicleAuthorizationStatus authorizationStatus,
    string? parkingSpaceIdentifier,
    string? parkingAllocationNotes,
    string? notes,
    string? residentSearchText,
    string? propertySearchText,
    string? contractSearchText)
  {
    var normalizedPlate = VehicleCode.NormalizePlate(plate);
    var normalizedParking = VehicleCode.NormalizeIdentifier(parkingSpaceIdentifier);

    if (normalizedPlate is null)
    {
      throw new ArgumentException("Vehicle plate must contain letters or digits.", nameof(plate));
    }

    if (!string.IsNullOrWhiteSpace(parkingSpaceIdentifier) && normalizedParking is null)
    {
      throw new ArgumentException("Parking identifier must contain letters or digits.", nameof(parkingSpaceIdentifier));
    }

    ResidentId = residentId;
    PropertyId = propertyId;
    ContractId = contractId;
    Plate = VehicleCode.Required(plate, 20, nameof(plate));
    NormalizedPlate = normalizedPlate;
    Type = type;
    Color = VehicleCode.Optional(color, 80, nameof(color));
    Brand = VehicleCode.Optional(brand, 120, nameof(brand));
    Model = VehicleCode.Optional(model, 120, nameof(model));
    Year = ValidateYear(year);
    AuthorizationStatus = RequireMutableStatus(authorizationStatus);
    ParkingSpaceIdentifier = VehicleCode.Optional(parkingSpaceIdentifier, 80, nameof(parkingSpaceIdentifier));
    NormalizedParkingSpaceIdentifier = normalizedParking;
    ParkingAllocationNotes = VehicleCode.Optional(parkingAllocationNotes, 500, nameof(parkingAllocationNotes));
    Notes = VehicleCode.Optional(notes, 2000, nameof(notes));
    SearchText = BuildSearchText(residentSearchText, propertySearchText, contractSearchText);
  }

  private string BuildSearchText(
    string? residentSearchText,
    string? propertySearchText,
    string? contractSearchText) =>
    VehicleCode.NormalizeSearchText(
      Id.Value.ToString("D"),
      ResidentId.Value.ToString("D"),
      PropertyId?.Value.ToString("D"),
      ContractId?.Value.ToString("D"),
      Plate,
      NormalizedPlate,
      Type.ToString(),
      Color,
      Brand,
      Model,
      Year?.ToString(CultureInfo.InvariantCulture),
      AuthorizationStatus.ToString(),
      ParkingSpaceIdentifier,
      NormalizedParkingSpaceIdentifier,
      ParkingAllocationNotes,
      Notes,
      residentSearchText,
      propertySearchText,
      contractSearchText);

  private void EnsureCanMutate()
  {
    if (IsDeleted || AuthorizationStatus == VehicleAuthorizationStatus.Archived)
    {
      throw new InvalidOperationException("Archived vehicles cannot be changed.");
    }
  }

  private static VehicleAuthorizationStatus RequireMutableStatus(VehicleAuthorizationStatus status) =>
    status == VehicleAuthorizationStatus.Archived
      ? throw new ArgumentException("Archived status is only set through archive.", nameof(status))
      : status;

  private static int? ValidateYear(int? year)
  {
    if (!year.HasValue)
    {
      return null;
    }

    return year.Value is < 1886 or > 9999
      ? throw new ArgumentOutOfRangeException(nameof(year), "Vehicle year is outside the supported range.")
      : year;
  }
}
