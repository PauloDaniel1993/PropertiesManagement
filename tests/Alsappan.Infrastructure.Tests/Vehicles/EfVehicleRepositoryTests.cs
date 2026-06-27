using Alsappan.Application.Common.Configuration;
using Alsappan.Application.Vehicles;
using Alsappan.Domain.Common.Identifiers;
using Alsappan.Domain.Common.ValueObjects;
using Alsappan.Domain.Contracts;
using Alsappan.Domain.Properties;
using Alsappan.Domain.Residents;
using Alsappan.Domain.Vehicles;
using Alsappan.Infrastructure.Persistence;
using Alsappan.Infrastructure.Vehicles;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Alsappan.Infrastructure.Tests.Vehicles;

public sealed class EfVehicleRepositoryTests
{
  [Fact]
  public async Task ListAsyncFiltersByTenantNormalizedPlateAndBuildsRelationshipSnapshots()
  {
    var databaseName = Guid.NewGuid().ToString("N");
    var organizationA = OrganizationId.New();
    var organizationB = OrganizationId.New();

    await using (var setup = CreateContext(organizationA, databaseName))
    {
      var relatedA = CreateRelatedRecords(organizationA);
      var relatedB = CreateRelatedRecords(organizationB);
      var vehicle = CreateVehicle(
        organizationA,
        relatedA.Property.Id,
        relatedA.Resident.Id,
        relatedA.Contract.Id,
        "áb-c 1234",
        "A1",
        VehicleAuthorizationStatus.Pending);
      var otherTenant = CreateVehicle(
        organizationB,
        relatedB.Property.Id,
        relatedB.Resident.Id,
        relatedB.Contract.Id,
        "ABC-1234",
        "A1",
        VehicleAuthorizationStatus.Pending);

      setup.Properties.AddRange(relatedA.Property, relatedB.Property);
      setup.Residents.AddRange(relatedA.Resident, relatedB.Resident);
      setup.Contracts.AddRange(relatedA.Contract, relatedB.Contract);
      setup.Vehicles.AddRange(vehicle, otherTenant);
      await setup.SaveChangesAsync();
    }

    await using var context = CreateContext(organizationA, databaseName);
    var repository = new EfVehicleRepository(context);

    var page = await repository.ListAsync(
      new VehicleListRequestDto(Plate: "abc-1234", HasParkingAllocation: true),
      organizationA);

    Assert.Single(page.Items);
    Assert.Equal("ABC1234", page.Items[0].Vehicle.NormalizedPlate);
    Assert.Equal("Casa Calabria", page.Items[0].Property!.Name);
    Assert.Equal("Joao da Silva", page.Items[0].Resident.Name);
    Assert.NotNull(page.Items[0].Contract);
  }

  [Fact]
  public async Task HasActiveParkingAllocationIsTenantScopedAndIgnoresInactiveStatuses()
  {
    var databaseName = Guid.NewGuid().ToString("N");
    var organizationA = OrganizationId.New();
    var organizationB = OrganizationId.New();
    var relatedA = CreateRelatedRecords(organizationA);
    var relatedB = CreateRelatedRecords(organizationB);
    var pending = CreateVehicle(
      organizationA,
      relatedA.Property.Id,
      relatedA.Resident.Id,
      relatedA.Contract.Id,
      "ABC-1234",
      "A1",
      VehicleAuthorizationStatus.Pending);
    var denied = CreateVehicle(
      organizationA,
      relatedA.Property.Id,
      relatedA.Resident.Id,
      relatedA.Contract.Id,
      "XYZ-9876",
      "B2",
      VehicleAuthorizationStatus.Denied);
    var otherTenant = CreateVehicle(
      organizationB,
      relatedB.Property.Id,
      relatedB.Resident.Id,
      relatedB.Contract.Id,
      "DEF-4567",
      "A1",
      VehicleAuthorizationStatus.Pending);

    await using (var setup = CreateContext(organizationA, databaseName))
    {
      setup.Properties.AddRange(relatedA.Property, relatedB.Property);
      setup.Residents.AddRange(relatedA.Resident, relatedB.Resident);
      setup.Contracts.AddRange(relatedA.Contract, relatedB.Contract);
      setup.Vehicles.AddRange(pending, denied, otherTenant);
      await setup.SaveChangesAsync();
    }

    await using var context = CreateContext(organizationA, databaseName);
    var repository = new EfVehicleRepository(context);

    Assert.True(await repository.HasActiveParkingAllocationAsync(organizationA, relatedA.Property.Id, "A1"));
    Assert.False(await repository.HasActiveParkingAllocationAsync(
      organizationA,
      relatedA.Property.Id,
      "A1",
      pending.Id));
    Assert.False(await repository.HasActiveParkingAllocationAsync(organizationA, relatedA.Property.Id, "B2"));
    Assert.False(await repository.HasActiveParkingAllocationAsync(organizationB, relatedA.Property.Id, "A1"));
  }

