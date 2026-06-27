using Alsappan.Application.Common.Configuration;
using Alsappan.Application.Common.Authorization;
using Alsappan.Application.Occurrences;
using Alsappan.Domain.Common.Identifiers;
using Alsappan.Domain.Common.ValueObjects;
using Alsappan.Domain.Contracts;
using Alsappan.Domain.Documents;
using Alsappan.Domain.Identity;
using Alsappan.Domain.Occurrences;
using Alsappan.Domain.Properties;
using Alsappan.Domain.Residents;
using Alsappan.Infrastructure.Occurrences;
using Alsappan.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Alsappan.Infrastructure.Tests.Occurrences;

public sealed class EfOccurrenceRepositoryTests
{
  [Fact]
  public void ModelContainsOccurrenceOperationalIndexes()
  {
    using var context = CreateContext(OrganizationId.New(), Guid.NewGuid().ToString("N"));
    var entityType = context.Model.FindEntityType(typeof(Occurrence));

    Assert.NotNull(entityType);
    var indexes = entityType.GetIndexes()
      .Select(index => index.Properties.Select(property => property.Name).ToArray())
      .ToArray();

    Assert.Contains(indexes, properties => properties.SequenceEqual(["OrganizationId", "Status"]));
    Assert.Contains(indexes, properties => properties.SequenceEqual(["OrganizationId", "Priority"]));
    Assert.Contains(indexes, properties => properties.SequenceEqual(["OrganizationId", "AssignedUserId"]));
    Assert.Contains(indexes, properties => properties.SequenceEqual(["OrganizationId", "PropertyId"]));
    Assert.Contains(indexes, properties => properties.SequenceEqual(["OrganizationId", "ResidentId"]));
    Assert.Contains(indexes, properties => properties.SequenceEqual(["OrganizationId", "DueDate"]));
  }

  [Fact]
  public async Task ListAsyncFiltersByTenantStatusPriorityAssigneeRelatedEntitiesAndDates()
  {
    var databaseName = Guid.NewGuid().ToString("N");
    var organizationA = OrganizationId.New();
    var organizationB = OrganizationId.New();
    var userId = UserId.New();

    await using (var setup = CreateContext(organizationA, databaseName))
    {
      var relatedA = CreateRelatedRecords(organizationA);
      var relatedB = CreateRelatedRecords(organizationB);
      AddActiveMember(setup, organizationA, userId, "Bruno Operador");
      var document = CreateDocument(organizationA, relatedA.Property.Id);
      var visible = CreateOccurrence(
        organizationA,
        relatedA.Property.Id,
        relatedA.Resident.Id,
        relatedA.Contract.Id,
        userId,
        OccurrencePriority.Urgent,
        new DateOnly(2026, 7, 2));
      visible.AddComment("Morador confirmou acesso.", true, DateTimeOffset.UtcNow.AddMinutes(1), userId);
      visible.LinkDocument(document.Id, "Foto", DateTimeOffset.UtcNow.AddMinutes(2), userId);
      var resolved = CreateOccurrence(
        organizationA,
        relatedA.Property.Id,
        relatedA.Resident.Id,
        null,
        userId,
        OccurrencePriority.Urgent,
        new DateOnly(2026, 7, 2));
      resolved.Resolve("Concluido.", DateTimeOffset.UtcNow.AddMinutes(3), userId);
      var otherTenant = CreateOccurrence(
        organizationB,
        relatedB.Property.Id,
        relatedB.Resident.Id,
        relatedB.Contract.Id,
        userId,
        OccurrencePriority.Urgent,
        new DateOnly(2026, 7, 2));

      setup.Properties.AddRange(relatedA.Property, relatedB.Property);
      setup.Residents.AddRange(relatedA.Resident, relatedB.Resident);
      setup.Contracts.AddRange(relatedA.Contract, relatedB.Contract);
      setup.Documents.Add(document);
      setup.Occurrences.AddRange(visible, resolved, otherTenant);
      await setup.SaveChangesAsync();
    }

    await using var context = CreateContext(organizationA, databaseName);
    var repository = new EfOccurrenceRepository(context);

    var page = await repository.ListAsync(
      new OccurrenceListRequestDto(
        Search: "vazamento",
        Type: "maintenance",
        Priority: "urgent",
        Status: "assigned",
        AssignedUserId: userId.Value,
        PropertyId: context.Properties.Single().Id.Value,
        ResidentId: context.Residents.Single().Id.Value,
        DateFrom: new DateOnly(2026, 7, 1),
        DateTo: new DateOnly(2026, 7, 3),
        UnresolvedOnly: true),
      organizationA);

    var snapshot = Assert.Single(page.Items);
    Assert.Equal("Casa Calabria", snapshot.Property!.Name);
    Assert.Equal("Joao da Silva", snapshot.Resident!.Name);
    Assert.Equal("Contrato 2026-06 - Casa Calabria", snapshot.Contract!.DisplayName);
    Assert.Equal("Bruno Operador", snapshot.AssignedUser!.DisplayName);
    Assert.Single(snapshot.Comments);
    Assert.Single(snapshot.Attachments);
    Assert.NotEmpty(snapshot.StatusHistory);
    Assert.Equal(1, page.TotalItems);
  }

