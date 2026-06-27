using Alsappan.Application.Common.Configuration;
using Alsappan.Application.Residents;
using Alsappan.Domain.Common.Identifiers;
using Alsappan.Domain.Residents;
using Alsappan.Infrastructure.Persistence;
using Alsappan.Infrastructure.Residents;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Alsappan.Infrastructure.Tests.Residents;

public sealed class EfResidentRepositoryTests
{
  [Fact]
  public async Task ListAsyncFiltersByOrganizationSearchStatusPortalAndArchivedState()
  {
    var databaseName = Guid.NewGuid().ToString("N");
    var organizationA = OrganizationId.New();
    var organizationB = OrganizationId.New();

    await using (var setup = CreateContext(organizationA, databaseName))
    {
      var visible = CreateResident(organizationA, "Joao da Silva", ResidentStatus.Active, ResidentPortalStatus.Active);
      var otherTenant = CreateResident(organizationB, "Joao outra org", ResidentStatus.Active, ResidentPortalStatus.Active);
      var archived = CreateResident(organizationA, "Joao antigo", ResidentStatus.Active, ResidentPortalStatus.NotInvited);
      archived.Archive(DateTimeOffset.UtcNow, null);

      setup.Residents.AddRange(visible, otherTenant, archived);
      await setup.SaveChangesAsync();
    }

    await using var context = CreateContext(organizationA, databaseName);
    var repository = new EfResidentRepository(context);

    var page = await repository.ListAsync(
      new ResidentListRequestDto(Search: "joao", Status: "active", PortalStatus: "active", HasPortalAccess: true),
      organizationA);

    Assert.Single(page.Items);
    Assert.Equal("Joao da Silva", page.Items[0].FullName);

    var includingArchived = await repository.ListAsync(
      new ResidentListRequestDto(Search: "joao", IncludeArchived: true),
      organizationA);
    Assert.Equal(2, includingArchived.TotalItems);
  }

  [Fact]
  public async Task FindPotentialDuplicatesAsyncMatchesNormalizedEmailPhoneAndDocument()
  {
    var organizationId = OrganizationId.New();
    await using var context = CreateContext(organizationId, Guid.NewGuid().ToString("N"));
    var resident = CreateResident(organizationId, "Joao da Silva", ResidentStatus.Active, ResidentPortalStatus.NotInvited);
    context.Residents.Add(resident);
    await context.SaveChangesAsync();
    var repository = new EfResidentRepository(context);

    var matches = await repository.FindPotentialDuplicatesAsync(
      new ResidentDuplicateWarningRequestDto("JOAO@example.com", "(11) 99999-8888", "12345678900"),
      organizationId);

    Assert.Single(matches);
    Assert.Equal(resident.Id, matches[0].Id);
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

  private static Resident CreateResident(
    OrganizationId organizationId,
    string fullName,
    ResidentStatus status,
    ResidentPortalStatus portalStatus) =>
    Resident.Create(
      EntityId.New(),
      organizationId,
      fullName,
      null,
      "joao@example.com",
      "(11) 99999-8888",
      null,
      "CPF",
      "123.456.789-00",
      new DateOnly(1985, 1, 20),
      "Maria",
      "Mae",
      "(11) 98888-7777",
      status,
      portalStatus,
      ResidentPrivacyOptions.IdentificationData,
      "Observacoes",
      portalStatus == ResidentPortalStatus.Active ? UserId.New() : null,
      DateTimeOffset.UtcNow);
}
