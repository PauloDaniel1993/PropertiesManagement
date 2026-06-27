using Alsappan.Application.Common.Configuration;
using Alsappan.Application.Notifications;
using Alsappan.Application.Notifications.Repositories;
using Alsappan.Domain.Common.Events;
using Alsappan.Domain.Common.Identifiers;
using Alsappan.Infrastructure.Notifications;
using Alsappan.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Alsappan.Infrastructure.Tests.Notifications;

public sealed class EfNotificationRepositoryTests
{
  [Fact]
  public async Task ListAsyncFiltersByOrganizationCategoryReadStateAndArchivedRows()
  {
    var databaseName = Guid.NewGuid().ToString("N");
    var organizationA = OrganizationId.New();
    var organizationB = OrganizationId.New();

    await using (var setup = CreateContext(organizationA, databaseName))
    {
      var visible = CreateNotification(organizationA, "payments", "payment.overdue", "payment-a", "Aluguel junho");
      var read = CreateNotification(organizationA, "payments", "payment.overdue", "payment-b", "Aluguel maio");
      read.MarkRead(DateTimeOffset.UtcNow);
      var otherCategory = CreateNotification(organizationA, "properties", "property.updated", "property-a", "Casa");
      var otherTenant = CreateNotification(organizationB, "payments", "payment.overdue", "payment-c", "Outra org");
      var archived = CreateNotification(organizationA, "payments", "payment.overdue", "payment-d", "Arquivada");
      archived.Archive(DateTimeOffset.UtcNow);

      setup.NotificationRecords.AddRange(visible, read, otherCategory, otherTenant, archived);
      await setup.SaveChangesAsync();
    }

    await using var context = CreateContext(organizationA, databaseName);
    var repository = new EfNotificationRepository(context);

    var page = await repository.ListAsync(
      new NotificationListRequestDto(Search: "junho", Category: "payments", IsRead: false),
      organizationA,
      UserId.New());
    var includingArchived = await repository.ListAsync(
      new NotificationListRequestDto(Category: "payments", IncludeArchived: true),
      organizationA,
      UserId.New());

    Assert.Single(page.Items);
    Assert.Equal("Aluguel junho", page.Items[0].SubjectDisplayName);
    Assert.Equal(3, includingArchived.TotalItems);
  }

  [Fact]
  public async Task MarkReadMarkAllReadAndArchivePersistState()
  {
    var databaseName = Guid.NewGuid().ToString("N");
    var organizationId = OrganizationId.New();
    var userId = UserId.New();

    await using (var setup = CreateContext(organizationId, databaseName))
    {
      setup.NotificationRecords.Add(CreateNotification(
        organizationId,
        "properties",
        "property.updated",
        "property-a",
        "Casa"));
      setup.NotificationRecords.Add(CreateNotification(
        organizationId,
        "residents",
        "resident.updated",
        "resident-a",
        "Maria"));
      await setup.SaveChangesAsync();
    }

    await using (var context = CreateContext(organizationId, databaseName))
    {
      var repository = new EfNotificationRepository(context);
      var first = await context.NotificationRecords.FirstAsync();

      var marked = await repository.MarkReadAsync(first.Id, organizationId, userId, DateTimeOffset.UtcNow);
      var remaining = await repository.MarkAllReadAsync(organizationId, userId, DateTimeOffset.UtcNow);
      var archived = await repository.ArchiveAsync(first.Id, organizationId, userId, DateTimeOffset.UtcNow);

      Assert.NotNull(marked);
      Assert.True(marked.IsRead);
      Assert.Equal(1, remaining);
      Assert.True(archived);
    }

    await using var assertContext = CreateContext(organizationId, databaseName);
    var active = await assertContext.NotificationRecords.ToListAsync();
    var archivedCount = await assertContext.NotificationRecords
      .IgnoreQueryFilters()
      .CountAsync(notification => notification.DeletedAt != null);

    Assert.Single(active);
    Assert.Equal(1, archivedCount);
    Assert.Equal(0, await new EfNotificationRepository(assertContext).CountUnreadAsync(organizationId, userId));
  }

  [Fact]
  public async Task PreferencesPersistAndSuppressDisabledCategoryChannels()
  {
    var databaseName = Guid.NewGuid().ToString("N");
    var organizationId = OrganizationId.New();
    var userId = UserId.New();

    await using (var setup = CreateContext(organizationId, databaseName))
    {
      setup.NotificationRecords.Add(CreateNotification(
        organizationId,
        "payments",
        "payment.overdue",
        "payment-a",
        "Aluguel"));
      setup.NotificationRecords.Add(CreateNotification(
        organizationId,
        "properties",
        "property.updated",
        "property-a",
        "Casa"));
      await setup.SaveChangesAsync();
    }

    await using var context = CreateContext(organizationId, databaseName);
    var repository = new EfNotificationRepository(context);
    var savedAt = DateTimeOffset.UtcNow;

    var savedPreferences = await repository.SavePreferencesAsync(
      organizationId,
      userId,
      [
        new NotificationPreferenceWriteModel("payments", "in-app", false, false),
        new NotificationPreferenceWriteModel("properties", "in-app", true, false)
      ],
      savedAt);
    var page = await repository.ListAsync(new NotificationListRequestDto(), organizationId, userId);
    var unreadCount = await repository.CountUnreadAsync(organizationId, userId);
    var markAllRead = await repository.MarkAllReadAsync(organizationId, userId, DateTimeOffset.UtcNow);

    Assert.Contains(savedPreferences, item => item.Category == "payments" && !item.IsEnabled);
    var item = Assert.Single(page.Items);
    Assert.Equal("properties", item.Category);
    Assert.Equal(1, unreadCount);
    Assert.Equal(1, markAllRead);
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

  private static NotificationRecord CreateNotification(
    OrganizationId organizationId,
    string moduleName,
    string eventName,
    string subjectEntityId,
    string subjectDisplayName) =>
    NotificationRecord.FromEnvelope(
      ModuleEventEnvelope.Create(
        organizationId,
        moduleName,
        eventName,
        DateTimeOffset.UtcNow,
        EventActor.User(UserId.New(), "Paulo"),
        new EntityReference(moduleName.TrimEnd('s'), subjectEntityId, subjectDisplayName),
        ModuleEventConsumer.Notifications,
        new Dictionary<string, string> { ["status"] = "updated" }),
      DateTimeOffset.UtcNow);
}
