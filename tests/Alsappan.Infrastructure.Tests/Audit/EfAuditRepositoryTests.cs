using Alsappan.Application.Audit;
using Alsappan.Application.Common.Audit;
using Alsappan.Application.Common.Configuration;
using Alsappan.Domain.Common.Events;
using Alsappan.Domain.Common.Identifiers;
using Alsappan.Infrastructure.Audit;
using Alsappan.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Alsappan.Infrastructure.Tests.Audit;

public sealed class EfAuditRepositoryTests
{
  [Fact]
  public async Task ListAsyncFiltersByTenantSearchCategoryAndDate()
  {
    var databaseName = Guid.NewGuid().ToString("N");
    var organizationA = OrganizationId.New();
    var organizationB = OrganizationId.New();
    var occurredAt = new DateTimeOffset(2026, 6, 27, 10, 0, 0, TimeSpan.Zero);

    await using (var setup = CreateContext(organizationA, databaseName))
    {
      setup.AuditLogEntries.Add(AuditLogEntry.FromDraft(CreateDraft(
        organizationA,
        "property.created",
        AuditEntryCategory.Mutation,
        "Calabria casa1",
        occurredAt)));
      setup.AuditLogEntries.Add(AuditLogEntry.FromDraft(CreateDraft(
        organizationB,
        "property.created",
        AuditEntryCategory.Mutation,
        "Calabria outra",
        occurredAt)));
      setup.AuditLogEntries.Add(AuditLogEntry.FromDraft(CreateDraft(
        organizationA,
        "identity.login",
        AuditEntryCategory.Security,
        "Login",
        occurredAt)));
      await setup.SaveChangesAsync();
    }

    await using var context = CreateContext(organizationA, databaseName);
    var repository = new EfAuditRepository(context);

    var page = await repository.ListAsync(new AuditListRequestDto(
      Search: "calabria",
      Category: "Mutation",
      From: new DateOnly(2026, 6, 27),
      To: new DateOnly(2026, 6, 27)));

    var item = Assert.Single(page.Items);
    Assert.Equal("property.created", item.Action);
    Assert.Equal("Calabria casa1", item.TargetDisplayName);
    Assert.Equal("Calabria casa1", item.ChangedFields["displayName"]);
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

  private static AuditEntryDraft CreateDraft(
    OrganizationId organizationId,
    string action,
    AuditEntryCategory category,
    string displayName,
    DateTimeOffset occurredAt) =>
    new(
      organizationId,
      action,
      category,
      EventActor.User(UserId.New(), "Paulo"),
      EntityReference.FromGuid("property", Guid.NewGuid(), displayName),
      occurredAt,
      new Dictionary<string, string> { ["displayName"] = displayName },
      new Dictionary<string, string> { ["module"] = "properties" },
      "trace-a");
}
