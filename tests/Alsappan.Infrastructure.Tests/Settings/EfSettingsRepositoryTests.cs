using Alsappan.Application.Common.Configuration;
using Alsappan.Application.Settings;
using Alsappan.Application.Settings.Repositories;
using Alsappan.Domain.Common.Identifiers;
using Alsappan.Domain.Identity;
using Alsappan.Domain.Settings;
using Alsappan.Infrastructure.Persistence;
using Alsappan.Infrastructure.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace Alsappan.Infrastructure.Tests.Settings;

public sealed class EfSettingsRepositoryTests
{
  [Fact]
  public async Task GetOrCreateSettingsAndCatalogDefaultsAreTenantScoped()
  {
    var databaseName = Guid.NewGuid().ToString("N");
    var organizationA = OrganizationId.New();
    var organizationB = OrganizationId.New();
    var now = DateTimeOffset.UtcNow;

    await using var context = CreateContext(organizationA, databaseName);
    var repository = new EfSettingsRepository(context);

    var settingsA = await repository.GetOrCreateSettingsAsync(organizationA, now);
    var settingsB = await repository.GetOrCreateSettingsAsync(organizationB, now);
    await repository.EnsureCatalogDefaultsAsync(organizationA, now, catalogType: SettingsCatalog.PropertyTypes);
    await repository.EnsureCatalogDefaultsAsync(organizationB, now, catalogType: SettingsCatalog.DocumentCategories);

    var propertyTypesA = await repository.ListCatalogItemsAsync(organizationA, SettingsCatalog.PropertyTypes);
    var propertyTypesB = await repository.ListCatalogItemsAsync(organizationB, SettingsCatalog.PropertyTypes);
    var documentsB = await repository.ListCatalogItemsAsync(organizationB, SettingsCatalog.DocumentCategories);

    Assert.NotEqual(settingsA.Id, settingsB.Id);
    Assert.All(propertyTypesA, item => Assert.Equal(organizationA, item.OrganizationId));
    Assert.NotEmpty(propertyTypesA);
    Assert.Empty(propertyTypesB);
    Assert.NotEmpty(documentsB);
    Assert.All(documentsB, item => Assert.Equal(organizationB, item.OrganizationId));
  }

  [Fact]
  public async Task EnsureCatalogDefaultsIsIdempotentAndAllowsCustomCatalogUpdates()
  {
    var organizationId = OrganizationId.New();
    await using var context = CreateContext(organizationId, Guid.NewGuid().ToString("N"));
    var repository = new EfSettingsRepository(context);

    await repository.EnsureCatalogDefaultsAsync(
      organizationId,
      DateTimeOffset.UtcNow,
      catalogType: SettingsCatalog.UtilityTypes);
    await repository.EnsureCatalogDefaultsAsync(
      organizationId,
      DateTimeOffset.UtcNow,
      catalogType: SettingsCatalog.UtilityTypes);
    repository.AddCatalogItem(DomainCatalogSetting.Create(
      EntityId.New(),
      organizationId,
      SettingsCatalog.UtilityTypes,
      "solar",
      "Energia solar",
      "Solar power",
      5,
      isEnabled: true,
      isSystem: false,
      DateTimeOffset.UtcNow));
    await repository.SaveChangesAsync();

    var utilityTypes = await repository.ListCatalogItemsAsync(organizationId, SettingsCatalog.UtilityTypes);
    var electricity = utilityTypes.Single(item => item.Code == "electricity");
    electricity.Update(
      "Energia eletrica",
      "Electricity",
      electricity.SortOrder,
      electricity.IsEnabled,
      DateTimeOffset.UtcNow,
      updatedByUserId: null);
    await repository.SaveChangesAsync();
    context.ChangeTracker.Clear();

    var updatedUtilityTypes = await repository.ListCatalogItemsAsync(organizationId, SettingsCatalog.UtilityTypes);

    Assert.Equal(
      SettingsCatalog.DefaultCatalogItems.Count(item => item.CatalogType == SettingsCatalog.UtilityTypes) + 1,
      updatedUtilityTypes.Count);
    Assert.Contains(updatedUtilityTypes, item => item.Code == "solar" && !item.IsSystem);
    Assert.Contains(updatedUtilityTypes, item => item.Code == "electricity" && item.LabelPtBr == "Energia eletrica");
  }

  [Fact]
  public async Task UserLocalePreferenceIsScopedByOrganizationAndUser()
  {
    var organizationA = OrganizationId.New();
    var organizationB = OrganizationId.New();
    var userId = UserId.New();
    await using var context = CreateContext(organizationA, Guid.NewGuid().ToString("N"));
    var repository = new EfSettingsRepository(context);

    repository.AddUserLocalePreference(UserLocalePreference.Create(
      EntityId.New(),
      organizationA,
      userId,
      "en-US",
      DateTimeOffset.UtcNow,
      userId));
    repository.AddUserLocalePreference(UserLocalePreference.Create(
      EntityId.New(),
      organizationB,
      userId,
      "pt-BR",
      DateTimeOffset.UtcNow,
      userId));
    await repository.SaveChangesAsync();

    var preferenceA = await repository.FindUserLocalePreferenceAsync(organizationA, userId);
    var preferenceB = await repository.FindUserLocalePreferenceAsync(organizationB, userId);

    Assert.Equal("en-US", preferenceA!.Locale);
    Assert.Equal("pt-BR", preferenceB!.Locale);
  }

  [Fact]
  public async Task SettingsSeedContributorSeedsSettingsAndCatalogDefaultsForOrganizations()
  {
    var databaseName = Guid.NewGuid().ToString("N");
    var organizationId = OrganizationId.New();
    var services = new ServiceCollection();
    services.AddSingleton(TimeProvider.System);
    services.AddScoped<ISettingsRepository, EfSettingsRepository>();
    services.AddDbContext<AlsappanDbContext>(options => options
      .UseInMemoryDatabase(databaseName)
      .ReplaceService<IModelCacheKeyFactory, AlsappanModelCacheKeyFactory>());
    await using var provider = services.BuildServiceProvider();

    await using (var scope = provider.CreateAsyncScope())
    {
      var dbContext = scope.ServiceProvider.GetRequiredService<AlsappanDbContext>();
      dbContext.IdentityOrganizations.Add(IdentityOrganization.Create(
        organizationId,
        "seeded",
        "Seeded",
        DateTimeOffset.UtcNow,
        displayName: "Seeded"));
      await dbContext.SaveChangesAsync();
    }

    await using (var scope = provider.CreateAsyncScope())
    {
      var contributor = new SettingsSeedContributor();
      await contributor.SeedAsync(scope.ServiceProvider);
    }

    await using (var scope = provider.CreateAsyncScope())
    {
      var dbContext = scope.ServiceProvider.GetRequiredService<AlsappanDbContext>();
      var settings = await dbContext.OrganizationSettings
        .IgnoreQueryFilters()
        .SingleAsync(item => item.OrganizationId == organizationId);
      var catalogs = await dbContext.DomainCatalogSettings
        .IgnoreQueryFilters()
        .Where(item => item.OrganizationId == organizationId)
        .ToListAsync();

      Assert.Equal("pt-BR", settings.DefaultLocale);
      Assert.Contains(catalogs, item => item.CatalogType == SettingsCatalog.PropertyTypes);
      Assert.Contains(catalogs, item => item.CatalogType == SettingsCatalog.DocumentCategories);
    }
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
}
