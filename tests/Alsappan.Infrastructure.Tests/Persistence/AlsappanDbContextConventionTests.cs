using Alsappan.Application.Common.Configuration;
using Alsappan.Domain.Common.Events;
using Alsappan.Domain.Common.Identifiers;
using Alsappan.Infrastructure.Notifications;
using Alsappan.Infrastructure.Outbox;
using Alsappan.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Alsappan.Infrastructure.Tests.Persistence;

public sealed class AlsappanDbContextConventionTests
{
  [Fact]
  public void ModelUsesConfiguredSchemaAndSnakeCaseNames()
  {
    using var context = CreateContext(OrganizationId.New(), schema: "tenant_core");
    var entityType = context.Model.FindEntityType(typeof(OutboxMessage));

    Assert.NotNull(entityType);
    Assert.Equal("tenant_core", entityType.GetSchema());
    Assert.Equal("outbox_messages", entityType.GetTableName());
    Assert.Equal("organization_id", entityType.FindProperty(nameof(OutboxMessage.OrganizationId))?.GetColumnName());
  }

  [Fact]
  public async Task TenantAndSoftDeleteFiltersHideOtherOrganizationsAndArchivedRows()
  {
    var databaseName = Guid.NewGuid().ToString("N");
    var organizationA = OrganizationId.New();
    var organizationB = OrganizationId.New();

    await using (var setup = CreateContext(organizationA, databaseName: databaseName))
    {
      var active = NotificationRecord.FromEnvelope(CreateEnvelope(organizationA), DateTimeOffset.UtcNow);
      var otherTenant = NotificationRecord.FromEnvelope(CreateEnvelope(organizationB), DateTimeOffset.UtcNow);
      var archived = NotificationRecord.FromEnvelope(CreateEnvelope(organizationA), DateTimeOffset.UtcNow);
      archived.Archive(DateTimeOffset.UtcNow);

      setup.NotificationRecords.AddRange(active, otherTenant, archived);
      await setup.SaveChangesAsync();
    }

    await using var context = CreateContext(organizationA, databaseName: databaseName);

    var visible = await context.NotificationRecords.ToListAsync();
    var allRows = await context.NotificationRecords.IgnoreQueryFilters().ToListAsync();

    Assert.Single(visible);
    Assert.Equal(3, allRows.Count);
  }

  [Fact]
  public async Task OutboxProcessorCanIgnoreTenantFilterForBackgroundWork()
  {
    var databaseName = Guid.NewGuid().ToString("N");
    var organizationA = OrganizationId.New();
    var organizationB = OrganizationId.New();

    await using (var setup = CreateContext(organizationA, databaseName: databaseName))
    {
      setup.OutboxMessages.Add(OutboxMessage.FromEnvelope(CreateEnvelope(organizationA), DateTimeOffset.UtcNow));
      setup.OutboxMessages.Add(OutboxMessage.FromEnvelope(CreateEnvelope(organizationB), DateTimeOffset.UtcNow));
      await setup.SaveChangesAsync();
    }

    await using var context = CreateContext(organizationA, databaseName: databaseName);

    Assert.Single(await context.OutboxMessages.ToListAsync());
    Assert.Equal(2, await context.OutboxMessages.IgnoreQueryFilters().CountAsync());
  }

  [Fact]
  public void PostgreSqlFixtureIsEnvironmentDriven()
  {
    var fixture = new PostgreSqlIntegrationFixture();

    Assert.Equal(
      !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(
        PostgreSqlIntegrationFixture.ConnectionStringEnvironmentVariable)),
      fixture.IsEnabled);
  }

  private static AlsappanDbContext CreateContext(
    OrganizationId organizationId,
    string? databaseName = null,
    string schema = "app")
  {
    var options = new DbContextOptionsBuilder<AlsappanDbContext>()
      .UseInMemoryDatabase(databaseName ?? Guid.NewGuid().ToString("N"))
      .ReplaceService<IModelCacheKeyFactory, AlsappanModelCacheKeyFactory>()
      .Options;

    return new AlsappanDbContext(
      options,
      new StaticActiveOrganizationContext(organizationId),
      new DatabaseOptions { Schema = schema });
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
      new Dictionary<string, string> { ["status"] = "available" });
}
