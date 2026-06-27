using Alsappan.Domain.Common.Identifiers;
using Alsappan.Domain.Common.Metadata;
using Alsappan.Domain.Identity;

namespace Alsappan.Domain.Settings;

#pragma warning disable CA1054, CA1056, CA1819

public sealed class OrganizationSettings : TenantScopedEntity<EntityId>
{
  public const int DefaultSessionTimeoutMinutes = 60;
  public const int DefaultPasswordMinimumLength = 8;
  public const string DefaultTimeZone = "America/Sao_Paulo";
  public const string DefaultMfaPolicy = "optional";

  private OrganizationSettings()
  {
  }

  private OrganizationSettings(
    EntityId id,
    OrganizationId organizationId,
    DateTimeOffset createdAt,
    UserId? createdByUserId)
    : base(id, organizationId, createdAt, createdByUserId)
  {
    DefaultLocale = IdentityDefaults.DefaultLocale;
    FallbackLocale = IdentityDefaults.DefaultLocale;
    EnabledLocales = [IdentityDefaults.DefaultLocale, "en-US"];
    TimeZone = DefaultTimeZone;
    SessionTimeoutMinutes = DefaultSessionTimeoutMinutes;
    PasswordMinimumLength = DefaultPasswordMinimumLength;
    PasswordRequireUppercase = true;
    PasswordRequireLowercase = true;
    PasswordRequireDigit = true;
    PasswordRequireSymbol = false;
    MfaPolicy = DefaultMfaPolicy;
    EnabledNotificationCategories =
    [
      "properties",
      "residents",
      "contracts",
      "payments",
      "utility-accounts",
      "documents",
      "pets",
      "vehicles",
      "occurrences",
      "inspections",
      "administrators",
      "settings",
      "system"
    ];
    EnabledNotificationChannels = ["in-app"];
    InAppNotificationsEnabled = true;
    EmailNotificationsEnabled = false;
    WhatsAppNotificationsEnabled = false;
    ResidentPortalEnabled = true;
    ResidentOccurrenceCreationEnabled = true;
    ResidentDocumentUploadEnabled = false;
    ResidentProfileUpdateRequestEnabled = true;
    AllowOrganizationSwitching = true;
    RequireActiveOrganization = true;
    StrictTenantIsolation = true;
  }

  public string? ContactEmail { get; private set; }

  public string? ContactPhone { get; private set; }

  public string? ContactWebsite { get; private set; }

  public string TimeZone { get; private set; } = DefaultTimeZone;

  public string DefaultLocale { get; private set; } = IdentityDefaults.DefaultLocale;

  public string FallbackLocale { get; private set; } = IdentityDefaults.DefaultLocale;

  public string[] EnabledLocales { get; private set; } = [IdentityDefaults.DefaultLocale, "en-US"];

  public bool AllowOrganizationSwitching { get; private set; }

  public bool RequireActiveOrganization { get; private set; }

  public bool StrictTenantIsolation { get; private set; }

  public bool ResidentPortalEnabled { get; private set; }

  public bool ResidentOccurrenceCreationEnabled { get; private set; }

  public bool ResidentDocumentUploadEnabled { get; private set; }

  public bool ResidentProfileUpdateRequestEnabled { get; private set; }

  public int SessionTimeoutMinutes { get; private set; } = DefaultSessionTimeoutMinutes;

  public int PasswordMinimumLength { get; private set; } = DefaultPasswordMinimumLength;

  public bool PasswordRequireUppercase { get; private set; }

  public bool PasswordRequireLowercase { get; private set; }

  public bool PasswordRequireDigit { get; private set; }

  public bool PasswordRequireSymbol { get; private set; }

  public string MfaPolicy { get; private set; } = DefaultMfaPolicy;

  public string[] EnabledNotificationCategories { get; private set; } = [];

  public string[] EnabledNotificationChannels { get; private set; } = [];

  public bool InAppNotificationsEnabled { get; private set; }

  public bool EmailNotificationsEnabled { get; private set; }

  public bool WhatsAppNotificationsEnabled { get; private set; }

  public string? BrandDisplayName { get; private set; }

  public string? LogoStorageKey { get; private set; }

  public string? LogoFileName { get; private set; }

  public string? LogoContentType { get; private set; }

  public long? LogoSizeBytes { get; private set; }

