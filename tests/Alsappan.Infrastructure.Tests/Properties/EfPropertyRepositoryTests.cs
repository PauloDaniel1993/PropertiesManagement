using Alsappan.Application.Properties;
using Alsappan.Application.Common.Configuration;
using Alsappan.Domain.Common.Identifiers;
using Alsappan.Domain.Common.ValueObjects;
using Alsappan.Domain.Properties;
using Alsappan.Infrastructure.Persistence;
using Alsappan.Infrastructure.Properties;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Alsappan.Infrastructure.Tests.Properties;

public sealed class EfPropertyRepositoryTests
{
  [Fact]
  public async Task ListAsyncFiltersByOrganizationSearchStatusAndArchivedState()
  {
    var databaseName = Guid.NewGuid().ToString("N");
    var organizationA = OrganizationId.New();
    var organizationB = OrganizationId.New();

    await using (var setup = CreateContext(organizationA, databaseName))
    {
      var visible = CreateProperty(organizationA, "Calabria casa1", "Vila Fazzione", PropertyStatus.Rented);
      var otherTenant = CreateProperty(organizationB, "Calabria outra", "Vila Fazzione", PropertyStatus.Rented);
      var archived = CreateProperty(organizationA, "Calabria antiga", "Vila Fazzione", PropertyStatus.Rented);
      archived.Archive(DateTimeOffset.UtcNow, null);

      setup.Properties.AddRange(visible, otherTenant, archived);
      await setup.SaveChangesAsync();
    }

    await using var context = CreateContext(organizationA, databaseName);
    var repository = new EfPropertyRepository(context);

    var page = await repository.ListAsync(
      new PropertyListRequestDto(Search: "fazzione", Status: "rented"),
      organizationA);

    Assert.Single(page.Items);
    Assert.Equal("Calabria casa1", page.Items[0].Name);

    var includingArchived = await repository.ListAsync(
      new PropertyListRequestDto(Search: "calabria", IncludeArchived: true),
      organizationA);
    Assert.Equal(2, includingArchived.TotalItems);
  }

  [Fact]
  public async Task FindAsyncCanIncludeArchivedProperty()
  {
    var organizationId = OrganizationId.New();
    await using var context = CreateContext(organizationId, Guid.NewGuid().ToString("N"));
    var property = CreateProperty(organizationId, "Casa Arquivada", "Centro", PropertyStatus.Available);
    property.Archive(DateTimeOffset.UtcNow, null);
    context.Properties.Add(property);
    await context.SaveChangesAsync();

    var repository = new EfPropertyRepository(context);

    Assert.Null(await repository.FindAsync(property.Id, organizationId));
    Assert.NotNull(await repository.FindAsync(property.Id, organizationId, includeArchived: true));
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

  private static RentalProperty CreateProperty(
    OrganizationId organizationId,
    string name,
    string neighborhood,
    PropertyStatus status) =>
    RentalProperty.Create(
      EntityId.New(),
      organizationId,
      name,
      PropertyType.House,
      "Teste",
      new Address("Rua Calabria", "82", null, neighborhood, "Sao Paulo", "SP", "00000-000"),
      status,
      new Money(700m, "BRL"),
      1,
      "A1",
      null,
      DateTimeOffset.UtcNow);
}