  [Fact]
  public async Task UserAndDocumentLookupsAreOrganizationScopedAndRequireActiveRecords()
  {
    var databaseName = Guid.NewGuid().ToString("N");
    var organizationA = OrganizationId.New();
    var organizationB = OrganizationId.New();
    var activeUserId = UserId.New();
    var inactiveUserId = UserId.New();
    var relatedA = CreateRelatedRecords(organizationA);
    var documentA = CreateDocument(organizationA, relatedA.Property.Id);
    var documentB = CreateDocument(organizationB, EntityId.New());

    await using (var setup = CreateContext(organizationA, databaseName))
    {
      AddActiveMember(setup, organizationA, activeUserId, "Bruno Operador");
      AddInactiveMember(setup, organizationA, inactiveUserId, "Usuario Inativo");
      setup.Documents.AddRange(documentA, documentB);
      await setup.SaveChangesAsync();
    }

    await using var context = CreateContext(organizationA, databaseName);
    var repository = new EfOccurrenceRepository(context);

    Assert.NotNull(await repository.GetAssignableUserSnapshotAsync(activeUserId, organizationA));
    Assert.Null(await repository.GetAssignableUserSnapshotAsync(inactiveUserId, organizationA));
    Assert.True(await repository.DocumentExistsAsync(documentA.Id, organizationA));
    Assert.False(await repository.DocumentExistsAsync(documentB.Id, organizationA));
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

  private static Occurrence CreateOccurrence(
    OrganizationId organizationId,
    EntityId propertyId,
    EntityId residentId,
    EntityId? contractId,
    UserId assignedUserId,
    OccurrencePriority priority,
    DateOnly dueDate) =>
    Occurrence.Create(
      EntityId.New(),
      organizationId,
      "Vazamento na cozinha",
      "Morador relatou vazamento recorrente.",
      OccurrenceType.Maintenance,
      priority,
      propertyId,
      residentId,
      contractId,
      assignedUserId,
      dueDate,
      "Casa Calabria",
      "Joao da Silva",
      "Contrato Casa Calabria",
      "Bruno Operador",
      DateTimeOffset.UtcNow,
      assignedUserId);

  private static DocumentRecord CreateDocument(OrganizationId organizationId, EntityId entityId) =>
    DocumentRecord.Create(
      EntityId.New(),
      organizationId,
      DocumentCategory.Occurrence,
      "Foto do vazamento",
      "Imagem enviada pelo morador",
      "foto.jpg",
      "image/jpeg",
      1024,
      $"documents/{Guid.NewGuid():N}.jpg",
      null,
      [new DocumentLinkDraft("occurrence", entityId, "Foto")],
      DateTimeOffset.UtcNow,
      null);

  private static void AddActiveMember(
    AlsappanDbContext context,
    OrganizationId organizationId,
    UserId userId,
    string displayName)
  {
    var now = DateTimeOffset.UtcNow;
    context.IdentityUsers.Add(IdentityUser.Create(
      userId,
      $"{userId.Value:N}@example.com",
      displayName,
      UserAccountType.Admin,
      now,
      status: UserStatus.Active));
    context.IdentityMemberships.Add(IdentityMembership.Create(
      EntityId.New(),
      organizationId,
      userId,
      [RoleCodes.OrganizationAdmin],
      now));
  }

  private static void AddInactiveMember(
    AlsappanDbContext context,
    OrganizationId organizationId,
    UserId userId,
    string displayName)
  {
    var now = DateTimeOffset.UtcNow;
    context.IdentityUsers.Add(IdentityUser.Create(
      userId,
      $"{userId.Value:N}@example.com",
      displayName,
      UserAccountType.Admin,
      now,
      status: UserStatus.Inactive));
    var membership = IdentityMembership.Create(
      EntityId.New(),
      organizationId,
      userId,
      [RoleCodes.OrganizationAdmin],
      now);
    membership.Deactivate(now.AddMinutes(1), null);
    context.IdentityMemberships.Add(membership);
  }

  private sealed record RelatedRecords(
    RentalProperty Property,
    Resident Resident,
    LeaseContract Contract);
}
