using Alsappan.Application.Common.Configuration;
using Alsappan.Application.Common.Authorization;
using Alsappan.Application.Notifications;
using Alsappan.Application.Notifications.Repositories;
using Alsappan.Domain.Common.Events;
using Alsappan.Domain.Common.Identifiers;
using Alsappan.Domain.Identity;
using Alsappan.Infrastructure.Authorization;
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
    var userId = UserId.New();

    await using (var setup = CreateContext(organizationA, databaseName))
    {
      AddActiveMember(setup, organizationA, userId, UserAccountType.Admin, [RoleCodes.OrganizationAdmin]);
      var visible = CreateNotification(organizationA, userId, "payments", "payment.overdue", "payment-a", "Aluguel junho");
      var read = CreateNotification(organizationA, userId, "payments", "payment.overdue", "payment-b", "Aluguel maio");
      read.MarkRead(DateTimeOffset.UtcNow);
      var otherCategory = CreateNotification(organizationA, userId, "properties", "property.updated", "property-a", "Casa");
      var otherTenant = CreateNotification(organizationB, userId, "payments", "payment.overdue", "payment-c", "Outra org");
      var otherRecipient = CreateNotification(organizationA, UserId.New(), "payments", "payment.overdue", "payment-e", "Outro usuario");
      var archived = CreateNotification(organizationA, userId, "payments", "payment.overdue", "payment-d", "Arquivada");
      archived.Archive(DateTimeOffset.UtcNow);

      setup.NotificationRecords.AddRange(visible, read, otherCategory, otherTenant, otherRecipient, archived);
      await setup.SaveChangesAsync();
    }

    await using var context = CreateContext(organizationA, databaseName);
    var repository = new EfNotificationRepository(context);

    var page = await repository.ListAsync(
      new NotificationListRequestDto(Search: "junho", Category: "payments", IsRead: false),
      organizationA,
      userId);
    var includingArchived = await repository.ListAsync(
      new NotificationListRequestDto(Category: "payments", IncludeArchived: true),
      organizationA,
      userId);

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
      AddActiveMember(setup, organizationId, userId, UserAccountType.Admin, [RoleCodes.OrganizationAdmin]);
      setup.NotificationRecords.Add(CreateNotification(
        organizationId,
        userId,
        "properties",
        "property.updated",
        "property-a",
        "Casa"));
      setup.NotificationRecords.Add(CreateNotification(
        organizationId,
        userId,
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
  public async Task MarkReadDoesNotAffectOtherRecipients()
  {
    var databaseName = Guid.NewGuid().ToString("N");
    var organizationId = OrganizationId.New();
    var firstUserId = UserId.New();
    var secondUserId = UserId.New();
    var eventId = Guid.NewGuid();

    await using (var setup = CreateContext(organizationId, databaseName))
    {
      AddActiveMember(setup, organizationId, firstUserId, UserAccountType.Admin, [RoleCodes.OrganizationAdmin]);
      AddActiveMember(setup, organizationId, secondUserId, UserAccountType.Admin, [RoleCodes.OrganizationAdmin]);
      setup.NotificationRecords.Add(CreateNotification(
        organizationId,
        firstUserId,
        "properties",
        "property.updated",
        "property-a",
        "Casa",
        eventId));
      setup.NotificationRecords.Add(CreateNotification(
        organizationId,
        secondUserId,
        "properties",
        "property.updated",
        "property-a",
        "Casa",
        eventId));
      await setup.SaveChangesAsync();
    }

    await using var context = CreateContext(organizationId, databaseName);
    var repository = new EfNotificationRepository(context);
    var firstUserNotification = await context.NotificationRecords
      .IgnoreQueryFilters()
      .SingleAsync(notification => notification.RecipientUserId == firstUserId);

    var marked = await repository.MarkReadAsync(
      firstUserNotification.Id,
      organizationId,
      firstUserId,
      DateTimeOffset.UtcNow);
    var firstUnreadCount = await repository.CountUnreadAsync(organizationId, firstUserId);
    var secondUnreadCount = await repository.CountUnreadAsync(organizationId, secondUserId);

    Assert.NotNull(marked);
    Assert.Equal(0, firstUnreadCount);
    Assert.Equal(1, secondUnreadCount);
  }

  [Fact]
  public async Task PreferencesPersistAndSuppressDisabledCategoryChannels()
  {
    var databaseName = Guid.NewGuid().ToString("N");
    var organizationId = OrganizationId.New();
    var userId = UserId.New();

    await using (var setup = CreateContext(organizationId, databaseName))
    {
      AddActiveMember(setup, organizationId, userId, UserAccountType.Admin, [RoleCodes.OrganizationAdmin]);
      setup.NotificationRecords.Add(CreateNotification(
        organizationId,
        userId,
        "payments",
        "payment.overdue",
        "payment-a",
        "Aluguel"));
      setup.NotificationRecords.Add(CreateNotification(
        organizationId,
        userId,
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

  [Fact]
  public async Task DispatcherCreatesRecipientRowsOnlyForMembersWithNotificationAndSourcePermissions()
  {
    var databaseName = Guid.NewGuid().ToString("N");
    var organizationId = OrganizationId.New();
    var adminUserId = UserId.New();
    var residentUserId = UserId.New();

    await using var context = CreateContext(organizationId, databaseName);
    AddActiveMember(context, organizationId, adminUserId, UserAccountType.Admin, [RoleCodes.OrganizationAdmin]);
    AddActiveMember(context, organizationId, residentUserId, UserAccountType.Resident, [RoleCodes.ResidentUser]);
    await context.SaveChangesAsync();

    var dispatcher = new EfNotificationDispatcher(context, new DefaultRolePermissionCatalog());
    await dispatcher.DispatchAsync(CreateEnvelope(
      organizationId,
      "administrators",
      "administrators.invited",
      "identityUser",
      "admin-a",
      "Ana Admin"));

    var notification = await context.NotificationRecords
      .IgnoreQueryFilters()
      .SingleAsync();

    Assert.Equal(adminUserId, notification.RecipientUserId);
  }

  [Fact]
  public async Task ListAsyncFiltersRecipientRowsByCurrentSourceModulePermission()
  {
    var databaseName = Guid.NewGuid().ToString("N");
    var organizationId = OrganizationId.New();
    var residentUserId = UserId.New();

    await using (var setup = CreateContext(organizationId, databaseName))
    {
      AddActiveMember(setup, organizationId, residentUserId, UserAccountType.Resident, [RoleCodes.ResidentUser]);
      setup.NotificationRecords.Add(CreateNotification(
        organizationId,
        residentUserId,
        "administrators",
        "administrators.invited",
        "identityUser",
        "Ana Admin"));
      await setup.SaveChangesAsync();
    }

    await using var context = CreateContext(organizationId, databaseName);
    var repository = new EfNotificationRepository(context);

    var page = await repository.ListAsync(new NotificationListRequestDto(), organizationId, residentUserId);
    var unreadCount = await repository.CountUnreadAsync(organizationId, residentUserId);

    Assert.Empty(page.Items);
    Assert.Equal(0, unreadCount);
  }

  [Fact]
  public async Task DispatcherFansOutSeparateRowsForEachEligibleRecipient()
  {
    var databaseName = Guid.NewGuid().ToString("N");
    var organizationId = OrganizationId.New();
    var adminUserId = UserId.New();
    var managerUserId = UserId.New();
    var residentUserId = UserId.New();

    await using var context = CreateContext(organizationId, databaseName);
    AddActiveMember(context, organizationId, adminUserId, UserAccountType.Admin, [RoleCodes.OrganizationAdmin]);
    AddActiveMember(context, organizationId, managerUserId, UserAccountType.Admin, [RoleCodes.OrganizationManager]);
    AddActiveMember(context, organizationId, residentUserId, UserAccountType.Resident, [RoleCodes.ResidentUser]);
    await context.SaveChangesAsync();

    var dispatcher = new EfNotificationDispatcher(context, new DefaultRolePermissionCatalog());
    await dispatcher.DispatchAsync(CreateEnvelope(
      organizationId,
      "properties",
      "property.updated",
      "property",
      "property-a",
      "Casa"));

    var recipients = await context.NotificationRecords
      .IgnoreQueryFilters()
      .Select(notification => notification.RecipientUserId)
      .ToListAsync();

    Assert.Equal(2, recipients.Count);
    Assert.Contains(adminUserId, recipients);
    Assert.Contains(managerUserId, recipients);
    Assert.DoesNotContain(residentUserId, recipients);
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
    UserId recipientUserId,
    string moduleName,
    string eventName,
    string subjectEntityId,
    string subjectDisplayName,
    Guid? eventId = null) =>
    NotificationRecord.FromEnvelope(
      new ModuleEventEnvelope(
        eventId ?? Guid.NewGuid(),
        organizationId,
        moduleName,
        eventName,
        DateTimeOffset.UtcNow,
        EventActor.User(UserId.New(), "Paulo"),
        new EntityReference(moduleName.TrimEnd('s'), subjectEntityId, subjectDisplayName),
        ModuleEventConsumer.Notifications,
        new Dictionary<string, string> { ["status"] = "updated" }),
      DateTimeOffset.UtcNow,
      recipientUserId);

  private static ModuleEventEnvelope CreateEnvelope(
    OrganizationId organizationId,
    string moduleName,
    string eventName,
    string subjectEntityType,
    string subjectEntityId,
    string subjectDisplayName) =>
    ModuleEventEnvelope.Create(
      organizationId,
      moduleName,
      eventName,
      DateTimeOffset.UtcNow,
      EventActor.User(UserId.New(), "Paulo"),
      new EntityReference(subjectEntityType, subjectEntityId, subjectDisplayName),
      ModuleEventConsumer.Notifications,
      new Dictionary<string, string> { ["status"] = "updated" });

  private static void AddActiveMember(
    AlsappanDbContext context,
    OrganizationId organizationId,
    UserId userId,
    UserAccountType accountType,
    IReadOnlyList<string> roleCodes)
  {
    var now = DateTimeOffset.UtcNow;
    context.IdentityUsers.Add(IdentityUser.Create(
      userId,
      $"{userId.Value:N}@example.com",
      "Notification recipient",
      accountType,
      now,
      status: UserStatus.Active));
    context.IdentityMemberships.Add(IdentityMembership.Create(
      EntityId.New(),
      organizationId,
      userId,
      roleCodes,
      now));
  }
}
