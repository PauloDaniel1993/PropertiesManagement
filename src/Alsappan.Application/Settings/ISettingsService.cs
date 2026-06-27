using Alsappan.Application.Common.Results;

namespace Alsappan.Application.Settings;

public interface ISettingsService
{
  Task<ApplicationOperationResult<SettingsDashboardDto>> GetAsync(
    string? locale = null,
    CancellationToken cancellationToken = default);

  Task<ApplicationOperationResult<OrganizationProfileSettingsDto>> UpdateOrganizationProfileAsync(
    OrganizationProfileUpdateRequestDto request,
    string? locale = null,
    CancellationToken cancellationToken = default);

  Task<ApplicationOperationResult<LocalizationSettingsDto>> UpdateLocalizationAsync(
    LocalizationUpdateRequestDto request,
    string? locale = null,
    CancellationToken cancellationToken = default);

  Task<ApplicationOperationResult<UserLocalePreferenceDto>> GetUserLocalePreferenceAsync(
    string? locale = null,
    CancellationToken cancellationToken = default);

  Task<ApplicationOperationResult<UserLocalePreferenceDto>> UpdateUserLocalePreferenceAsync(
    UserLocalePreferenceUpdateRequestDto request,
    string? locale = null,
    CancellationToken cancellationToken = default);

  Task<ApplicationOperationResult<TenantBehaviorSettingsDto>> UpdateTenantBehaviorAsync(
    TenantBehaviorUpdateRequestDto request,
    string? locale = null,
    CancellationToken cancellationToken = default);

  Task<ApplicationOperationResult<ResidentPortalSettingsDto>> UpdateResidentPortalAsync(
    ResidentPortalUpdateRequestDto request,
    string? locale = null,
    CancellationToken cancellationToken = default);

  Task<ApplicationOperationResult<SecuritySettingsDto>> UpdateSecurityAsync(
    SecuritySettingsUpdateRequestDto request,
    string? locale = null,
    CancellationToken cancellationToken = default);

  Task<ApplicationOperationResult<NotificationSettingsDto>> UpdateNotificationsAsync(
    NotificationSettingsUpdateRequestDto request,
    string? locale = null,
    CancellationToken cancellationToken = default);

  Task<ApplicationOperationResult<IReadOnlyList<DomainCatalogSettingsDto>>> ListCatalogsAsync(
    string? catalogType = null,
    string? locale = null,
    CancellationToken cancellationToken = default);

  Task<ApplicationOperationResult<DomainCatalogSettingsDto>> UpdateCatalogAsync(
    string catalogType,
    DomainCatalogUpdateRequestDto request,
    string? locale = null,
    CancellationToken cancellationToken = default);

  Task<ApplicationOperationResult<OrganizationBrandingSettingsDto>> UpdateBrandingAsync(
    OrganizationBrandingUpdateRequestDto request,
    string? locale = null,
    CancellationToken cancellationToken = default);

  Task<ApplicationOperationResult<OrganizationBrandingSettingsDto>> UploadLogoAsync(
    BrandLogoUploadRequestDto request,
    string? locale = null,
    CancellationToken cancellationToken = default);

  Task<ApplicationOperationResult<OrganizationBrandingSettingsDto>> RemoveLogoAsync(
    string? locale = null,
    CancellationToken cancellationToken = default);

  Task<ApplicationOperationResult<OrganizationBrandingSettingsDto>> ResetBrandingAsync(
    string? locale = null,
    CancellationToken cancellationToken = default);
}