  private static AlsappanDbContext CreateContext(OrganizationId organizationId, string databaseName)
  {
    var options = new DbContextOptionsBuilder<AlsappanDbContext>()
      .UseInMemoryDatabase(databaseName)
      .ReplaceService<IModelCacheKeyFactory, AlsappanModelCacheKeyFactory>()
      .Options;

    return new AlsappanDbContext(
      options,
      new StaticActiveOrganizationContext(organizationId),
      new DatabaseOptions());
  }

  private static RelatedRecords CreateRelatedRecords(OrganizationId organizationId)
  {
    var property = RentalProperty.Create(
      EntityId.New(),
      organizationId,
      "Casa Calabria",
      PropertyType.House,
      "Para testes",
      new Address("Rua Calabria", "82", null, "Vila Fazzione", "Sao Paulo", "SP", "00000-000"),
      PropertyStatus.Rented,
      new Money(1000m, "BRL"),
      2,
      "A1;B2",
      null,
      DateTimeOffset.UtcNow);
    var resident = Resident.Create(
      EntityId.New(),
      organizationId,
      "Joao da Silva",
      null,
      "joao@example.com",
      "11999999999",
      null,
      "cpf",
      "12345678900",
      null,
      null,
      null,
      null,
      ResidentStatus.Active,
      ResidentPortalStatus.NotInvited,
      ResidentPrivacyOptions.None,
      null,
      null,
      DateTimeOffset.UtcNow);
    var contract = LeaseContract.Create(
      EntityId.New(),
      organizationId,
      property.Id,
      resident.Id,
      [resident.Id],
      new DateOnly(2026, 6, 1),
      new DateOnly(2027, 5, 31),
      new Money(1000m, "BRL"),
      10,
      null,
      ContractAdjustmentIndex.Ipca,
      12,
      null,
      null,
      null,
      true,
      null,
      property.Name,
      resident.FullName,
      DateTimeOffset.UtcNow);
    contract.Activate(DateTimeOffset.UtcNow.AddMinutes(1), null);
    return new RelatedRecords(property, resident, contract);
  }

  private static Vehicle CreateVehicle(
    OrganizationId organizationId,
    EntityId propertyId,
    EntityId residentId,
    EntityId contractId,
    string plate,
    string parkingSpaceIdentifier,
    VehicleAuthorizationStatus status) =>
    Vehicle.Create(
      EntityId.New(),
      organizationId,
      residentId,
      propertyId,
      contractId,
      plate,
      VehicleType.Car,
      "Prata",
      "Honda",
      "Civic",
      2024,
      status,
      parkingSpaceIdentifier,
      null,
      null,
      "Joao da Silva",
      "Casa Calabria",
      "Contrato Casa Calabria",
      DateTimeOffset.UtcNow);

  private sealed record RelatedRecords(
    RentalProperty Property,
    Resident Resident,
    LeaseContract Contract);
}
