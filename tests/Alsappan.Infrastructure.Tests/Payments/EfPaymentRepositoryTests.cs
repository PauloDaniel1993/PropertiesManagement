using Alsappan.Application.Common.Configuration;
using Alsappan.Application.Payments;
using Alsappan.Domain.Common.Identifiers;
using Alsappan.Domain.Common.ValueObjects;
using Alsappan.Domain.Contracts;
using Alsappan.Domain.Documents;
using Alsappan.Domain.Payments;
using Alsappan.Domain.Properties;
using Alsappan.Domain.Residents;
using Alsappan.Infrastructure.Payments;
using Alsappan.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Alsappan.Infrastructure.Tests.Payments;

public sealed class EfPaymentRepositoryTests
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
      var overdue = CreateCharge(organizationA, related.Contract, "Aluguel junho", new DateOnly(2026, 6, 20));
      var otherTenant = CreateCharge(organizationB, CreateRelatedRecords(organizationB).Contract, "Aluguel junho", today);

      setup.Properties.AddRange(related.Property);
      setup.Residents.AddRange(related.Resident);
      setup.Contracts.AddRange(related.Contract);
      setup.PaymentCharges.AddRange(overdue, otherTenant);
      await setup.SaveChangesAsync();
    }

    await using var context = CreateContext(organizationA, databaseName);
    var repository = new EfPaymentRepository(context);

    var page = await repository.ListAsync(
      new PaymentListRequestDto(Search: "aluguel", Status: "overdue"),
      organizationA,
      today);

    Assert.Single(page.Items);
    Assert.Equal("Aluguel junho", page.Items[0].Charge.Title);
    Assert.NotNull(page.Items[0].Contract);
    Assert.Equal("Casa Calabria", page.Items[0].Property!.Name);
    Assert.Equal("Joao da Silva", page.Items[0].Resident!.Name);
  }

  [Fact]
  public async Task ReceiptAndProviderLookupsStayTenantScoped()
  {
    var databaseName = Guid.NewGuid().ToString("N");
    var organizationId = OrganizationId.New();
    var related = CreateRelatedRecords(organizationId);
    var charge = CreateCharge(organizationId, related.Contract, "Aluguel julho", new DateOnly(2026, 7, 10));
    var document = CreateDocument(organizationId);
    charge.SetProviderInstruction("mock-pix", "mock-pix-reference", "{}", DateTimeOffset.UtcNow, null);

    await using (var setup = CreateContext(organizationId, databaseName))
    {
      setup.Properties.Add(related.Property);
      setup.Residents.Add(related.Resident);
      setup.Contracts.Add(related.Contract);
      setup.Documents.Add(document);
      setup.PaymentCharges.Add(charge);
      await setup.SaveChangesAsync();
    }

    await using var context = CreateContext(organizationId, databaseName);
    var repository = new EfPaymentRepository(context);

    Assert.True(await repository.ReceiptDocumentExistsAsync(document.Id, organizationId));
    Assert.NotNull(await repository.FindByProviderReferenceAsync("mock-pix", "mock-pix-reference", organizationId));
    Assert.Null(await repository.FindByProviderReferenceAsync("mock-pix", "mock-pix-reference", OrganizationId.New()));
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

  private static PaymentCharge CreateCharge(
    OrganizationId organizationId,
    LeaseContract contract,
    string title,
    DateOnly dueDate) =>
    PaymentCharge.Create(
      EntityId.New(),
      organizationId,
      contract.Id,
      contract.PropertyId,
      contract.PrimaryResidentId,
      null,
      title,
      "Mensalidade",
      dueDate,
      new Money(1000m, "BRL"),
      null,
      null,
      PaymentMethod.Pix,
      PaymentReconciliationStatus.Pending,
      null,
      "Contrato Casa Calabria",
      "Casa Calabria",
      "Joao da Silva",
      DateTimeOffset.UtcNow);

  private static DocumentRecord CreateDocument(OrganizationId organizationId) =>
    DocumentRecord.Create(
      EntityId.New(),
      organizationId,
      DocumentCategory.PaymentReceipt,
      "Recibo",
      null,
      "recibo.pdf",
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
