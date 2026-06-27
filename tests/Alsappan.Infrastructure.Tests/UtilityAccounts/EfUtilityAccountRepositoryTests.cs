using Alsappan.Application.Common.Configuration;
using Alsappan.Application.UtilityAccounts;
using Alsappan.Domain.Common.Identifiers;
using Alsappan.Domain.Common.ValueObjects;
using Alsappan.Domain.Contracts;
using Alsappan.Domain.Documents;
using Alsappan.Domain.Properties;
using Alsappan.Domain.Residents;
using Alsappan.Domain.UtilityAccounts;
using Alsappan.Infrastructure.Persistence;
using Alsappan.Infrastructure.UtilityAccounts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Alsappan.Infrastructure.Tests.UtilityAccounts;

public sealed class EfUtilityAccountRepositoryTests
{
  [Fact]
  public async Task ListAsyncFiltersByTenantSearchStatusAndBuildsRelationshipSnapshots()
  {
    var databaseName = Guid.NewGuid().ToString("N");
    var organizationA = OrganizationId.New();
    var organizationB = OrganizationId.New();
    var today = new DateOnly(2026, 6, 27);

    await using (var setup = CreateContext(organizationA, databaseName))
    {
      var related = CreateRelatedRecords(organizationA);
      var overdue = CreateAccount(organizationA, related.Contract, "Energia junho", new DateOnly(2026, 6, 20));
      var otherTenant = CreateAccount(
        organizationB,
        CreateRelatedRecords(organizationB).Contract,
        "Energia junho",
        today);

      setup.Properties.AddRange(related.Property);
      setup.Residents.AddRange(related.Resident);
      setup.Contracts.AddRange(related.Contract);
      setup.UtilityAccounts.AddRange(overdue, otherTenant);
      await setup.SaveChangesAsync();
    }

    await using var context = CreateContext(organizationA, databaseName);
    var repository = new EfUtilityAccountRepository(context);

    var page = await repository.ListAsync(
      new UtilityAccountListRequestDto(Search: "energia", Status: "overdue"),
      organizationA,
      today);

    Assert.Single(page.Items);
    Assert.Equal("Energia junho", page.Items[0].UtilityAccount.Title);
    Assert.NotNull(page.Items[0].Contract);
    Assert.Equal("Casa Calabria", page.Items[0].Property!.Name);
    Assert.Equal("Joao da Silva", page.Items[0].Resident!.Name);
  }

  [Fact]
  public async Task DocumentLookupAndSnapshotLinksStayTenantScoped()
  {
    var databaseName = Guid.NewGuid().ToString("N");
    var organizationId = OrganizationId.New();
    var related = CreateRelatedRecords(organizationId);
    var account = CreateAccount(organizationId, related.Contract, "Agua julho", new DateOnly(2026, 7, 10));
    var document = CreateDocument(organizationId);
    account.LinkDocument(document.Id, UtilityDocumentKind.Bill, "Conta", DateTimeOffset.UtcNow, null);

    await using (var setup = CreateContext(organizationId, databaseName))
    {
      setup.Properties.Add(related.Property);
      setup.Residents.Add(related.Resident);
      setup.Contracts.Add(related.Contract);
      setup.Documents.Add(document);
      setup.UtilityAccounts.Add(account);
      await setup.SaveChangesAsync();
    }

    await using var context = CreateContext(organizationId, databaseName);
    var repository = new EfUtilityAccountRepository(context);
    var snapshot = await repository.FindSnapshotAsync(account.Id, organizationId, includeArchived: true);

    Assert.True(await repository.DocumentExistsAsync(document.Id, organizationId));
    Assert.False(await repository.DocumentExistsAsync(document.Id, OrganizationId.New()));
    Assert.NotNull(snapshot);
    Assert.Equal(document.Id, Assert.Single(snapshot!.Documents).DocumentId);
  }

  [Fact]
  public async Task ArchivedStatusFilterUsesDeletedStateInsteadOfStoredLifecycleStatus()
  {
    var databaseName = Guid.NewGuid().ToString("N");
    var organizationId = OrganizationId.New();
    var related = CreateRelatedRecords(organizationId);
    var archived = CreateAccount(organizationId, related.Contract, "Energia arquivada", new DateOnly(2026, 7, 10));
    archived.Archive(DateTimeOffset.UtcNow, null);

    await using (var setup = CreateContext(organizationId, databaseName))
    {
      setup.Properties.Add(related.Property);
      setup.Residents.Add(related.Resident);
      setup.Contracts.Add(related.Contract);
      setup.UtilityAccounts.Add(archived);
      await setup.SaveChangesAsync();
    }

    await using var context = CreateContext(organizationId, databaseName);
    var repository = new EfUtilityAccountRepository(context);

    var archivedPage = await repository.ListAsync(
      new UtilityAccountListRequestDto(Status: "archived", IncludeArchived: true),
      organizationId,
      new DateOnly(2026, 6, 27));
    var openPage = await repository.ListAsync(
      new UtilityAccountListRequestDto(Status: "open", IncludeArchived: true),
      organizationId,
      new DateOnly(2026, 6, 27));

    Assert.Single(archivedPage.Items);
    Assert.Empty(openPage.Items);
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
    contract.Activate(DateTimeOffset.UtcNow.AddMinutes(1), null);
    return new RelatedRecords(property, resident, contract);
  }

  private static UtilityAccount CreateAccount(
    OrganizationId organizationId,
    LeaseContract contract,
    string title,
    DateOnly dueDate) =>
    UtilityAccount.Create(
      EntityId.New(),
      organizationId,
      contract.PropertyId,
      contract.Id,
      contract.PrimaryResidentId,
      UtilityAccountType.Electricity,
      UtilityResponsibility.Contract,
      title,
      "Conta de consumo",
      new DateOnly(2026, 6, 1),
      new DateOnly(2026, 6, 30),
      dueDate,
      new Money(300m, "BRL"),
      null,
      "Casa Calabria",
      "Contrato Casa Calabria",
      "Joao da Silva",
      DateTimeOffset.UtcNow);

  private static DocumentRecord CreateDocument(OrganizationId organizationId) =>
    DocumentRecord.Create(
      EntityId.New(),
      organizationId,
      DocumentCategory.UtilityAccount,
      "Conta",
      null,
      "conta.pdf",
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
