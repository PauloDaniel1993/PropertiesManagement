namespace Alsappan.Application.Settings;

#pragma warning disable CA1054, CA1056

public sealed record SettingsDashboardDto(
  OrganizationProfileSettingsDto Organization,
  TenantBehaviorSettingsDto TenantBehavior,
  ResidentPortalSettingsDto ResidentPortal,
  LocalizationSettingsDto Localization,
  SecuritySettingsDto Security,
  NotificationSettingsDto Notifications,
  OrganizationBrandingSettingsDto Branding,
  UserLocalePreferenceDto ProfilePreferences,
  IReadOnlyList<DomainCatalogSettingsDto> Catalogs);

public sealed record OrganizationProfileSettingsDto(
  Guid OrganizationId,
  string Slug,
  string Name,
  string DisplayName,
  string CurrencyCode,
  string TimeZone,
  string? ContactEmail,
  string? ContactPhone,
  string? ContactWebsite,
  DateTimeOffset CreatedAt,
  DateTimeOffset? UpdatedAt,
  string? ConcurrencyToken);

public sealed record OrganizationProfileUpdateRequestDto(
  string Slug,
  string Name,
  string DisplayName,
  string CurrencyCode,
  string TimeZone,
  string? ContactEmail,
  string? ContactPhone,
  string? ContactWebsite,
  string? ConcurrencyToken = null);

public sealed record TenantBehaviorSettingsDto(
  bool AllowOrganizationSwitching,
  bool RequireActiveOrganization,
  bool StrictTenantIsolation,
  string? ConcurrencyToken);

public sealed record TenantBehaviorUpdateRequestDto(
  bool AllowOrganizationSwitching,
  bool RequireActiveOrganization,
  bool StrictTenantIsolation,
  string? ConcurrencyToken = null);

public sealed record ResidentPortalSettingsDto(
  bool IsEnabled,
  bool AllowOccurrenceCreation,
  bool AllowDocumentUpload,
  bool AllowProfileUpdateRequests,
  string? ConcurrencyToken);

public sealed record ResidentPortalUpdateRequestDto(
  bool IsEnabled,
  bool AllowOccurrenceCreation,
  bool AllowDocumentUpload,
  bool AllowProfileUpdateRequests,
  string? ConcurrencyToken = null);

public sealed record LocalizationSettingsDto(
  string DefaultLocale,
  string FallbackLocale,
  IReadOnlyList<string> EnabledLocales,
  IReadOnlyList<LocaleOptionDto> SupportedLocales,
  string? ConcurrencyToken);

public sealed record LocalizationUpdateRequestDto(
  string DefaultLocale,
  string FallbackLocale,
  IReadOnlyList<string> EnabledLocales,
  string? ConcurrencyToken = null);

public sealed record LocaleOptionDto(
  string Code,
  string Label,
  bool IsEnabled,
  bool IsDefault,
  bool IsFallback);

public sealed record UserLocalePreferenceDto(
  string Locale,
  string EffectiveLocale,
  bool IsPersisted,
  IReadOnlyList<LocaleOptionDto> SupportedLocales,
  string Notice);

public sealed record UserLocalePreferenceUpdateRequestDto(string Locale);

public sealed record SecuritySettingsDto(
  int SessionTimeoutMinutes,
  PasswordRuleSettingsDto PasswordRules,
  string MfaPolicy,
  IReadOnlyList<string> SupportedMfaPolicies,
  string? ConcurrencyToken);

public sealed record SecuritySettingsUpdateRequestDto(
  int SessionTimeoutMinutes,
  PasswordRuleSettingsDto PasswordRules,
  string MfaPolicy,
  string? ConcurrencyToken = null);

public sealed record PasswordRuleSettingsDto(
  int MinimumLength,
  bool RequireUppercase,
  bool RequireLowercase,
  bool RequireDigit,
  bool RequireSymbol);

public sealed record NotificationSettingsDto(
  IReadOnlyList<string> EnabledCategories,
  IReadOnlyList<string> EnabledChannels,
  bool InAppEnabled,
  bool EmailEnabled,
  bool WhatsAppEnabled,
  IReadOnlyList<NotificationSettingOptionDto> AvailableCategories,
  IReadOnlyList<NotificationSettingOptionDto> AvailableChannels,
  string? ConcurrencyToken);

public sealed record NotificationSettingsUpdateRequestDto(
  IReadOnlyList<string> EnabledCategories,
  IReadOnlyList<string> EnabledChannels,
  bool InAppEnabled,
  bool EmailEnabled,
  bool WhatsAppEnabled,
  string? ConcurrencyToken = null);

public sealed record NotificationSettingOptionDto(
  string Code,
  string Label,
  bool IsEnabled);

public sealed record OrganizationBrandingSettingsDto(
  string? DisplayName,
  string? LogoUrl,
  string? LogoAlt,
  BrandLogoMetadataDto? Logo,
  string? PrimaryColor,
  string? PrimaryForegroundColor,
  string? AccentColor,
  string? AccentForegroundColor,
  string? SupportEmail,
  string? SupportPhone,
  string? SupportUrl,
  string? ConcurrencyToken);

public sealed record OrganizationBrandingUpdateRequestDto(
  string? DisplayName,
  string? LogoUrl,
  string? LogoAlt,
  string? PrimaryColor,
  string? PrimaryForegroundColor,
  string? AccentColor,
  string? AccentForegroundColor,
  string? SupportEmail,
  string? SupportPhone,
  string? SupportUrl,
  string? ConcurrencyToken = null);

public sealed record BrandLogoMetadataDto(
  string FileName,
  string ContentType,
  long SizeBytes,
  int? Width,
  int? Height,
  string StorageKey);

public sealed record BrandLogoUploadRequestDto(
  string FileName,
  string ContentType,
  long SizeBytes,
  int Width,
  int Height,
  string LogoAlt,
  string ContentBase64,
  string? ConcurrencyToken = null);

public sealed record DomainCatalogSettingsDto(
  string CatalogType,
  string Label,
  IReadOnlyList<DomainCatalogItemDto> Items);

public sealed record DomainCatalogItemDto(
  Guid Id,
  string CatalogType,
  string Code,
  IReadOnlyDictionary<string, string> Labels,
  bool IsEnabled,
  bool IsSystem,
  int SortOrder,
  string? ConcurrencyToken);

public sealed record DomainCatalogUpdateRequestDto(
  IReadOnlyList<DomainCatalogItemUpdateRequestDto> Items);

public sealed record DomainCatalogItemUpdateRequestDto(
  string Code,
  IReadOnlyDictionary<string, string> Labels,
  bool IsEnabled = true,
  int SortOrder = 0,
  string? ConcurrencyToken = null);

#pragma warning restore CA1054, CA1056
