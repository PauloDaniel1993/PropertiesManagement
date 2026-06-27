using Alsappan.Domain.Common.Identifiers;
using Alsappan.Domain.Vehicles;

namespace Alsappan.Domain.Tests.Vehicles;

public sealed class VehicleTests
{
  [Fact]
  public void CreateNormalizesPlateAndParkingIdentifiers()
  {
    var vehicle = CreateVehicle("áb-c 1234", "vaga-á1");

    Assert.Equal("ABC1234", vehicle.NormalizedPlate);
    Assert.Equal("VAGAA1", vehicle.NormalizedParkingSpaceIdentifier);
    Assert.Contains("ABC1234", vehicle.SearchText, StringComparison.Ordinal);
    Assert.True(vehicle.HasParkingAllocation);
  }

  [Fact]
  public void AuthorizationLifecycleArchivesAndRestoresVehicle()
  {
    var vehicle = CreateVehicle("ABC-1234", "A1");

    vehicle.Authorize(DateTimeOffset.UtcNow.AddMinutes(1), null);
    Assert.Equal(VehicleAuthorizationStatus.Authorized, vehicle.AuthorizationStatus);

    vehicle.Deny("Documento recusado", DateTimeOffset.UtcNow.AddMinutes(2), null);
    Assert.Equal(VehicleAuthorizationStatus.Denied, vehicle.AuthorizationStatus);
    Assert.Equal("Documento recusado", vehicle.Notes);

    vehicle.Archive(DateTimeOffset.UtcNow.AddMinutes(3), null);
    Assert.True(vehicle.IsDeleted);
    Assert.Equal(VehicleAuthorizationStatus.Archived, vehicle.AuthorizationStatus);

    vehicle.Restore(DateTimeOffset.UtcNow.AddMinutes(4), null);
    Assert.False(vehicle.IsDeleted);
    Assert.Equal(VehicleAuthorizationStatus.Pending, vehicle.AuthorizationStatus);
  }

  [Fact]
  public void CreateRejectsPlateWithoutLettersOrDigits()
  {
    Assert.Throws<ArgumentException>(() => CreateVehicle("---", null));
  }

  private static Vehicle CreateVehicle(string plate, string? parkingSpaceIdentifier) =>
    Vehicle.Create(
      EntityId.New(),
      OrganizationId.New(),
      EntityId.New(),
      EntityId.New(),
      null,
      plate,
      VehicleType.Car,
      "Prata",
      "Honda",
      "Civic",
      2024,
      VehicleAuthorizationStatus.Pending,
      parkingSpaceIdentifier,
      "Vaga vinculada ao contrato",
      "Observacao",
      "Joao da Silva",
      "Casa Calabria",
      null,
      DateTimeOffset.UtcNow);
}
