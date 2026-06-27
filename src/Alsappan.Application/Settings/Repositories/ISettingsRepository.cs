using Alsappan.Domain.Common.Identifiers;
using Alsappan.Domain.Identity;
using Alsappan.Domain.Settings;

namespace Alsappan.Application.Settings.Repositories;

public interface ISettingsRepository
{
  Task<IdentityOrganization?> FindOrganizationAsync(
    OrganizationId organizationId,
    CancellationToken cancellationToken = default);

  Task<OrganizationSettings> GetOrCreateSettingsAsync(
    OrganizationId organizationId,
    DateTimeOffset createdAt,
    UserId? createdByUserId = null,
    CancellationToken cancellationToken = default);

  Task<IReadOnlyList<DomainCatalogSetting>> ListCatalogItemsAsync(
    OrganizationId organizationId,
    string? catalogType = null,
    CancellationToken cancellationToken = default);

  Task EnsureCatalogDefaultsAsync(
    OrganizationId organizationId,
    DateTimeOffset createdAt,
    UserId? createdByUserId = null,
    string? catalogType = null,
    CancellationToken cancellationToken = default);

  Task<UserLocalePreference?> FindUserLocalePreferenceAsync(
    OrganizationId organizationId,
    UserId userId,
    CancellationToken cancellationToken = default);

  void AddUserLocalePreference(UserLocalePreference preference);

  void AddCatalogItem(DomainCatalogSetting item);

  Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
