using Alsappan.Application.Common.Configuration;
using Alsappan.Application.Search.Repositories;
using Alsappan.Domain.Common.Identifiers;
using Alsappan.Domain.Common.ValueObjects;
using Alsappan.Domain.Contracts;
using Alsappan.Domain.Documents;
using Alsappan.Domain.Inspections;
using Alsappan.Domain.Occurrences;
using Alsappan.Domain.Payments;
using Alsappan.Domain.Properties;
using Alsappan.Domain.Residents;
using Alsappan.Infrastructure.Persistence;
using Alsappan.Infrastructure.Search;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Alsappan.Infrastructure.Tests.Search;

public sealed class EfGlobalSearchRepositoryTests
{
  [Fact]
  public void ModelDefinesSearchTextIndexesForGlobalSearchEntities()
  {
    using var context = CreateContext(OrganizationId.New(), Guid.NewGuid().ToString("N"));

    AssertSearchIndex<RentalProperty>(context);
    AssertSearchIndex<Resident>(context);
    AssertSearchIndex<LeaseContract>(context);
    AssertSearchIndex<PaymentCharge>(context);
    AssertSearchIndex<DocumentRecord>(context);
    AssertSearchIndex<Occurrence>(context);
    AssertSearchIndex<Inspection>(context);
  }

  [Fact]
  public async Task SearchAsyncFindsReadableRecordsAcrossGlobalEntityTypes()
  {
    var databaseName = Guid.NewGuid().ToString("N");
    var organizationA = OrganizationId.New();
    var organizationB = OrganizationId.New();
    var now = new DateTimeOffset(2026, 6, 27, 12, 0, 0, TimeSpan.Zero);

    await using (var setup = CreateContext(organizationA, databaseName))
    {
      var relatedA = CreateRelatedRecords(organizationA, now);
      var relatedB = CreateRelatedRecords(organizationB, now);

      setup.Properties.AddRange(relatedA.Property, relatedB.Property);
      setup.Residents.AddRange(relatedA.Resident, relatedB.Resident);
      setup.Contracts.AddRange(relatedA.Contract, relatedB.Contract);
      setup.PaymentCharges.Add(CreateCharge(organizationA, relatedA.Contract, now));
      setup.Documents.Add(CreateDocument(organizationA, relatedA.Property.Id, now));
      setup.Occurrences.Add(CreateOccurrence(organizationA, relatedA, now));
      setup.Inspections.Add(CreateInspection(organizationA, relatedA, now.AddDays(1), now));
      await setup.SaveChangesAsync();
    }

    await using var context = CreateContext(organizationA, databaseName);
    var repository = new EfGlobalSearchRepository(context);

    var allResults = await repository.SearchAsync(
      organizationA,
      new GlobalSearchQuery("calabria", 5, null));

    Assert.Equal(
      ["property", "resident", "contract", "payment", "document", "occurrence", "inspection"],
      allResults.Select(result => result.EntityType).ToArray());

    var propertyOnly = await repository.SearchAsync(
      organizationA,
      new GlobalSearchQuery(
        "calabria",
        5,
        new HashSet<string>(["property"], StringComparer.OrdinalIgnoreCase)));

    var result = Assert.Single(propertyOnly);
    Assert.Equal("property", result.EntityType);
    Assert.Equal("Casa Calabria", result.Label);
  }

  private static void AssertSearchIndex<TEntity>(AlsappanDbContext context)
  {
    var entityType = context.Model.FindEntityType(typeof(TEntity));

    Assert.NotNull(entityType);
    Assert.Contains(entityType.GetIndexes(), index =>
      index.Properties.Select(property => property.Name).SequenceEqual(["OrganizationId", "SearchText"]));
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

  private static RelatedRecords CreateRelatedRecords(OrganizationId organizationId, DateTimeOffset now)
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
      now);
    var resident = Resident.Create(
      EntityId.New(),
      organizationId,
      "Joao Calabria",
      null,
      "joao.calabria@example.com",
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
      now);
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
      now);
    contract.Activate(now.AddMinutes(1), null);
    return new RelatedRecords(property, resident, contract);
  }

  private static PaymentCharge CreateCharge(
    OrganizationId organizationId,
    LeaseContract contract,
    DateTimeOffset now) =>
    PaymentCharge.Create(
      EntityId.New(),
      organizationId,
      contract.Id,
      contract.PropertyId,
      contract.PrimaryResidentId,
      null,
      "Aluguel Calabria",
      "Mensalidade",
      new DateOnly(2026, 6, 20),
      new Money(1000m, "BRL"),
      null,
      null,
      PaymentMethod.Pix,
      PaymentReconciliationStatus.Pending,
      null,
      "Contrato Casa Calabria",
      "Casa Calabria",
      "Joao Calabria",
      now);

  private static DocumentRecord CreateDocument(
    OrganizationId organizationId,
    EntityId propertyId,
    DateTimeOffset now) =>
    DocumentRecord.Create(
      EntityId.New(),
      organizationId,
      DocumentCategory.Property,
      "Documento Calabria",
      "Contrato e anexos",
      "documento-calabria.pdf",
      "application/pdf",
      128,
      $"documents/{Guid.NewGuid():N}.pdf",
      null,
      [new DocumentLinkDraft("property", propertyId, "Casa Calabria")],
      now);

  private static Occurrence CreateOccurrence(
    OrganizationId organizationId,
    RelatedRecords related,
    DateTimeOffset now) =>
    Occurrence.Create(
      EntityId.New(),
      organizationId,
      "Ocorrencia Calabria",
      "Vazamento recorrente",
      OccurrenceType.Maintenance,
      OccurrencePriority.Medium,
      related.Property.Id,
      related.Resident.Id,
      related.Contract.Id,
      null,
      null,
      related.Property.Name,
      related.Resident.FullName,
      "Contrato Casa Calabria",
      null,
      now,
      UserId.New());

  private static Inspection CreateInspection(
    OrganizationId organizationId,
    RelatedRecords related,
    DateTimeOffset scheduledAt,
    DateTimeOffset now) =>
    Inspection.Create(
      EntityId.New(),
      organizationId,
      InspectionType.MoveIn,
      related.Property.Id,
      related.Contract.Id,
      related.Resident.Id,
      scheduledAt,
      UserId.New(),
      "Ana Admin",
      "Vistoria Calabria",
      "Observacao",
      related.Property.Name,
      "Contrato Casa Calabria",
      related.Resident.FullName,
      now,
      null);

  private sealed record RelatedRecords(
    RentalProperty Property,
    Resident Resident,
    LeaseContract Contract);
}
