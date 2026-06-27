using Alsappan.Application.Settings;
using Alsappan.Application.Settings.Repositories;
using Alsappan.Domain.Common.Identifiers;
using Alsappan.Domain.Identity;
using Alsappan.Domain.Settings;
using Alsappan.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Alsappan.Infrastructure.Settings;

public sealed class EfSettingsRepository : ISettingsRepository
{
  private readonly AlsappanDbContext dbContext;

  public EfSettingsRepository(AlsappanDbContext dbContext)
  {
    this.dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
  }

  public Task<IdentityOrganization?> FindOrganizationAsync(
    OrganizationId organizationId,
    CancellationToken cancellationToken = default) =>
    dbContext.IdentityOrganizations
      .IgnoreQueryFilters()
      .FirstOrDefaultAsync(
        organization => organization.Id == organizationId && organization.DeletedAt == null,
        cancellationToken);

  public async Task<OrganizationSettings> GetOrCreateSettingsAsync(
    OrganizationId organizationId,
    DateTimeOffset createdAt,
    UserId? createdByUserId = null,
    CancellationToken cancellationToken = default)
  {
    var settings = await dbContext.OrganizationSettings
      .IgnoreQueryFilters()
      .FirstOrDefaultAsync(
        candidate => candidate.OrganizationId == organizationId && candidate.DeletedAt == null,
        cancellationToken)
      .ConfigureAwait(false);

    if (settings is not null)
    {
      return settings;
    }

    settings = OrganizationSettings.Create(EntityId.New(), organizationId, createdAt, createdByUserId);
    dbContext.OrganizationSettings.Add(settings);
    await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

    return settings;
  }

  public async Task<IReadOnlyList<DomainCatalogSetting>> ListCatalogItemsAsync(
    OrganizationId organizationId,
    string? catalogType = null,
    CancellationToken cancellationToken = default)
  {
    var query = dbContext.DomainCatalogSettings
      .IgnoreQueryFilters()
      .Where(item => item.OrganizationId == organizationId && item.DeletedAt == null);

    if (!string.IsNullOrWhiteSpace(catalogType))
    {
      var normalizedCatalogType = SettingsCatalog.NormalizeCatalogType(catalogType);
      query = query.Where(item => item.CatalogType == normalizedCatalogType);
    }

    return await query
      .OrderBy(item => item.CatalogType)
      .ThenBy(item => item.SortOrder)
      .ThenBy(item => item.Code)
      .ToListAsync(cancellationToken)
      .ConfigureAwait(false);
  }

  public async Task EnsureCatalogDefaultsAsync(
    OrganizationId organizationId,
    DateTimeOffset createdAt,
    UserId? createdByUserId = null,
    string? catalogType = null,
    CancellationToken cancellationToken = default)
  {
    var normalizedCatalogType = string.IsNullOrWhiteSpace(catalogType)
      ? null
      : SettingsCatalog.NormalizeCatalogType(catalogType);
    var defaultItems = SettingsCatalog.DefaultCatalogItems
      .Where(item => normalizedCatalogType is null || item.CatalogType == normalizedCatalogType)
      .ToArray();

    if (defaultItems.Length == 0)
    {
      return;
    }

    var catalogTypes = defaultItems
      .Select(item => item.CatalogType)
      .Distinct(StringComparer.Ordinal)
      .ToArray();
    var existing = await dbContext.DomainCatalogSettings
      .IgnoreQueryFilters()
      .Where(item =>
        item.OrganizationId == organizationId &&
        item.DeletedAt == null &&
        catalogTypes.Contains(item.CatalogType))
      .Select(item => new { item.CatalogType, item.Code })
      .ToListAsync(cancellationToken)
      .ConfigureAwait(false);
    var existingKeys = existing
      .Select(item => $"{item.CatalogType}:{item.Code}")
      .ToHashSet(StringComparer.OrdinalIgnoreCase);

    foreach (var defaultItem in defaultItems)
    {
      var key = $"{defaultItem.CatalogType}:{defaultItem.Code}";
      if (existingKeys.Contains(key))
      {
        continue;
      }

      dbContext.DomainCatalogSettings.Add(DomainCatalogSetting.Create(
        EntityId.New(),
        organizationId,
        defaultItem.CatalogType,
        defaultItem.Code,
        defaultItem.LabelPtBr,
        defaultItem.LabelEnUs,
        defaultItem.SortOrder,
        isEnabled: true,
        isSystem: true,
        createdAt,
        createdByUserId));
      existingKeys.Add(key);
    }

    await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
  }

  public Task<UserLocalePreference?> FindUserLocalePreferenceAsync(
    OrganizationId organizationId,
    UserId userId,
    CancellationToken cancellationToken = default) =>
    dbContext.UserLocalePreferences
      .IgnoreQueryFilters()
      .FirstOrDefaultAsync(
        preference =>
          preference.OrganizationId == organizationId &&
          preference.UserId == userId &&
          preference.DeletedAt == null,
        cancellationToken);

  public void AddUserLocalePreference(UserLocalePreference preference)
  {
    ArgumentNullException.ThrowIfNull(preference);

    dbContext.UserLocalePreferences.Add(preference);
  }

  public void AddCatalogItem(DomainCatalogSetting item)
  {
    ArgumentNullException.ThrowIfNull(item);

    dbContext.DomainCatalogSettings.Add(item);
  }

  public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
    dbContext.SaveChangesAsync(cancellationToken);
}
