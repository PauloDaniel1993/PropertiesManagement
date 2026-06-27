using Alsappan.Application.Common.Authorization;
using Alsappan.Application.Common.Configuration;
using Alsappan.Application.Inspections;
using Alsappan.Domain.Common.Identifiers;
using Alsappan.Domain.Common.ValueObjects;
using Alsappan.Domain.Contracts;
using Alsappan.Domain.Identity;
using Alsappan.Domain.Inspections;
using Alsappan.Domain.Properties;
using Alsappan.Domain.Residents;
using Alsappan.Infrastructure.Inspections;
using Alsappan.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Alsappan.Infrastructure.Tests.Inspections;

public sealed class EfInspectionRepositoryTests
{
  [Fact]
  public void ModelDefinesInspectionFilterIndexes()
  {
    using var context = CreateContext(OrganizationId.New(), Guid.NewGuid().ToString("N"));
    var entityType = context.Model.FindEntityType(typeof(Inspection));

    Assert.NotNull(entityType);
    Assert.Contains(entityType.GetIndexes(), index =>
      index.Properties.Select(property => property.Name).SequenceEqual(["OrganizationId", "PropertyId"]));
    Assert.Contains(entityType.GetIndexes(), index =>
      index.Properties.Select(property => property.Name).SequenceEqual(["OrganizationId", "ContractId"]));
    Assert.Contains(entityType.GetIndexes(), index =>
      index.Properties.Select(property => property.Name).SequenceEqual(["OrganizationId", "AssignedUserId"]));
    Assert.Contains(entityType.GetIndexes(), index =>
      index.Properties.Select(property => property.Name).SequenceEqual(["OrganizationId", "ScheduledAt"]));
    Assert.Contains(entityType.GetIndexes(), index =>
      index.Properties.Select(property => property.Name).SequenceEqual(["OrganizationId", "Status"]));
    Assert.Contains(entityType.GetIndexes(), index =>
      index.Properties.Select(property => property.Name).SequenceEqual(["OrganizationId", "DeletedAt"]));
  }

  [Fact]
  public async Task ListAsyncFiltersByTenantPendingStateAndBuildsRelationshipSnapshots()
  {
    var databaseName = Guid.NewGuid().ToString("N");
    var organizationA = OrganizationId.New();
    var organizationB = OrganizationId.New();
    var now = new DateTimeOffset(2026, 6, 27, 12, 0, 0, TimeSpan.Zero);

    await using (var setup = CreateContext(organizationA, databaseName))
    {
      var relatedA = CreateRelatedRecords(organizationA);
      var relatedB = CreateRelatedRecords(organizationB);
      var inspection = CreateInspection(
        organizationA,
        relatedA,
        "Vistoria de entrada",
        now.AddDays(1));
      var otherTenant = CreateInspection(
        organizationB,
        relatedB,
        "Vistoria de entrada",
        now.AddDays(1));

      setup.Properties.AddRange(relatedA.Property, relatedB.Property);
      setup.Residents.AddRange(relatedA.Resident, relatedB.Resident);
      setup.Contracts.AddRange(relatedA.Contract, relatedB.Contract);
      setup.IdentityUsers.AddRange(relatedA.User, relatedB.User);
      setup.IdentityMemberships.AddRange(relatedA.Membership, relatedB.Membership);
      setup.Inspections.AddRange(inspection, otherTenant);
      await setup.SaveChangesAsync();
    }

    await using var context = CreateContext(organizationA, databaseName);
    var repository = new EfInspectionRepository(context);
    var related = context.Contracts.IgnoreQueryFilters().Single(contract => contract.OrganizationId == organizationA);

    var page = await repository.ListAsync(
      new InspectionListRequestDto(
        Search: "entrada",
        Status: "scheduled",
        PropertyId: related.PropertyId.Value,
        ContractId: related.Id.Value,
        AssignedUserId: context.IdentityMemberships.IgnoreQueryFilters()
          .Single(membership => membership.OrganizationId == organizationA)
          .UserId.Value,
        ScheduledFrom: now,
        ScheduledTo: now.AddDays(7),
        PendingOnly: true),
      organizationA,
      now);

    var snapshot = Assert.Single(page.Items);
    Assert.Equal("Vistoria de entrada", snapshot.Inspection.Title);
    Assert.Equal("Casa Calabria", snapshot.Property.Name);
    Assert.Equal("Joao da Silva", snapshot.Resident!.Name);
    Assert.NotNull(snapshot.Contract);
    Assert.Equal("Ana Admin", snapshot.Assignee.Name);
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
    var user = IdentityUser.Create(
      UserId.New(),
      $"ana-{organizationId.Value:N}@example.com",
      "Ana Admin",
      UserAccountType.Admin,
      DateTimeOffset.UtcNow,
      status: UserStatus.Active);
    var membership = IdentityMembership.Create(
      EntityId.New(),
      organizationId,
      user.Id,
      [RoleCodes.OrganizationStaff],
      DateTimeOffset.UtcNow);

    contract.Activate(DateTimeOffset.UtcNow.AddMinutes(1), null);
    return new RelatedRecords(property, resident, contract, user, membership);
  }

  private static Inspection CreateInspection(
    OrganizationId organizationId,
    RelatedRecords related,
    string title,
    DateTimeOffset scheduledAt) =>
    Inspection.Create(
      EntityId.New(),
      organizationId,
      InspectionType.MoveIn,
      related.Property.Id,
      related.Contract.Id,
      related.Resident.Id,
      scheduledAt,
      related.User.Id,
      related.User.DisplayName,
      title,
      "Observacao",
      related.Property.Name,
      "Contrato Casa Calabria",
      related.Resident.FullName,
      DateTimeOffset.UtcNow,
      null);

  private sealed record RelatedRecords(
    RentalProperty Property,
    Resident Resident,
    LeaseContract Contract,
    IdentityUser User,
    IdentityMembership Membership);
}