  public int? LogoWidth { get; private set; }

  public int? LogoHeight { get; private set; }

  public string? LogoAlt { get; private set; }

  public string? LogoUrl { get; private set; }

  public string? PrimaryColor { get; private set; }

  public string? PrimaryForegroundColor { get; private set; }

  public string? AccentColor { get; private set; }

  public string? AccentForegroundColor { get; private set; }

  public string? SupportEmail { get; private set; }

  public string? SupportPhone { get; private set; }

  public string? SupportUrl { get; private set; }

  public static OrganizationSettings Create(
    EntityId id,
    OrganizationId organizationId,
    DateTimeOffset createdAt,
    UserId? createdByUserId = null) =>
    new(id, organizationId, createdAt, createdByUserId);

  public void UpdateProfile(
    string? contactEmail,
    string? contactPhone,
    string? contactWebsite,
    string timeZone,
    DateTimeOffset updatedAt,
    UserId? updatedByUserId)
  {
    ContactEmail = SettingsCode.Optional(contactEmail, 320, nameof(contactEmail));
    ContactPhone = SettingsCode.Optional(contactPhone, 64, nameof(contactPhone));
    ContactWebsite = SettingsCode.Optional(contactWebsite, 400, nameof(contactWebsite));
    TimeZone = SettingsCode.Required(timeZone, nameof(timeZone), 96);
    MarkUpdated(updatedAt, updatedByUserId);
  }

  public void UpdateLocalization(
    string defaultLocale,
    string fallbackLocale,
    IEnumerable<string> enabledLocales,
    DateTimeOffset updatedAt,
    UserId? updatedByUserId)
  {
    DefaultLocale = SettingsCode.NormalizeLocale(defaultLocale);
    FallbackLocale = SettingsCode.NormalizeLocale(fallbackLocale);
    EnabledLocales = SettingsCode.NormalizeLocales(enabledLocales);
    MarkUpdated(updatedAt, updatedByUserId);
  }

  public void UpdateTenantBehavior(
    bool allowOrganizationSwitching,
    bool requireActiveOrganization,
    bool strictTenantIsolation,
    DateTimeOffset updatedAt,
    UserId? updatedByUserId)
  {
    AllowOrganizationSwitching = allowOrganizationSwitching;
    RequireActiveOrganization = requireActiveOrganization;
    StrictTenantIsolation = strictTenantIsolation;
    MarkUpdated(updatedAt, updatedByUserId);
  }

  public void UpdateResidentPortal(
    bool residentPortalEnabled,
    bool occurrenceCreationEnabled,
    bool documentUploadEnabled,
    bool profileUpdateRequestEnabled,
    DateTimeOffset updatedAt,
    UserId? updatedByUserId)
  {
    ResidentPortalEnabled = residentPortalEnabled;
    ResidentOccurrenceCreationEnabled = occurrenceCreationEnabled;
    ResidentDocumentUploadEnabled = documentUploadEnabled;
    ResidentProfileUpdateRequestEnabled = profileUpdateRequestEnabled;
    MarkUpdated(updatedAt, updatedByUserId);
  }

  public void UpdateSecurity(
    int sessionTimeoutMinutes,
    int passwordMinimumLength,
    bool passwordRequireUppercase,
    bool passwordRequireLowercase,
    bool passwordRequireDigit,
    bool passwordRequireSymbol,
    string mfaPolicy,
    DateTimeOffset updatedAt,
    UserId? updatedByUserId)
  {
    SessionTimeoutMinutes = sessionTimeoutMinutes;
    PasswordMinimumLength = passwordMinimumLength;
    PasswordRequireUppercase = passwordRequireUppercase;
    PasswordRequireLowercase = passwordRequireLowercase;
    PasswordRequireDigit = passwordRequireDigit;
    PasswordRequireSymbol = passwordRequireSymbol;
    MfaPolicy = SettingsCode.NormalizeCode(mfaPolicy);
    MarkUpdated(updatedAt, updatedByUserId);
  }

