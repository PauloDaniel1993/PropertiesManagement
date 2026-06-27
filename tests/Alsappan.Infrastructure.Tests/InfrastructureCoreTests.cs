using Alsappan.Application.Common.Audit;
using Alsappan.Application.Common.Authorization;
using Alsappan.Application.Common.Configuration;
using Alsappan.Application.Common.Seeding;
using Alsappan.Domain.Common.Events;
using Alsappan.Domain.Common.Identifiers;
using Alsappan.Domain.Identity;
using Alsappan.Infrastructure.Audit;
using Alsappan.Infrastructure.Authorization;
using Alsappan.Infrastructure.Notifications;
using Alsappan.Infrastructure.Outbox;
using Alsappan.Infrastructure.Persistence;
using Alsappan.Infrastructure.Seeding;
using Alsappan.Infrastructure.Timeline;
using Microsoft.EntityFrameworkCore;

namespace Alsappan.Infrastructure.Tests;

public sealed class InfrastructureCoreTests
{
  [Fact]
  public async Task AuditWriterPersistsMutationAuditEntry()
  {
    var organizationId = OrganizationId.New();
    await using var context = CreateContext(organizationId);
    var writer = new EfAuditWriter(context);

    await writer.WriteAsync(new AuditEntryDraft(
      organizationId,
      "property.updated",
      AuditEntryCategory.Mutation,
      EventActor.User(UserId.New(), "Paulo"),
      EntityReference.FromGuid("property", Guid.NewGuid()),
      DateTimeOffset.UtcNow,
      new Dictionary<string, string> { ["status"] = "available" }));

    var entry = await context.AuditLogEntries.SingleAsync();

    Assert.Equal("property.updated", entry.Action);
    Assert.Contains("available", entry.ChangedFieldsJson, StringComparison.Ordinal);
  }

  [Fact]
  public async Task OutboxProcessorDispatchesAuditTimelineAndNotificationConsumers()
  {
    var organizationId = OrganizationId.New();
    var databaseName = Guid.NewGuid().ToString("N");

    await using (var setup = CreateContext(organizationId, databaseName))
    {
      AddActiveMember(setup, organizationId, UserId.New(), [RoleCodes.OrganizationAdmin]);
      var outboxWriter = new EfModuleEventOutboxWriter(setup);
      await outboxWriter.EnqueueAsync(CreateEnvelope(organizationId));
      await setup.SaveChangesAsync();
    }

    await using (var processorContext = CreateContext(organizationId, databaseName))
    {
      var dispatcher = new ModuleEventDispatcher(
        new EfAuditWriter(processorContext),
        new EfTimelineProjectionWriter(processorContext),
        new EfNotificationDispatcher(processorContext, new DefaultRolePermissionCatalog()));
      var processor = new OutboxProcessor(processorContext, dispatcher);

      var result = await processor.ProcessPendingAsync();

      Assert.Equal(1, result.ProcessedCount);
      Assert.Equal(0, result.FailedCount);
    }

    await using var assertContext = CreateContext(organizationId, databaseName);

    Assert.Equal(1, await assertContext.AuditLogEntries.CountAsync());
    Assert.Equal(1, await assertContext.TimelineEntries.CountAsync());
    Assert.Equal(1, await assertContext.NotificationRecords.CountAsync());
    Assert.True((await assertContext.OutboxMessages.IgnoreQueryFilters().SingleAsync()).IsProcessed);
  }

  [Fact]
  public async Task SeedRunnerRecordsContributorsOnce()
  {
    var organizationId = OrganizationId.New();
    await using var context = CreateContext(organizationId);
    var contributor = new TestSeedContributor();
    var runner = new EfDatabaseSeedRunner(
      context,
      [contributor],
      EmptyServiceProvider.Instance);

    var firstRun = await runner.RunAsync();
    var secondRun = await runner.RunAsync();

    Assert.True(firstRun.ExecutedAny);
    Assert.Equal(["platform-reference-data@2026-06-26"], firstRun.ExecutedContributors);
    Assert.Equal(["platform-reference-data@2026-06-26"], secondRun.SkippedContributors);
    Assert.Equal(1, contributor.CallCount);
  }

  private static AlsappanDbContext CreateContext(
    OrganizationId organizationId,
    string? databaseName = null)
  {
    var options = new DbContextOptionsBuilder<AlsappanDbContext>()
      .UseInMemoryDatabase(databaseName ?? Guid.NewGuid().ToString("N"))
      .Options;

    return new AlsappanDbContext(
      options,
      new StaticActiveOrganizationContext(organizationId),
      new DatabaseOptions());
  }

  private static ModuleEventEnvelope CreateEnvelope(OrganizationId organizationId) =>
    ModuleEventEnvelope.Create(
      organizationId,
      "properties",
      "property.updated",
      DateTimeOffset.UtcNow,
      EventActor.User(UserId.New(), "Paulo"),
      EntityReference.FromGuid("property", Guid.NewGuid(), "Calabria casa1"),
      ModuleEventConsumer.Audit | ModuleEventConsumer.Timeline | ModuleEventConsumer.Notifications,
      new Dictionary<string, string> { ["status"] = "available" },
      correlationId: "request-1");

  private static void AddActiveMember(
    AlsappanDbContext context,
    OrganizationId organizationId,
    UserId userId,
    IReadOnlyList<string> roleCodes)
  {
    var now = DateTimeOffset.UtcNow;
    context.IdentityUsers.Add(IdentityUser.Create(
      userId,
      $"{userId.Value:N}@example.com",
      "Notification recipient",
      UserAccountType.Admin,
      now,
      status: UserStatus.Active));
    context.IdentityMemberships.Add(IdentityMembership.Create(
      EntityId.New(),
      organizationId,
      userId,
      roleCodes,
      now));
  }

  private sealed class TestSeedContributor : IDatabaseSeedContributor
  {
    public string Name => "platform-reference-data";

    public string Version => "2026-06-26";

    public int CallCount { get; private set; }

    public Task SeedAsync(IServiceProvider serviceProvider, CancellationToken cancellationToken = default)
    {
      CallCount++;
      return Task.CompletedTask;
    }
  }

  private sealed class EmptyServiceProvider : IServiceProvider
  {
    public static EmptyServiceProvider Instance { get; } = new();

    public object? GetService(Type serviceType) => null;
  }
}
