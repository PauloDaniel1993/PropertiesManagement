using Alsappan.Application.Common.Configuration;
using Alsappan.Application.Pets;
using Alsappan.Domain.Common.Identifiers;
using Alsappan.Domain.Common.ValueObjects;
using Alsappan.Domain.Contracts;
using Alsappan.Domain.Documents;
using Alsappan.Domain.Pets;
using Alsappan.Domain.Properties;
using Alsappan.Domain.Residents;
using Alsappan.Infrastructure.Persistence;
using Alsappan.Infrastructure.Pets;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Alsappan.Infrastructure.Tests.Pets;

public sealed class EfPetRepositoryTests
{
  [Fact]
  public async Task ListAsyncFiltersByTenantSearchSpeciesStatusAndBuildsRelationshipSnapshots()
  {
    var databaseName = Guid.NewGuid().ToString("N");
    var organizationA = OrganizationId.New();
    var organizationB = OrganizationId.New();

    await using (var setup = CreateContext(organizationA, databaseName))
    {
      var related = CreateRelatedRecords(organizationA);
      var pet = CreatePet(organizationA, related.Contract, "Luna");
      var otherTenant = CreatePet(organizationB, CreateRelatedRecords(organizationB).Contract, "Luna");

      setup.Properties.Add(related.Property);
      setup.Residents.Add(related.Resident);
      setup.Contracts.Add(related.Contract);
      setup.Pets.AddRange(pet, otherTenant);
      await setup.SaveChangesAsync();
    }

    await using var context = CreateContext(organizationA, databaseName);
    var repository = new EfPetRepository(context);

    var page = await repository.ListAsync(
      new PetListRequestDto(Search: "luna", Species: "cat", AuthorizationStatus: "pending"),
      organizationA);

    Assert.Single(page.Items);
    Assert.Equal("Luna", page.Items[0].Pet.Name);
    Assert.Equal("Casa Calabria", page.Items[0].Property!.Name);
    Assert.Equal("Joao da Silva", page.Items[0].Resident.Name);
    Assert.NotNull(page.Items[0].Contract);
  }

  [Fact]
  public async Task DocumentLookupAndSnapshotLinksStayTenantScoped()
  {
    var databaseName = Guid.NewGuid().ToString("N");
    var organizationId = OrganizationId.New();
    var related = CreateRelatedRecords(organizationId);
    var pet = CreatePet(organizationId, related.Contract, "Luna");
    var document = CreateDocument(organizationId);
    pet.LinkDocument(document.Id, PetDocumentKind.VaccinationRecord, "Carteira", DateTimeOffset.UtcNow, null);

    await using (var setup = CreateContext(organizationId, databaseName))
    {
      setup.Properties.Add(related.Property);
      setup.Residents.Add(related.Resident);
      setup.Contracts.Add(related.Contract);
      setup.Documents.Add(document);
      setup.Pets.Add(pet);
      await setup.SaveChangesAsync();
    }

    await using var context = CreateContext(organizationId, databaseName);
    var repository = new EfPetRepository(context);
    var snapshot = await repository.FindSnapshotAsync(pet.Id, organizationId, includeArchived: true);

    Assert.True(await repository.DocumentExistsAsync(document.Id, organizationId));
    Assert.False(await repository.DocumentExistsAsync(document.Id, OrganizationId.New()));
    Assert.NotNull(snapshot);
    Assert.Equal(document.Id, Assert.Single(snapshot!.Documents).DocumentId);
  }

  [Fact]
  public async Task ActiveContractOnlyReturnsOnlyPetsLinkedToActiveContracts()
  {
    var databaseName = Guid.NewGuid().ToString("N");
    var organizationId = OrganizationId.New();
    var active = CreateRelatedRecords(organizationId);
    var draft = CreateRelatedRecords(organizationId, activateContract: false);
    var activePet = CreatePet(organizationId, active.Contract, "Luna");
    var draftPet = CreatePet(organizationId, draft.Contract, "Thor");

    await using (var setup = CreateContext(organizationId, databaseName))
    {
      setup.Properties.AddRange(active.Property, draft.Property);
      setup.Residents.AddRange(active.Resident, draft.Resident);
      setup.Contracts.AddRange(active.Contract, draft.Contract);
      setup.Pets.AddRange(activePet, draftPet);
      await setup.SaveChangesAsync();
    }

    await using var context = CreateContext(organizationId, databaseName);
    var repository = new EfPetRepository(context);

    var page = await repository.ListAsync(
      new PetListRequestDto(ActiveContractOnly: true),
      organizationId);

    var item = Assert.Single(page.Items);
    Assert.Equal(activePet.Id, item.Pet.Id);
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

  private static RelatedRecords CreateRelatedRecords(
    OrganizationId organizationId,
    bool activateContract = true)
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
      1,
      "A1",
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
    if (activateContract)
    {
      contract.Activate(DateTimeOffset.UtcNow.AddMinutes(1), null);
    }

    return new RelatedRecords(property, resident, contract);
  }

  private static Pet CreatePet(
    OrganizationId organizationId,
    LeaseContract contract,
    string name) =>
    Pet.Create(
      EntityId.New(),
      organizationId,
      contract.PrimaryResidentId,
      contract.PropertyId,
      contract.Id,
      name,
      PetSpecies.Cat,
      "SRD",
      PetAuthorizationStatus.Pending,
      null,
      "Docil",
      "Joao da Silva",
      "Casa Calabria",
      "Contrato Casa Calabria",
      DateTimeOffset.UtcNow);

  private static DocumentRecord CreateDocument(OrganizationId organizationId) =>
    DocumentRecord.Create(
      EntityId.New(),
      organizationId,
      DocumentCategory.Pet,
      "Carteira de vacinacao",
      null,
      "vacina.pdf",
      "application/pdf",
      128,
      $"documents/{Guid.NewGuid():N}.pdf",
      null,
      [],
      DateTimeOffset.UtcNow);

  private sealed record RelatedRecords(
    RentalProperty Property,
    Resident Resident,
    LeaseContract Contract);
}