  public void UpdateNotifications(
    IEnumerable<string> enabledCategories,
    IEnumerable<string> enabledChannels,
    bool inAppEnabled,
    bool emailEnabled,
    bool whatsAppEnabled,
    DateTimeOffset updatedAt,
    UserId? updatedByUserId)
  {
    EnabledNotificationCategories = NormalizeTokens(enabledCategories);
    EnabledNotificationChannels = NormalizeTokens(enabledChannels);
    InAppNotificationsEnabled = inAppEnabled;
    EmailNotificationsEnabled = emailEnabled;
    WhatsAppNotificationsEnabled = whatsAppEnabled;
    MarkUpdated(updatedAt, updatedByUserId);
  }

  public void UpdateBranding(
    string? displayName,
    string? logoAlt,
    string? logoUrl,
    string? primaryColor,
    string? primaryForegroundColor,
    string? accentColor,
    string? accentForegroundColor,
    string? supportEmail,
    string? supportPhone,
    string? supportUrl,
    DateTimeOffset updatedAt,
    UserId? updatedByUserId)
  {
    BrandDisplayName = SettingsCode.Optional(displayName, 120, nameof(displayName));
    LogoAlt = SettingsCode.Optional(logoAlt, 140, nameof(logoAlt));
    LogoUrl = SettingsCode.Optional(logoUrl, 400, nameof(logoUrl));
    PrimaryColor = SettingsCode.Optional(primaryColor, 16, nameof(primaryColor));
    PrimaryForegroundColor = SettingsCode.Optional(primaryForegroundColor, 16, nameof(primaryForegroundColor));
    AccentColor = SettingsCode.Optional(accentColor, 16, nameof(accentColor));
    AccentForegroundColor = SettingsCode.Optional(accentForegroundColor, 16, nameof(accentForegroundColor));
    SupportEmail = SettingsCode.Optional(supportEmail, 320, nameof(supportEmail));
    SupportPhone = SettingsCode.Optional(supportPhone, 64, nameof(supportPhone));
    SupportUrl = SettingsCode.Optional(supportUrl, 400, nameof(supportUrl));
    MarkUpdated(updatedAt, updatedByUserId);
  }

  public void SetLogo(
    string storageKey,
    string fileName,
    string contentType,
    long sizeBytes,
    int? width,
    int? height,
    string logoAlt,
    DateTimeOffset updatedAt,
    UserId? updatedByUserId)
  {
    LogoStorageKey = SettingsCode.Required(storageKey, nameof(storageKey), 600);
    LogoFileName = SettingsCode.Required(fileName, nameof(fileName), 180);
    LogoContentType = SettingsCode.Required(contentType, nameof(contentType), 140);
    LogoSizeBytes = sizeBytes > 0
      ? sizeBytes
      : throw new ArgumentOutOfRangeException(nameof(sizeBytes), "Logo size must be positive.");
    LogoWidth = width;
    LogoHeight = height;
    LogoAlt = SettingsCode.Required(logoAlt, nameof(logoAlt), 140);
    LogoUrl = null;
    MarkUpdated(updatedAt, updatedByUserId);
  }

  public string? RemoveLogo(DateTimeOffset updatedAt, UserId? updatedByUserId)
  {
    var previousStorageKey = LogoStorageKey;
    LogoStorageKey = null;
    LogoFileName = null;
    LogoContentType = null;
    LogoSizeBytes = null;
    LogoWidth = null;
    LogoHeight = null;
    LogoAlt = null;
    LogoUrl = null;
    MarkUpdated(updatedAt, updatedByUserId);
    return previousStorageKey;
  }

  public string? ResetBranding(DateTimeOffset updatedAt, UserId? updatedByUserId)
  {
    var previousStorageKey = RemoveLogo(updatedAt, updatedByUserId);
    BrandDisplayName = null;
    PrimaryColor = null;
    PrimaryForegroundColor = null;
    AccentColor = null;
    AccentForegroundColor = null;
    SupportEmail = null;
    SupportPhone = null;
    SupportUrl = null;
    MarkUpdated(updatedAt, updatedByUserId);
    return previousStorageKey;
  }

  private static string[] NormalizeTokens(IEnumerable<string>? values)
  {
    var tokens = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    foreach (var value in values ?? [])
    {
      if (!string.IsNullOrWhiteSpace(value))
      {
        tokens.Add(SettingsCode.NormalizeCode(value));
      }
    }

    return tokens.Order(StringComparer.OrdinalIgnoreCase).ToArray();
  }
}

#pragma warning restore CA1054, CA1056, CA1819
