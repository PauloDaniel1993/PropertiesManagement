using Alsappan.Application.Common.Configuration;
using Alsappan.Application.Timeline;
using Alsappan.Domain.Common.Events;
using Alsappan.Domain.Common.Identifiers;
using Alsappan.Infrastructure.Persistence;
using Alsappan.Infrastructure.Timeline;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Alsappan.Infrastructure.Tests.Timeline;

public sealed class EfTimelineRepositoryTests
{
  [Fact]
  public async Task ListAsyncFiltersByOrganizationEntityEventActorAndDate()
  {
    var databaseName = Guid.NewGuid().ToString("N");
    var organizationA = OrganizationId.New();
    var organizationB = OrganizationId.New();
    var actor = UserId.New();
    var occurredAt = new DateTimeOffset(2026, 6, 27, 10, 0, 0, TimeSpan.Zero);

    await using (var setup = CreateContext(organizationA, databaseName))
    {
      setup.TimelineEntries.AddRange(
        CreateEntry(organizationA, "property.created", "property", "property-a", "Casa A", occurredAt, actor),
        CreateEntry(organizationA, "resident.created", "resident", "resident-a", "Joao", occurredAt, actor),
        CreateEntry(organizationB, "property.created", "property", "property-b", "Casa B", occurredAt, actor));
      await setup.SaveChangesAsync();
    }

    await using var context = CreateContext(organizationA, databaseName);
    var repository = new EfTimelineRepository(context);

    var page = await repository.ListAsync(
      new TimelineListRequestDto(
        EntityType: "property",
        EventType: "property.created",
        ActorUserId: actor.Value,
        From: new DateOnly(2026, 6, 27),
        To: new DateOnly(2026, 6, 27)),
      organizationA,
      null);

    var item = Assert.Single(page.Items);
    Assert.Equal("property-a", item.Subject.EntityId);
    Assert.Equal(1, page.TotalItems);
  }

  [Fact]
  public async Task ListEntityAsyncIncludesRelatedEntitiesAndAppliesReadableSubjectFilter()
  {
    var databaseName = Guid.NewGuid().ToString("N");
    var organizationId = OrganizationId.New();
    var occurredAt = new DateTimeOffset(2026, 6, 27, 10, 0, 0, TimeSpan.Zero);
    var propertyId = Guid.NewGuid().ToString("D");

    await using (var setup = CreateContext(organizationId, databaseName))
    {
      setup.TimelineEntries.AddRange(
        CreateEntry(
          organizationId,
          "payment.received",
          "payment",
          "payment-a",
          "Boleto junho",
          occurredAt,
          UserId.New(),
          [new EntityReference("property", propertyId, "Casa A")]),
        CreateEntry(
          organizationId,
          "resident.created",
          "resident",
          "resident-a",
          "Joao",
          occurredAt.AddMinutes(-5),
          UserId.New(),
          [new EntityReference("property", propertyId, "Casa A")]));
      await setup.SaveChangesAsync();
    }

    await using var context = CreateContext(organizationId, databaseName);
    var repository = new EfTimelineRepository(context);

    var page = await repository.ListEntityAsync(
      "property",
      propertyId.ToUpperInvariant(),
      new TimelineEntityListRequestDto(),
      organizationId,
      new HashSet<string>(["payment", "property"], StringComparer.OrdinalIgnoreCase));

    var item = Assert.Single(page.Items);
    Assert.Equal("payment.received", item.EventType);
    Assert.Equal("Boleto junho", item.Subject.DisplayName);
    Assert.Equal(propertyId, Assert.Single(item.RelatedEntities).EntityId);
  }

  [Fact]
  public async Task ListAsyncFiltersByRelatedEntity()
  {
    var databaseName = Guid.NewGuid().ToString("N");
    var organizationId = OrganizationId.New();
    var occurredAt = new DateTimeOffset(2026, 6, 27, 10, 0, 0, TimeSpan.Zero);

    await using (var setup = CreateContext(organizationId, databaseName))
    {
      setup.TimelineEntries.AddRange(
        CreateEntry(
          organizationId,
          "property.updated",
          "property",
          "property-a",
          "Casa A",
          occurredAt,
          UserId.New(),
          [new EntityReference("resident", "resident-a", "Joao")]),
        CreateEntry(
          organizationId,
          "property.updated",
          "property",
          "property-b",
          "Casa B",
          occurredAt.AddMinutes(-1),
          UserId.New(),
          [new EntityReference("resident", "resident-b", "Maria")]));
      await setup.SaveChangesAsync();
    }

    await using var context = CreateContext(organizationId, databaseName);
    var repository = new EfTimelineRepository(context);

    var page = await repository.ListAsync(
      new TimelineListRequestDto(RelatedEntityType: "resident", RelatedEntityId: "resident-a"),
      organizationId,
      null);

    var item = Assert.Single(page.Items);
    Assert.Equal("property-a", item.Subject.EntityId);
  }

  [Fact]
  public async Task ListAsyncRemovesUnreadableRelatedEntitiesAndIgnoresHiddenRelatedFilter()
  {
    var databaseName = Guid.NewGuid().ToString("N");
    var organizationId = OrganizationId.New();
    var occurredAt = new DateTimeOffset(2026, 6, 27, 10, 0, 0, TimeSpan.Zero);

    await using (var setup = CreateContext(organizationId, databaseName))
    {
      setup.TimelineEntries.Add(
        CreateEntry(
          organizationId,
          "payment.received",
          "payment",
          "payment-a",
          "Boleto junho",
          occurredAt,
          UserId.New(),
          [new EntityReference("property", "property-a", "Casa A")]));
      await setup.SaveChangesAsync();
    }

    await using var context = CreateContext(organizationId, databaseName);
    var repository = new EfTimelineRepository(context);
    var readableTypes = new HashSet<string>(["payment"], StringComparer.OrdinalIgnoreCase);

    var visiblePage = await repository.ListAsync(
      new TimelineListRequestDto(),
      organizationId,
      readableTypes);
    var visibleItem = Assert.Single(visiblePage.Items);
    Assert.Equal("payment-a", visibleItem.Subject.EntityId);
    Assert.Empty(visibleItem.RelatedEntities);

    var hiddenFilterPage = await repository.ListAsync(
      new TimelineListRequestDto(RelatedEntityId: "property-a"),
      organizationId,
      readableTypes);
    Assert.Empty(hiddenFilterPage.Items);
    Assert.Equal(0, hiddenFilterPage.TotalItems);
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

  private static TimelineEntry CreateEntry(
    OrganizationId organizationId,
    string eventType,
    string entityType,
    string entityId,
    string displayName,
    DateTimeOffset occurredAt,
    UserId actorUserId,
    IReadOnlyCollection<EntityReference>? relatedEntities = null) =>
    TimelineEntry.FromEnvelope(
      ModuleEventEnvelope.Create(
        organizationId,
        $"{entityType}s",
        eventType,
        occurredAt,
        EventActor.User(actorUserId, "Ana Admin"),
        new EntityReference(entityType, entityId, displayName),
        ModuleEventConsumer.Timeline,
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
          ["source"] = "test"
        },
        relatedEntities),
      occurredAt.AddSeconds(1));
}
