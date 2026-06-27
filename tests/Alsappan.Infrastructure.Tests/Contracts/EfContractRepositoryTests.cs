using Alsappan.Application.Common.Configuration;
using Alsappan.Application.Contracts;
using Alsappan.Domain.Common.Identifiers;
using Alsappan.Domain.Common.ValueObjects;
using Alsappan.Domain.Contracts;
using Alsappan.Domain.Properties;
using Alsappan.Domain.Residents;
using Alsappan.Infrastructure.Contracts;
using Alsappan.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Alsappan.Infrastructure.Tests.Contracts;

public sealed class EfContractRepositoryTests
{
  [Fact]
  public async Task ListAsyncFiltersByOrganizationSearchStatusResidentAndArchivedState()
  {
    var databaseName = Guid.NewGuid().ToString("N");
    var organizationA = OrganizationId.New();
    var organizationB = OrganizationId.New();
    var residentA = CreateResident(organizationA, "Joao da Silva");

    await using (var setup = CreateContext(organizationA, databaseName))
    {
      var propertyA = CreateProperty(organizationA, "Casa Calabria");
      var propertyB = CreateProperty(organizationB, "Casa Calabria outra");
      var residentB = CreateResident(organizationB, "Joao outra org");
      var archivedResident = CreateResident(organizationA, "Maria Arquivada");
      var visible = CreateContract(organizationA, propertyA, residentA);
      visible.Activate(DateTimeOffset.UtcNow, null);
      var otherTenant = CreateContract(organizationB, propertyB, residentB);
      otherTenant.Activate(DateTimeOffset.UtcNow, null);
      var archived = CreateContract(organizationA, propertyA, archivedResident);
      archived.Activate(DateTimeOffset.UtcNow, null);
      archived.Archive(DateTimeOffset.UtcNow.AddMinutes(1), null);

      setup.Properties.AddRange(propertyA, propertyB);
      setup.Residents.AddRange(residentA, residentB, archivedResident);
      setup.Contracts.AddRange(visible, otherTenant, archived);
      await setup.SaveChangesAsync();
    }

    await using var context = CreateContext(organizationA, databaseName);
    var repository = new EfContractRepository(context);

    var page = await repository.ListAsync(
      new ContractListRequestDto(Search: "calabria", Status: "active", ResidentId: residentA.Id.Value),
      organizationA,
      new DateOnly(2026, 7, 1));

    Assert.Single(page.Items);
    Assert.Equal("Casa Calabria", page.Items[0].Property.PropertyName);
    Assert.Equal("Joao da Silva", Assert.Single(page.Items[0].Residents).ResidentName);

    var includingArchived = await repository.ListAsync(
      new ContractListRequestDto(Search: "calabria", IncludeArchived: true),
      organizationA,
      new DateOnly(2026, 7, 1));
    Assert.Equal(2, includingArchived.TotalItems);
  }

  [Fact]
  public async Task HasOverlappingActiveContractAsyncMatchesOnlyActiveOverlappingContracts()
  {
    var organizationId = OrganizationId.New();
    await using var context = CreateContext(organizationId, Guid.NewGuid().ToString("N"));
    var property = CreateProperty(organizationId, "Casa Calabria");
    var resident = CreateResident(organizationId, "Joao da Silva");
    var active = CreateContract(organizationId, property, resident);
    active.Activate(DateTimeOffset.UtcNow, null);
    context.Properties.Add(property);
    context.Residents.Add(resident);
    context.Contracts.Add(active);
    await context.SaveChangesAsync();
    var repository = new EfContractRepository(context);

    Assert.True(await repository.HasOverlappingActiveContractAsync(
      property.Id,
      organizationId,
      new DateOnly(2026, 8, 1),
      new DateOnly(2026, 8, 31)));

    Assert.False(await repository.HasOverlappingActiveContractAsync(
      property.Id,
      organizationId,
      new DateOnly(2028, 1, 1),
      new DateOnly(2028, 12, 31)));

    Assert.False(await repository.HasOverlappingActiveContractAsync(
      property.Id,
      organizationId,
      new DateOnly(2026, 8, 1),
      new DateOnly(2026, 8, 31),
      active.Id));
  }

  [Fact]
  public async Task HasAnyActiveContractAsyncIgnoresArchivedAndIgnoredContract()
  {
    var organizationId = OrganizationId.New();
    await using var context = CreateContext(organizationId, Guid.NewGuid().ToString("N"));
    var property = CreateProperty(organizationId, "Casa Calabria");
    var firstResident = CreateResident(organizationId, "Joao da Silva");
    var secondResident = CreateResident(organizationId, "Maria Souza");
    var firstActive = CreateContract(organizationId, property, firstResident);
    var secondActive = CreateContract(organizationId, property, secondResident);
    firstActive.Activate(DateTimeOffset.UtcNow, null);
    secondActive.Activate(DateTimeOffset.UtcNow, null);
    context.Properties.Add(property);
    context.Residents.AddRange(firstResident, secondResident);
    context.Contracts.AddRange(firstActive, secondActive);
    await context.SaveChangesAsync();
    var repository = new EfContractRepository(context);

    Assert.True(await repository.HasAnyActiveContractAsync(property.Id, organizationId, firstActive.Id));

    secondActive.Archive(DateTimeOffset.UtcNow.AddMinutes(1), null);
    await context.SaveChangesAsync();

    Assert.False(await repository.HasAnyActiveContractAsync(property.Id, organizationId, firstActive.Id));
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

  private static RentalProperty CreateProperty(OrganizationId organizationId, string name) =>
    RentalProperty.Create(
      EntityId.New(),
      organizationId,
      name,
      PropertyType.House,
      "Para testes",
      new Address("Rua Calabria", "82", null, "Vila Fazzione", "Sao Paulo", "SP", "00000-000"),
      PropertyStatus.Available,
      new Money(2500m, "BRL"),
      1,
      "A1",
      null,
      DateTimeOffset.UtcNow);

  private static Resident CreateResident(OrganizationId organizationId, string fullName) =>
    Resident.Create(
      EntityId.New(),
      organizationId,
      fullName,
      null,
      "joao@example.com",
      "(11) 99999-8888",
      null,
      "CPF",
      "123.456.789-00",
      new DateOnly(1985, 1, 20),
      "Maria",
      "Mae",
      "(11) 98888-7777",
      ResidentStatus.Active,
      ResidentPortalStatus.NotInvited,
      ResidentPrivacyOptions.IdentificationData,
      "Observacoes",
      null,
      DateTimeOffset.UtcNow);

  private static LeaseContract CreateContract(
    OrganizationId organizationId,
    RentalProperty property,
    Resident primaryResident) =>
    LeaseContract.Create(
      EntityId.New(),
      organizationId,
      property.Id,
      primaryResident.Id,
      [primaryResident.Id],
      new DateOnly(2026, 7, 1),
      new DateOnly(2027, 6, 30),
      new Money(2500m, "BRL"),
      10,
      new Money(2500m, "BRL"),
      ContractAdjustmentIndex.Ipca,
      12,
      new DateOnly(2027, 7, 1),
      "Multa",
      "Desconto",
      true,
      "Observacoes",
      property.Name,
      primaryResident.FullName,
      DateTimeOffset.UtcNow);
}
