using System.Globalization;
using System.Text.RegularExpressions;
using Alsappan.Application.Common.Audit;
using Alsappan.Application.Common.Authorization;
using Alsappan.Application.Common.Files;
using Alsappan.Application.Common.Results;
using Alsappan.Application.Common.Tenancy;
using Alsappan.Application.Common.Validation;
using Alsappan.Application.Notifications;
using Alsappan.Application.Settings.Repositories;
using Alsappan.Domain.Common.Events;
using Alsappan.Domain.Common.Identifiers;
using Alsappan.Domain.Identity;
using Alsappan.Domain.Settings;

namespace Alsappan.Application.Settings;

public sealed partial class SettingsService : ISettingsService
{
  private readonly ISettingsRepository settingsRepository;
  private readonly IPermissionService permissionService;
  private readonly IActiveOrganizationContextResolver activeOrganizationContextResolver;
  private readonly IAuditWriter auditWriter;
  private readonly IFileStorageProvider fileStorageProvider;
  private readonly TimeProvider timeProvider;

  public SettingsService(
    ISettingsRepository settingsRepository,
    IPermissionService permissionService,
    IActiveOrganizationContextResolver activeOrganizationContextResolver,
    IAuditWriter auditWriter,
    IFileStorageProvider fileStorageProvider,
    TimeProvider? timeProvider = null)
  {
    this.settingsRepository = settingsRepository ?? throw new ArgumentNullException(nameof(settingsRepository));
    this.permissionService = permissionService ?? throw new ArgumentNullException(nameof(permissionService));
    this.activeOrganizationContextResolver = activeOrganizationContextResolver ??
      throw new ArgumentNullException(nameof(activeOrganizationContextResolver));
    this.auditWriter = auditWriter ?? throw new ArgumentNullException(nameof(auditWriter));
    this.fileStorageProvider = fileStorageProvider ?? throw new ArgumentNullException(nameof(fileStorageProvider));
    this.timeProvider = timeProvider ?? TimeProvider.System;
  }

  public async Task<ApplicationOperationResult<SettingsDashboardDto>> GetAsync(
    string? locale = null,
    CancellationToken cancellationToken = default)
  {
    var context = await AuthorizeContextAsync(
        PermissionCodes.Read(PermissionModules.Settings),
        cancellationToken)
      .ConfigureAwait(false);
    if (context is null)
    {
      return ApplicationOperationResult<SettingsDashboardDto>.Failed(ApplicationOperationFailure.Forbidden);
    }

    var organization = await settingsRepository.FindOrganizationAsync(context.OrganizationId, cancellationToken)
      .ConfigureAwait(false);
    if (organization is null)
    {
      return ApplicationOperationResult<SettingsDashboardDto>.Failed(ApplicationOperationFailure.NotFound);
    }

    var now = timeProvider.GetUtcNow();
    var settings = await settingsRepository.GetOrCreateSettingsAsync(
        context.OrganizationId,
        now,
        context.UserId,
        cancellationToken)
      .ConfigureAwait(false);
    await settingsRepository.EnsureCatalogDefaultsAsync(
        context.OrganizationId,
        now,
        context.UserId,
        cancellationToken: cancellationToken)
      .ConfigureAwait(false);

    var catalogs = await settingsRepository.ListCatalogItemsAsync(
        context.OrganizationId,
        cancellationToken: cancellationToken)
      .ConfigureAwait(false);
    var preference = await settingsRepository.FindUserLocalePreferenceAsync(
        context.OrganizationId,
        context.UserId,
        cancellationToken)
      .ConfigureAwait(false);

    return ApplicationOperationResult<SettingsDashboardDto>.Success(
      ToDashboard(organization, settings, catalogs, preference, locale));
  }

  public async Task<ApplicationOperationResult<OrganizationProfileSettingsDto>> UpdateOrganizationProfileAsync(
    OrganizationProfileUpdateRequestDto request,
    string? locale = null,
    CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(request);

    var validation = ValidateOrganizationProfile(request).ToList();
    if (validation.Count > 0)
    {
      return Invalid<OrganizationProfileSettingsDto>(validation);
    }

    var context = await AuthorizeContextAsync(
        PermissionCodes.Write(PermissionModules.Settings),
        cancellationToken)
      .ConfigureAwait(false);
    if (context is null)
    {
      return ApplicationOperationResult<OrganizationProfileSettingsDto>.Failed(ApplicationOperationFailure.Forbidden);
    }

    var organization = await settingsRepository.FindOrganizationAsync(context.OrganizationId, cancellationToken)
      .ConfigureAwait(false);
    if (organization is null)
    {
      return ApplicationOperationResult<OrganizationProfileSettingsDto>.Failed(ApplicationOperationFailure.NotFound);
    }

    var now = timeProvider.GetUtcNow();
    var settings = await settingsRepository.GetOrCreateSettingsAsync(
        context.OrganizationId,
        now,
        context.UserId,
        cancellationToken)
      .ConfigureAwait(false);
    var concurrencyFailure = EnsureCurrentConcurrencyToken(settings, request.ConcurrencyToken);
    if (concurrencyFailure is not null)
    {
      return ApplicationOperationResult<OrganizationProfileSettingsDto>.Failed(
        ApplicationOperationFailure.Conflict,
        concurrencyFailure);
    }

    organization.Rename(request.Slug, request.Name, request.DisplayName, now, context.UserId);
    organization.ChangeLocalization(settings.DefaultLocale, request.CurrencyCode, now, context.UserId);
    settings.UpdateProfile(
      request.ContactEmail,
      request.ContactPhone,
      request.ContactWebsite,
      request.TimeZone,
      now,
      context.UserId);

    await settingsRepository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    await WriteAuditAsync(
        "settings.organization.updated",
        context,
        organization.DisplayName,
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
          ["displayName"] = organization.DisplayName,
          ["timeZone"] = settings.TimeZone,
          ["currency"] = organization.Currency
        },
        cancellationToken)
      .ConfigureAwait(false);

    return ApplicationOperationResult<OrganizationProfileSettingsDto>.Success(
      ToOrganizationProfile(organization, settings));
  }

  public async Task<ApplicationOperationResult<LocalizationSettingsDto>> UpdateLocalizationAsync(
    LocalizationUpdateRequestDto request,
    string? locale = null,
    CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(request);

    var validation = ValidateLocalization(request).ToList();
    if (validation.Count > 0)
    {
      return Invalid<LocalizationSettingsDto>(validation);
    }

    var context = await AuthorizeContextAsync(
        PermissionCodes.Write(PermissionModules.Settings),
        cancellationToken)
      .ConfigureAwait(false);
    if (context is null)
    {
      return ApplicationOperationResult<LocalizationSettingsDto>.Failed(ApplicationOperationFailure.Forbidden);
    }

    var organization = await settingsRepository.FindOrganizationAsync(context.OrganizationId, cancellationToken)
      .ConfigureAwait(false);
    if (organization is null)
    {
      return ApplicationOperationResult<LocalizationSettingsDto>.Failed(ApplicationOperationFailure.NotFound);
    }

    var now = timeProvider.GetUtcNow();
    var settings = await settingsRepository.GetOrCreateSettingsAsync(
        context.OrganizationId,
        now,
        context.UserId,
        cancellationToken)
      .ConfigureAwait(false);
    var concurrencyFailure = EnsureCurrentConcurrencyToken(settings, request.ConcurrencyToken);
    if (concurrencyFailure is not null)
    {
      return ApplicationOperationResult<LocalizationSettingsDto>.Failed(
        ApplicationOperationFailure.Conflict,
        concurrencyFailure);
    }

    settings.UpdateLocalization(
      request.DefaultLocale,
      request.FallbackLocale,
      request.EnabledLocales,
      now,
      context.UserId);
    organization.ChangeLocalization(settings.DefaultLocale, organization.Currency, now, context.UserId);

    await settingsRepository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    await WriteAuditAsync(
        "settings.localization.updated",
        context,
        organization.DisplayName,
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
          ["defaultLocale"] = settings.DefaultLocale,
          ["fallbackLocale"] = settings.FallbackLocale,
          ["enabledLocales"] = string.Join(',', settings.EnabledLocales)
        },
        cancellationToken)
      .ConfigureAwait(false);

    return ApplicationOperationResult<LocalizationSettingsDto>.Success(ToLocalization(settings, locale));
  }

  public async Task<ApplicationOperationResult<UserLocalePreferenceDto>> GetUserLocalePreferenceAsync(
    string? locale = null,
    CancellationToken cancellationToken = default)
  {
    var context = await AuthorizeContextAsync(
        PermissionCodes.Read(PermissionModules.Settings),
        cancellationToken)
      .ConfigureAwait(false);
    if (context is null)
    {
      return ApplicationOperationResult<UserLocalePreferenceDto>.Failed(ApplicationOperationFailure.Forbidden);
    }

    var settings = await settingsRepository.GetOrCreateSettingsAsync(
        context.OrganizationId,
        timeProvider.GetUtcNow(),
        context.UserId,
        cancellationToken)
      .ConfigureAwait(false);
    var preference = await settingsRepository.FindUserLocalePreferenceAsync(
        context.OrganizationId,
        context.UserId,
        cancellationToken)
      .ConfigureAwait(false);

    return ApplicationOperationResult<UserLocalePreferenceDto>.Success(
      ToUserLocalePreference(settings, preference, locale));
  }

  public async Task<ApplicationOperationResult<UserLocalePreferenceDto>> UpdateUserLocalePreferenceAsync(
    UserLocalePreferenceUpdateRequestDto request,
    string? locale = null,
    CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(request);

    var context = await AuthorizeContextAsync(
        PermissionCodes.Read(PermissionModules.Settings),
        cancellationToken)
      .ConfigureAwait(false);
    if (context is null)
    {
      return ApplicationOperationResult<UserLocalePreferenceDto>.Failed(ApplicationOperationFailure.Forbidden);
    }

    var now = timeProvider.GetUtcNow();
    var settings = await settingsRepository.GetOrCreateSettingsAsync(
        context.OrganizationId,
        now,
        context.UserId,
        cancellationToken)
      .ConfigureAwait(false);
    var validation = ValidateUserLocalePreference(request, settings).ToList();
    if (validation.Count > 0)
    {
      return Invalid<UserLocalePreferenceDto>(validation);
    }

    var preference = await settingsRepository.FindUserLocalePreferenceAsync(
        context.OrganizationId,
        context.UserId,
        cancellationToken)
      .ConfigureAwait(false);
    if (preference is null)
    {
      preference = UserLocalePreference.Create(
        EntityId.New(),
        context.OrganizationId,
        context.UserId,
        request.Locale,
        now,
        context.UserId);
      settingsRepository.AddUserLocalePreference(preference);
    }
    else
    {
      preference.UpdateLocale(request.Locale, now, context.UserId);
    }

    await settingsRepository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

    return ApplicationOperationResult<UserLocalePreferenceDto>.Success(
      ToUserLocalePreference(settings, preference, locale));
  }

  public Task<ApplicationOperationResult<TenantBehaviorSettingsDto>> UpdateTenantBehaviorAsync(
    TenantBehaviorUpdateRequestDto request,
    string? locale = null,
    CancellationToken cancellationToken = default) =>
    UpdateSettingsAsync(
      request,
      PermissionCodes.Manage(PermissionModules.Settings),
      "settings.tenant-behavior.updated",
      static (settings, update, now, userId) =>
        settings.UpdateTenantBehavior(
          update.AllowOrganizationSwitching,
          update.RequireActiveOrganization,
          update.StrictTenantIsolation,
          now,
          userId),
      static settings => ToTenantBehavior(settings),
      static update => ValidateConcurrencyOnly(update.ConcurrencyToken),
      update => update.ConcurrencyToken,
      cancellationToken);

  public Task<ApplicationOperationResult<ResidentPortalSettingsDto>> UpdateResidentPortalAsync(
    ResidentPortalUpdateRequestDto request,
    string? locale = null,
    CancellationToken cancellationToken = default) =>
    UpdateSettingsAsync(
      request,
      PermissionCodes.Write(PermissionModules.Settings),
      "settings.resident-portal.updated",
      static (settings, update, now, userId) =>
        settings.UpdateResidentPortal(
          update.IsEnabled,
          update.AllowOccurrenceCreation,
          update.AllowDocumentUpload,
          update.AllowProfileUpdateRequests,
          now,
          userId),
      static settings => ToResidentPortal(settings),
      static update => ValidateConcurrencyOnly(update.ConcurrencyToken),
      update => update.ConcurrencyToken,
      cancellationToken);

  public async Task<ApplicationOperationResult<SecuritySettingsDto>> UpdateSecurityAsync(
    SecuritySettingsUpdateRequestDto request,
    string? locale = null,
    CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(request);

    return await UpdateSettingsAsync(
        request,
        PermissionCodes.Manage(PermissionModules.Settings),
        "settings.security.updated",
        static (settings, update, now, userId) =>
          settings.UpdateSecurity(
            update.SessionTimeoutMinutes,
            update.PasswordRules.MinimumLength,
            update.PasswordRules.RequireUppercase,
            update.PasswordRules.RequireLowercase,
            update.PasswordRules.RequireDigit,
            update.PasswordRules.RequireSymbol,
            update.MfaPolicy,
            now,
            userId),
        static settings => ToSecurity(settings),
        ValidateSecurity,
        update => update.ConcurrencyToken,
        cancellationToken)
      .ConfigureAwait(false);
  }

  public async Task<ApplicationOperationResult<NotificationSettingsDto>> UpdateNotificationsAsync(
    NotificationSettingsUpdateRequestDto request,
    string? locale = null,
    CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(request);

    return await UpdateSettingsAsync(
        request,
        PermissionCodes.Manage(PermissionModules.Settings),
        "settings.notifications.updated",
        static (settings, update, now, userId) =>
          settings.UpdateNotifications(
            update.EnabledCategories,
            update.EnabledChannels,
            update.InAppEnabled,
            update.EmailEnabled,
            update.WhatsAppEnabled,
            now,
            userId),
        settings => ToNotifications(settings, locale),
        ValidateNotifications,
        update => update.ConcurrencyToken,
        cancellationToken)
      .ConfigureAwait(false);
  }

  public async Task<ApplicationOperationResult<IReadOnlyList<DomainCatalogSettingsDto>>> ListCatalogsAsync(
    string? catalogType = null,
    string? locale = null,
    CancellationToken cancellationToken = default)
  {
    if (!string.IsNullOrWhiteSpace(catalogType) && !SettingsCatalog.IsKnownCatalogType(catalogType))
    {
      return Invalid<IReadOnlyList<DomainCatalogSettingsDto>>(
        [new ValidationFailure(nameof(catalogType), "validation.catalogType")]);
    }

    var context = await AuthorizeContextAsync(
        PermissionCodes.Read(PermissionModules.Settings),
        cancellationToken)
      .ConfigureAwait(false);
    if (context is null)
    {
      return ApplicationOperationResult<IReadOnlyList<DomainCatalogSettingsDto>>.Failed(
        ApplicationOperationFailure.Forbidden);
    }

    var normalizedCatalogType = string.IsNullOrWhiteSpace(catalogType)
      ? null
      : SettingsCatalog.NormalizeCatalogType(catalogType);
    var now = timeProvider.GetUtcNow();
    await settingsRepository.EnsureCatalogDefaultsAsync(
        context.OrganizationId,
        now,
        context.UserId,
        normalizedCatalogType,
        cancellationToken)
      .ConfigureAwait(false);
    var items = await settingsRepository.ListCatalogItemsAsync(
        context.OrganizationId,
        normalizedCatalogType,
        cancellationToken)
      .ConfigureAwait(false);

    return ApplicationOperationResult<IReadOnlyList<DomainCatalogSettingsDto>>.Success(
      ToCatalogGroups(items, locale));
  }

  public async Task<ApplicationOperationResult<DomainCatalogSettingsDto>> UpdateCatalogAsync(
    string catalogType,
    DomainCatalogUpdateRequestDto request,
    string? locale = null,
    CancellationToken cancellationToken = default)
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(catalogType);
    ArgumentNullException.ThrowIfNull(request);

    var normalizedCatalogType = SettingsCatalog.NormalizeCatalogType(catalogType);
    var validation = ValidateCatalogUpdate(normalizedCatalogType, request).ToList();
    if (validation.Count > 0)
    {
      return Invalid<DomainCatalogSettingsDto>(validation);
    }

    var context = await AuthorizeContextAsync(
        PermissionCodes.Manage(PermissionModules.Settings),
        cancellationToken)
      .ConfigureAwait(false);
    if (context is null)
    {
      return ApplicationOperationResult<DomainCatalogSettingsDto>.Failed(ApplicationOperationFailure.Forbidden);
    }

    var now = timeProvider.GetUtcNow();
    await settingsRepository.EnsureCatalogDefaultsAsync(
        context.OrganizationId,
        now,
        context.UserId,
        normalizedCatalogType,
        cancellationToken)
      .ConfigureAwait(false);
    var existing = await settingsRepository.ListCatalogItemsAsync(
        context.OrganizationId,
        normalizedCatalogType,
        cancellationToken)
      .ConfigureAwait(false);
    var existingByCode = existing.ToDictionary(item => item.Code, StringComparer.OrdinalIgnoreCase);

    foreach (var item in request.Items)
    {
      var code = SettingsCode.NormalizeCode(item.Code);
      var ptBr = item.Labels.TryGetValue("pt-BR", out var pt) ? pt : string.Empty;
      var enUs = item.Labels.TryGetValue("en-US", out var en) ? en : string.Empty;
      if (existingByCode.TryGetValue(code, out var current))
      {
        var concurrencyFailure = EnsureCurrentConcurrencyToken(current, item.ConcurrencyToken);
        if (concurrencyFailure is not null)
        {
          return ApplicationOperationResult<DomainCatalogSettingsDto>.Failed(
            ApplicationOperationFailure.Conflict,
            concurrencyFailure);
        }

        current.Update(ptBr, enUs, item.SortOrder, item.IsEnabled, now, context.UserId);
        continue;
      }

      settingsRepository.AddCatalogItem(DomainCatalogSetting.Create(
        EntityId.New(),
        context.OrganizationId,
        normalizedCatalogType,
        code,
        ptBr,
        enUs,
        item.SortOrder,
        item.IsEnabled,
        isSystem: false,
        now,
        context.UserId));
    }

    await settingsRepository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    await WriteAuditAsync(
        "settings.catalog.updated",
        context,
        normalizedCatalogType,
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
          ["catalogType"] = normalizedCatalogType,
          ["itemCount"] = request.Items.Count.ToString(CultureInfo.InvariantCulture)
        },
        cancellationToken)
      .ConfigureAwait(false);

    var updated = await settingsRepository.ListCatalogItemsAsync(
        context.OrganizationId,
        normalizedCatalogType,
        cancellationToken)
      .ConfigureAwait(false);
    return ApplicationOperationResult<DomainCatalogSettingsDto>.Success(
      ToCatalogGroups(updated, locale).Single());
  }

  public async Task<ApplicationOperationResult<OrganizationBrandingSettingsDto>> UpdateBrandingAsync(
    OrganizationBrandingUpdateRequestDto request,
    string? locale = null,
    CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(request);

    var validation = ValidateBranding(request).ToList();
    if (validation.Count > 0)
    {
      return Invalid<OrganizationBrandingSettingsDto>(validation);
    }

    var context = await AuthorizeContextAsync(
        PermissionCodes.Write(PermissionModules.Settings),
        cancellationToken)
      .ConfigureAwait(false);
    if (context is null)
    {
      return ApplicationOperationResult<OrganizationBrandingSettingsDto>.Failed(ApplicationOperationFailure.Forbidden);
    }

    var now = timeProvider.GetUtcNow();
    var settings = await settingsRepository.GetOrCreateSettingsAsync(
        context.OrganizationId,
        now,
        context.UserId,
        cancellationToken)
      .ConfigureAwait(false);
    var concurrencyFailure = EnsureCurrentConcurrencyToken(settings, request.ConcurrencyToken);
    if (concurrencyFailure is not null)
    {
      return ApplicationOperationResult<OrganizationBrandingSettingsDto>.Failed(
        ApplicationOperationFailure.Conflict,
        concurrencyFailure);
    }

    settings.UpdateBranding(
      request.DisplayName,
      request.LogoAlt,
      request.LogoUrl,
      NormalizeColor(request.PrimaryColor),
      NormalizeColor(request.PrimaryForegroundColor),
      NormalizeColor(request.AccentColor),
      NormalizeColor(request.AccentForegroundColor),
      request.SupportEmail,
      request.SupportPhone,
      request.SupportUrl,
      now,
      context.UserId);

    await settingsRepository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    await WriteAuditAsync(
        "settings.branding.updated",
        context,
        settings.BrandDisplayName ?? "branding",
        ToBrandingAuditData(settings),
        cancellationToken)
      .ConfigureAwait(false);

    return ApplicationOperationResult<OrganizationBrandingSettingsDto>.Success(ToBranding(settings));
  }

  public async Task<ApplicationOperationResult<OrganizationBrandingSettingsDto>> UploadLogoAsync(
    BrandLogoUploadRequestDto request,
    string? locale = null,
    CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(request);

    var validation = ValidateLogo(request).ToList();
    if (validation.Count > 0)
    {
      return Invalid<OrganizationBrandingSettingsDto>(validation);
    }

    var context = await AuthorizeContextAsync(
        PermissionCodes.Manage(PermissionModules.Settings),
        cancellationToken)
      .ConfigureAwait(false);
    if (context is null)
    {
      return ApplicationOperationResult<OrganizationBrandingSettingsDto>.Failed(ApplicationOperationFailure.Forbidden);
    }

    var now = timeProvider.GetUtcNow();
    var settings = await settingsRepository.GetOrCreateSettingsAsync(
        context.OrganizationId,
        now,
        context.UserId,
        cancellationToken)
      .ConfigureAwait(false);
    var concurrencyFailure = EnsureCurrentConcurrencyToken(settings, request.ConcurrencyToken);
    if (concurrencyFailure is not null)
    {
      return ApplicationOperationResult<OrganizationBrandingSettingsDto>.Failed(
        ApplicationOperationFailure.Conflict,
        concurrencyFailure);
    }

    var content = Convert.FromBase64String(request.ContentBase64);
    using var stream = new MemoryStream(content, writable: false);
    var stored = await fileStorageProvider.SaveAsync(
        new FileStorageRequest(
          context.OrganizationId,
          request.FileName,
          request.ContentType,
          stream,
          new Dictionary<string, string>(StringComparer.Ordinal)
          {
            ["assetType"] = "organization-logo",
            ["width"] = request.Width.ToString(CultureInfo.InvariantCulture),
            ["height"] = request.Height.ToString(CultureInfo.InvariantCulture)
          }),
        cancellationToken)
      .ConfigureAwait(false);

    var previousStorageKey = settings.LogoStorageKey;
    settings.SetLogo(
      stored.StorageKey,
      stored.FileName,
      stored.ContentType,
      stored.Length,
      request.Width,
      request.Height,
      request.LogoAlt,
      now,
      context.UserId);
    await settingsRepository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    if (!string.IsNullOrWhiteSpace(previousStorageKey))
    {
      await fileStorageProvider.DeleteAsync(context.OrganizationId, previousStorageKey, cancellationToken)
        .ConfigureAwait(false);
    }

    await WriteAuditAsync(
        "settings.branding.logo.uploaded",
        context,
        settings.LogoFileName ?? "logo",
        ToBrandingAuditData(settings),
        cancellationToken)
      .ConfigureAwait(false);

    return ApplicationOperationResult<OrganizationBrandingSettingsDto>.Success(ToBranding(settings));
  }

  public async Task<ApplicationOperationResult<OrganizationBrandingSettingsDto>> RemoveLogoAsync(
    string? locale = null,
    CancellationToken cancellationToken = default)
  {
    var result = await MutateBrandingLogoAsync(
        "settings.branding.logo.removed",
        static (settings, now, userId) => settings.RemoveLogo(now, userId),
        cancellationToken)
      .ConfigureAwait(false);
    return result;
  }

  public async Task<ApplicationOperationResult<OrganizationBrandingSettingsDto>> ResetBrandingAsync(
    string? locale = null,
    CancellationToken cancellationToken = default)
  {
    var result = await MutateBrandingLogoAsync(
        "settings.branding.reset",
        static (settings, now, userId) => settings.ResetBranding(now, userId),
        cancellationToken)
      .ConfigureAwait(false);
    return result;
  }

  private async Task<ApplicationOperationResult<OrganizationBrandingSettingsDto>> MutateBrandingLogoAsync(
    string action,
    Func<OrganizationSettings, DateTimeOffset, UserId?, string?> mutate,
    CancellationToken cancellationToken)
  {
    var context = await AuthorizeContextAsync(
        PermissionCodes.Manage(PermissionModules.Settings),
        cancellationToken)
      .ConfigureAwait(false);
    if (context is null)
    {
      return ApplicationOperationResult<OrganizationBrandingSettingsDto>.Failed(ApplicationOperationFailure.Forbidden);
    }

    var now = timeProvider.GetUtcNow();
    var settings = await settingsRepository.GetOrCreateSettingsAsync(
        context.OrganizationId,
        now,
        context.UserId,
        cancellationToken)
      .ConfigureAwait(false);
    var previousStorageKey = mutate(settings, now, context.UserId);
    await settingsRepository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    if (!string.IsNullOrWhiteSpace(previousStorageKey))
    {
      await fileStorageProvider.DeleteAsync(context.OrganizationId, previousStorageKey, cancellationToken)
        .ConfigureAwait(false);
    }

    await WriteAuditAsync(
        action,
        context,
        settings.BrandDisplayName ?? "branding",
        ToBrandingAuditData(settings),
        cancellationToken)
      .ConfigureAwait(false);

    return ApplicationOperationResult<OrganizationBrandingSettingsDto>.Success(ToBranding(settings));
  }

  private async Task<ApplicationOperationResult<TResponse>> UpdateSettingsAsync<TRequest, TResponse>(
    TRequest request,
    string permissionCode,
    string auditAction,
    Action<OrganizationSettings, TRequest, DateTimeOffset, UserId?> mutate,
    Func<OrganizationSettings, TResponse> map,
    Func<TRequest, IEnumerable<ValidationFailure>> validate,
    Func<TRequest, string?> concurrencyToken,
    CancellationToken cancellationToken)
  {
    ArgumentNullException.ThrowIfNull(request);

    var validation = validate(request).ToList();
    if (validation.Count > 0)
    {
      return Invalid<TResponse>(validation);
    }

    var context = await AuthorizeContextAsync(permissionCode, cancellationToken).ConfigureAwait(false);
    if (context is null)
    {
      return ApplicationOperationResult<TResponse>.Failed(ApplicationOperationFailure.Forbidden);
    }

    var now = timeProvider.GetUtcNow();
    var settings = await settingsRepository.GetOrCreateSettingsAsync(
        context.OrganizationId,
        now,
        context.UserId,
        cancellationToken)
      .ConfigureAwait(false);
    var concurrencyFailure = EnsureCurrentConcurrencyToken(settings, concurrencyToken(request));
    if (concurrencyFailure is not null)
    {
      return ApplicationOperationResult<TResponse>.Failed(ApplicationOperationFailure.Conflict, concurrencyFailure);
    }

    mutate(settings, request, now, context.UserId);
    await settingsRepository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    await WriteAuditAsync(
        auditAction,
        context,
        "settings",
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
          ["settingsId"] = settings.Id.Value.ToString("D")
        },
        cancellationToken)
      .ConfigureAwait(false);

    return ApplicationOperationResult<TResponse>.Success(map(settings));
  }

  private async Task<ActiveOrganizationContext?> AuthorizeContextAsync(
    string permissionCode,
    CancellationToken cancellationToken)
  {
    var permission = await permissionService.AuthorizeAsync(permissionCode, cancellationToken)
      .ConfigureAwait(false);
    if (!permission.IsGranted)
    {
      return null;
    }

    var context = await activeOrganizationContextResolver.ResolveAsync(cancellationToken)
      .ConfigureAwait(false);
    return context.Succeeded ? context.Context : null;
  }

  private async Task WriteAuditAsync(
    string action,
    ActiveOrganizationContext context,
    string? targetDisplayName,
    IReadOnlyDictionary<string, string> changedFields,
    CancellationToken cancellationToken)
  {
    var now = timeProvider.GetUtcNow();
    await auditWriter.WriteAsync(
        new AuditEntryDraft(
          context.OrganizationId,
          action,
          AuditEntryCategory.Mutation,
          EventActor.User(context.UserId, context.User.DisplayName),
          EntityReference.FromGuid("settings", context.OrganizationId.Value, targetDisplayName),
          now,
          changedFields,
          new Dictionary<string, string>(StringComparer.Ordinal)
          {
            ["module"] = PermissionModules.Settings
          }),
        cancellationToken)
      .ConfigureAwait(false);
  }

  private static SettingsDashboardDto ToDashboard(
    IdentityOrganization organization,
    OrganizationSettings settings,
    IReadOnlyList<DomainCatalogSetting> catalogItems,
    UserLocalePreference? preference,
    string? locale) =>
    new(
      ToOrganizationProfile(organization, settings),
      ToTenantBehavior(settings),
      ToResidentPortal(settings),
      ToLocalization(settings, locale),
      ToSecurity(settings),
      ToNotifications(settings, locale),
      ToBranding(settings),
      ToUserLocalePreference(settings, preference, locale),
      ToCatalogGroups(catalogItems, locale));

  private static OrganizationProfileSettingsDto ToOrganizationProfile(
    IdentityOrganization organization,
    OrganizationSettings settings) =>
    new(
      organization.Id.Value,
      organization.Slug,
      organization.Name,
      organization.DisplayName,
      organization.Currency,
      settings.TimeZone,
      settings.ContactEmail,
      settings.ContactPhone,
      settings.ContactWebsite,
      organization.CreatedAt,
      organization.UpdatedAt ?? settings.UpdatedAt,
      settings.ConcurrencyToken.Value);

  private static TenantBehaviorSettingsDto ToTenantBehavior(OrganizationSettings settings) =>
    new(
      settings.AllowOrganizationSwitching,
      settings.RequireActiveOrganization,
      settings.StrictTenantIsolation,
      settings.ConcurrencyToken.Value);

  private static ResidentPortalSettingsDto ToResidentPortal(OrganizationSettings settings) =>
    new(
      settings.ResidentPortalEnabled,
      settings.ResidentOccurrenceCreationEnabled,
      settings.ResidentDocumentUploadEnabled,
      settings.ResidentProfileUpdateRequestEnabled,
      settings.ConcurrencyToken.Value);

  private static LocalizationSettingsDto ToLocalization(OrganizationSettings settings, string? locale) =>
    new(
      settings.DefaultLocale,
      settings.FallbackLocale,
      settings.EnabledLocales,
      SettingsCatalog.BuildLocaleOptions(settings.EnabledLocales, settings.DefaultLocale, settings.FallbackLocale, locale),
      settings.ConcurrencyToken.Value);

  private static SecuritySettingsDto ToSecurity(OrganizationSettings settings) =>
    new(
      settings.SessionTimeoutMinutes,
      new PasswordRuleSettingsDto(
        settings.PasswordMinimumLength,
        settings.PasswordRequireUppercase,
        settings.PasswordRequireLowercase,
        settings.PasswordRequireDigit,
        settings.PasswordRequireSymbol),
      settings.MfaPolicy,
      SettingsCatalog.SupportedMfaPolicies,
      settings.ConcurrencyToken.Value);

  private static NotificationSettingsDto ToNotifications(OrganizationSettings settings, string? locale)
  {
    var categories = SettingsCatalog.BuildNotificationOptions(
      SettingsCatalog.NotificationCategories,
      settings.EnabledNotificationCategories,
      code => NotificationCatalog.GetCategoryLabel(code, locale).Label);
    var channels = SettingsCatalog.BuildNotificationOptions(
      SettingsCatalog.NotificationChannels,
      settings.EnabledNotificationChannels,
      code => NotificationCatalog.GetChannelLabel(code, locale).Label);

    return new NotificationSettingsDto(
      settings.EnabledNotificationCategories,
      settings.EnabledNotificationChannels,
      settings.InAppNotificationsEnabled,
      settings.EmailNotificationsEnabled,
      settings.WhatsAppNotificationsEnabled,
      categories,
      channels,
      settings.ConcurrencyToken.Value);
  }

  private static OrganizationBrandingSettingsDto ToBranding(OrganizationSettings settings) =>
    new(
      settings.BrandDisplayName,
      settings.LogoUrl,
      settings.LogoAlt,
      string.IsNullOrWhiteSpace(settings.LogoStorageKey) ||
        string.IsNullOrWhiteSpace(settings.LogoFileName) ||
        string.IsNullOrWhiteSpace(settings.LogoContentType) ||
        !settings.LogoSizeBytes.HasValue
          ? null
          : new BrandLogoMetadataDto(
            settings.LogoFileName,
            settings.LogoContentType,
            settings.LogoSizeBytes.Value,
            settings.LogoWidth,
            settings.LogoHeight,
            settings.LogoStorageKey),
      settings.PrimaryColor,
      settings.PrimaryForegroundColor,
      settings.AccentColor,
      settings.AccentForegroundColor,
      settings.SupportEmail,
      settings.SupportPhone,
      settings.SupportUrl,
      settings.ConcurrencyToken.Value);

  private static UserLocalePreferenceDto ToUserLocalePreference(
    OrganizationSettings settings,
    UserLocalePreference? preference,
    string? locale)
  {
    var effectiveLocale = preference?.Locale ?? settings.DefaultLocale;
    var portuguese = SettingsCatalog.IsPortuguese(locale);
    return new UserLocalePreferenceDto(
      preference?.Locale ?? string.Empty,
      effectiveLocale,
      preference is not null,
      SettingsCatalog.BuildLocaleOptions(settings.EnabledLocales, settings.DefaultLocale, settings.FallbackLocale, locale),
      preference is null
        ? portuguese
          ? "Locale padrao da organizacao aplicado."
          : "Organization default locale is applied."
        : portuguese
          ? "Preferencia de locale salva para este usuario."
          : "Locale preference is saved for this user.");
  }

  private static DomainCatalogSettingsDto[] ToCatalogGroups(
    IReadOnlyList<DomainCatalogSetting> items,
    string? locale) =>
    items
      .GroupBy(item => item.CatalogType, StringComparer.Ordinal)
      .OrderBy(group => group.Key, StringComparer.Ordinal)
      .Select(group => new DomainCatalogSettingsDto(
        group.Key,
        SettingsCatalog.CatalogTypeLabel(group.Key, locale),
        group
          .OrderBy(item => item.SortOrder)
          .ThenBy(item => item.Code, StringComparer.Ordinal)
          .Select(ToCatalogItem)
          .ToArray()))
      .ToArray();

  private static DomainCatalogItemDto ToCatalogItem(DomainCatalogSetting item) =>
    new(
      item.Id.Value,
      item.CatalogType,
      item.Code,
      new Dictionary<string, string>(StringComparer.Ordinal)
      {
        ["pt-BR"] = item.LabelPtBr,
        ["en-US"] = item.LabelEnUs
      },
      item.IsEnabled,
      item.IsSystem,
      item.SortOrder,
      item.ConcurrencyToken.Value);

  private static IEnumerable<ValidationFailure> ValidateOrganizationProfile(
    OrganizationProfileUpdateRequestDto request)
  {
    if (string.IsNullOrWhiteSpace(request.Slug))
    {
      yield return new ValidationFailure(nameof(request.Slug), ValidationMessageKeys.Required);
    }
    else if (request.Slug.Trim().Length > 96)
    {
      yield return new ValidationFailure(nameof(request.Slug), ValidationMessageKeys.MaxLength);
    }
    else if (!request.Slug.Trim().Any(char.IsLetterOrDigit))
    {
      yield return new ValidationFailure(nameof(request.Slug), "validation.slug");
    }

    if (string.IsNullOrWhiteSpace(request.Name))
    {
      yield return new ValidationFailure(nameof(request.Name), ValidationMessageKeys.Required);
    }
    else if (request.Name.Trim().Length > 180)
    {
      yield return new ValidationFailure(nameof(request.Name), ValidationMessageKeys.MaxLength);
    }

    if (string.IsNullOrWhiteSpace(request.DisplayName))
    {
      yield return new ValidationFailure(nameof(request.DisplayName), ValidationMessageKeys.Required);
    }
    else if (request.DisplayName.Trim().Length > 180)
    {
      yield return new ValidationFailure(nameof(request.DisplayName), ValidationMessageKeys.MaxLength);
    }

    if (string.IsNullOrWhiteSpace(request.CurrencyCode) ||
      request.CurrencyCode.Trim().Length != 3 ||
      request.CurrencyCode.Trim().Any(character => !char.IsAsciiLetter(character)))
    {
      yield return new ValidationFailure(nameof(request.CurrencyCode), ValidationMessageKeys.CurrencyCode);
    }

    if (string.IsNullOrWhiteSpace(request.TimeZone))
    {
      yield return new ValidationFailure(nameof(request.TimeZone), ValidationMessageKeys.Required);
    }
    else if (request.TimeZone.Trim().Length > 96)
    {
      yield return new ValidationFailure(nameof(request.TimeZone), ValidationMessageKeys.MaxLength);
    }

    if (!string.IsNullOrWhiteSpace(request.ContactEmail) &&
      !request.ContactEmail.Contains('@', StringComparison.Ordinal))
    {
      yield return new ValidationFailure(nameof(request.ContactEmail), ValidationMessageKeys.Email);
    }
    else if (request.ContactEmail?.Trim().Length > 320)
    {
      yield return new ValidationFailure(nameof(request.ContactEmail), ValidationMessageKeys.MaxLength);
    }

    if (request.ContactPhone?.Trim().Length > 64)
    {
      yield return new ValidationFailure(nameof(request.ContactPhone), ValidationMessageKeys.MaxLength);
    }

    if (!IsValidAbsoluteUrl(request.ContactWebsite))
    {
      yield return new ValidationFailure(nameof(request.ContactWebsite), "validation.url");
    }
    else if (request.ContactWebsite?.Trim().Length > 400)
    {
      yield return new ValidationFailure(nameof(request.ContactWebsite), ValidationMessageKeys.MaxLength);
    }
  }

  private static IEnumerable<ValidationFailure> ValidateLocalization(LocalizationUpdateRequestDto request)
  {
    var enabled = new HashSet<string>(StringComparer.Ordinal);
    if (request.EnabledLocales is null || request.EnabledLocales.Count == 0)
    {
      yield return new ValidationFailure(nameof(request.EnabledLocales), ValidationMessageKeys.Required);
      yield break;
    }

    foreach (var locale in request.EnabledLocales)
    {
      if (!IsSupportedLocale(locale, out var normalized))
      {
        yield return new ValidationFailure(nameof(request.EnabledLocales), "validation.locale");
        continue;
      }

      enabled.Add(normalized);
    }

    if (!IsSupportedLocale(request.DefaultLocale, out var defaultLocale) || !enabled.Contains(defaultLocale))
    {
      yield return new ValidationFailure(nameof(request.DefaultLocale), "validation.locale");
    }

    if (!IsSupportedLocale(request.FallbackLocale, out var fallbackLocale) || !enabled.Contains(fallbackLocale))
    {
      yield return new ValidationFailure(nameof(request.FallbackLocale), "validation.locale");
    }
  }

  private static IEnumerable<ValidationFailure> ValidateUserLocalePreference(
    UserLocalePreferenceUpdateRequestDto request,
    OrganizationSettings settings)
  {
    if (!IsSupportedLocale(request.Locale, out var locale) || !settings.EnabledLocales.Contains(locale, StringComparer.Ordinal))
    {
      yield return new ValidationFailure(nameof(request.Locale), "validation.locale");
    }
  }

  private static IEnumerable<ValidationFailure> ValidateSecurity(SecuritySettingsUpdateRequestDto request)
  {
    if (request.SessionTimeoutMinutes is < 5 or > 1440)
    {
      yield return new ValidationFailure(nameof(request.SessionTimeoutMinutes), ValidationMessageKeys.MinValue);
    }

    if (request.PasswordRules is null)
    {
      yield return new ValidationFailure(nameof(request.PasswordRules), ValidationMessageKeys.Required);
      yield break;
    }

    if (request.PasswordRules.MinimumLength is < 8 or > 128)
    {
      yield return new ValidationFailure(nameof(request.PasswordRules.MinimumLength), ValidationMessageKeys.MinValue);
    }

    if (string.IsNullOrWhiteSpace(request.MfaPolicy))
    {
      yield return new ValidationFailure(nameof(request.MfaPolicy), ValidationMessageKeys.Required);
    }
    else if (!SettingsCatalog.SupportedMfaPolicies.Contains(
      SettingsCode.NormalizeCode(request.MfaPolicy),
      StringComparer.OrdinalIgnoreCase))
    {
      yield return new ValidationFailure(nameof(request.MfaPolicy), "validation.mfaPolicy");
    }
  }

  private static IEnumerable<ValidationFailure> ValidateNotifications(NotificationSettingsUpdateRequestDto request)
  {
    if (request.EnabledCategories is null || request.EnabledCategories.Count == 0)
    {
      yield return new ValidationFailure(nameof(request.EnabledCategories), ValidationMessageKeys.Required);
    }
    else
    {
      foreach (var category in request.EnabledCategories)
      {
        if (string.IsNullOrWhiteSpace(category))
        {
          yield return new ValidationFailure(nameof(request.EnabledCategories), ValidationMessageKeys.Required);
        }
        else if (!NotificationCatalog.IsKnownCategory(category))
        {
          yield return new ValidationFailure(nameof(request.EnabledCategories), "validation.category");
        }
      }
    }

    if (request.EnabledChannels is null || request.EnabledChannels.Count == 0)
    {
      yield return new ValidationFailure(nameof(request.EnabledChannels), ValidationMessageKeys.Required);
    }
    else
    {
      foreach (var channel in request.EnabledChannels)
      {
        if (string.IsNullOrWhiteSpace(channel))
        {
          yield return new ValidationFailure(nameof(request.EnabledChannels), ValidationMessageKeys.Required);
        }
        else if (!NotificationCatalog.IsKnownChannel(channel))
        {
          yield return new ValidationFailure(nameof(request.EnabledChannels), "validation.channel");
        }
      }
    }
  }

  private static IEnumerable<ValidationFailure> ValidateCatalogUpdate(
    string catalogType,
    DomainCatalogUpdateRequestDto request)
  {
    if (!SettingsCatalog.IsKnownCatalogType(catalogType))
    {
      yield return new ValidationFailure(nameof(catalogType), "validation.catalogType");
    }

    if (request.Items is null || request.Items.Count == 0)
    {
      yield return new ValidationFailure(nameof(request.Items), ValidationMessageKeys.Required);
      yield break;
    }

    var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    for (var index = 0; index < request.Items.Count; index++)
    {
      var item = request.Items[index];
      if (string.IsNullOrWhiteSpace(item.Code))
      {
        yield return new ValidationFailure($"Items[{index}].Code", ValidationMessageKeys.Required);
      }
      else if (item.Code.Trim().Length > 96)
      {
        yield return new ValidationFailure($"Items[{index}].Code", ValidationMessageKeys.MaxLength);
      }
      else if (!seen.Add(SettingsCode.NormalizeCode(item.Code)))
      {
        yield return new ValidationFailure($"Items[{index}].Code", "validation.duplicate");
      }

      if (item.Labels is null ||
        !item.Labels.TryGetValue("pt-BR", out var ptBr) ||
        string.IsNullOrWhiteSpace(ptBr))
      {
        yield return new ValidationFailure($"Items[{index}].Labels.pt-BR", ValidationMessageKeys.Required);
      }
      else if (ptBr.Trim().Length > 120)
      {
        yield return new ValidationFailure($"Items[{index}].Labels.pt-BR", ValidationMessageKeys.MaxLength);
      }

      if (item.Labels is null ||
        !item.Labels.TryGetValue("en-US", out var enUs) ||
        string.IsNullOrWhiteSpace(enUs))
      {
        yield return new ValidationFailure($"Items[{index}].Labels.en-US", ValidationMessageKeys.Required);
      }
      else if (enUs.Trim().Length > 120)
      {
        yield return new ValidationFailure($"Items[{index}].Labels.en-US", ValidationMessageKeys.MaxLength);
      }
    }
  }

  private static IEnumerable<ValidationFailure> ValidateBranding(OrganizationBrandingUpdateRequestDto request)
  {
    var primaryColor = NormalizeColor(request.PrimaryColor);
    var primaryForegroundColor = NormalizeColor(request.PrimaryForegroundColor);
    var accentColor = NormalizeColor(request.AccentColor);
    var accentForegroundColor = NormalizeColor(request.AccentForegroundColor);

    if (!string.IsNullOrWhiteSpace(request.DisplayName) && request.DisplayName.Trim().Length > 120)
    {
      yield return new ValidationFailure(nameof(request.DisplayName), ValidationMessageKeys.MaxLength);
    }

    if (!string.IsNullOrWhiteSpace(request.LogoAlt) && request.LogoAlt.Trim().Length > 140)
    {
      yield return new ValidationFailure(nameof(request.LogoAlt), ValidationMessageKeys.MaxLength);
    }

    if (!string.IsNullOrWhiteSpace(request.LogoUrl) && request.LogoUrl.Trim().Length > 400)
    {
      yield return new ValidationFailure(nameof(request.LogoUrl), ValidationMessageKeys.MaxLength);
    }

    if (!string.IsNullOrWhiteSpace(request.SupportPhone) && request.SupportPhone.Trim().Length > 64)
    {
      yield return new ValidationFailure(nameof(request.SupportPhone), ValidationMessageKeys.MaxLength);
    }

    if (!string.IsNullOrWhiteSpace(request.PrimaryColor) && primaryColor is null)
    {
      yield return new ValidationFailure(nameof(request.PrimaryColor), "validation.branding.colorHex");
    }

    if (!string.IsNullOrWhiteSpace(request.PrimaryForegroundColor) && primaryForegroundColor is null)
    {
      yield return new ValidationFailure(nameof(request.PrimaryForegroundColor), "validation.branding.colorHex");
    }

    if (!string.IsNullOrWhiteSpace(request.AccentColor) && accentColor is null)
    {
      yield return new ValidationFailure(nameof(request.AccentColor), "validation.branding.colorHex");
    }

    if (!string.IsNullOrWhiteSpace(request.AccentForegroundColor) && accentForegroundColor is null)
    {
      yield return new ValidationFailure(nameof(request.AccentForegroundColor), "validation.branding.colorHex");
    }

    if (primaryColor is not null &&
      primaryForegroundColor is not null &&
      !MeetsContrastRatio(primaryForegroundColor, primaryColor))
    {
      yield return new ValidationFailure(nameof(request.PrimaryForegroundColor), "validation.branding.colorContrast");
    }

    if (accentColor is not null &&
      accentForegroundColor is not null &&
      !MeetsContrastRatio(accentForegroundColor, accentColor))
    {
      yield return new ValidationFailure(nameof(request.AccentForegroundColor), "validation.branding.colorContrast");
    }

    if (!IsValidAbsoluteUrl(request.LogoUrl))
    {
      yield return new ValidationFailure(nameof(request.LogoUrl), "validation.url");
    }

    if (!IsValidAbsoluteUrl(request.SupportUrl))
    {
      yield return new ValidationFailure(nameof(request.SupportUrl), "validation.url");
    }
    else if (request.SupportUrl?.Trim().Length > 400)
    {
      yield return new ValidationFailure(nameof(request.SupportUrl), ValidationMessageKeys.MaxLength);
    }

    if (!string.IsNullOrWhiteSpace(request.SupportEmail) &&
      !request.SupportEmail.Contains('@', StringComparison.Ordinal))
    {
      yield return new ValidationFailure(nameof(request.SupportEmail), ValidationMessageKeys.Email);
    }
    else if (request.SupportEmail?.Trim().Length > 320)
    {
      yield return new ValidationFailure(nameof(request.SupportEmail), ValidationMessageKeys.MaxLength);
    }

    if ((!string.IsNullOrWhiteSpace(request.LogoUrl)) && string.IsNullOrWhiteSpace(request.LogoAlt))
    {
      yield return new ValidationFailure(nameof(request.LogoAlt), "validation.branding.logoAltRequired");
    }
  }

  private static IEnumerable<ValidationFailure> ValidateLogo(BrandLogoUploadRequestDto request)
  {
    if (string.IsNullOrWhiteSpace(request.FileName))
    {
      yield return new ValidationFailure(nameof(request.FileName), ValidationMessageKeys.Required);
    }
    else if (request.FileName.Trim().Length > 180)
    {
      yield return new ValidationFailure(nameof(request.FileName), ValidationMessageKeys.MaxLength);
    }

    if (!SettingsCatalog.SupportedLogoContentTypes.Contains(request.ContentType, StringComparer.OrdinalIgnoreCase))
    {
      yield return new ValidationFailure(nameof(request.ContentType), "validation.branding.logoMimeType");
    }

    if (request.SizeBytes is <= 0 or > SettingsCatalog.MaxLogoSizeBytes)
    {
      yield return new ValidationFailure(nameof(request.SizeBytes), "validation.branding.logoSize");
    }

    if (request.Width is <= 0 or > SettingsCatalog.MaxLogoDimensionPixels ||
      request.Height is <= 0 or > SettingsCatalog.MaxLogoDimensionPixels)
    {
      yield return new ValidationFailure(nameof(request.Width), "validation.branding.logoDimension");
      yield return new ValidationFailure(nameof(request.Height), "validation.branding.logoDimension");
    }

    if (string.IsNullOrWhiteSpace(request.LogoAlt))
    {
      yield return new ValidationFailure(nameof(request.LogoAlt), "validation.branding.logoAltRequired");
    }
    else if (request.LogoAlt.Trim().Length > 140)
    {
      yield return new ValidationFailure(nameof(request.LogoAlt), ValidationMessageKeys.MaxLength);
    }

    if (string.IsNullOrWhiteSpace(request.ContentBase64))
    {
      yield return new ValidationFailure(nameof(request.ContentBase64), ValidationMessageKeys.Required);
      yield break;
    }

    byte[]? bytes = null;
    try
    {
      bytes = Convert.FromBase64String(request.ContentBase64);
    }
    catch (FormatException)
    {
      // Validation failures are yielded after the catch because iterator blocks
      // cannot yield directly from a try/catch body.
    }

    if (bytes is null)
    {
      yield return new ValidationFailure(nameof(request.ContentBase64), "validation.fileType");
    }
    else if (bytes.LongLength != request.SizeBytes)
    {
      yield return new ValidationFailure(nameof(request.ContentBase64), "validation.branding.logoSize");
    }
    else if (!MatchesLogoContentType(bytes, request.ContentType))
    {
      yield return new ValidationFailure(nameof(request.ContentType), "validation.branding.logoMimeType");
    }
  }

  private static IEnumerable<ValidationFailure> ValidateConcurrencyOnly(string? concurrencyToken)
  {
    if (concurrencyToken is not null && string.IsNullOrWhiteSpace(concurrencyToken))
    {
      yield return new ValidationFailure(nameof(concurrencyToken), ValidationMessageKeys.Required);
    }
  }

  private static bool IsSupportedLocale(string? value, out string normalized)
  {
    normalized = string.Empty;
    if (string.IsNullOrWhiteSpace(value))
    {
      return false;
    }

    try
    {
      normalized = SettingsCode.NormalizeLocale(value);
    }
    catch (CultureNotFoundException)
    {
      return false;
    }

    return SettingsCatalog.SupportedLocales.Contains(normalized, StringComparer.Ordinal);
  }

  private static Dictionary<string, string[]>? EnsureCurrentConcurrencyToken(
    OrganizationSettings settings,
    string? concurrencyToken) =>
    string.IsNullOrWhiteSpace(concurrencyToken) ||
    !string.Equals(concurrencyToken.Trim(), settings.ConcurrencyToken.Value, StringComparison.Ordinal)
      ? new Dictionary<string, string[]> { ["concurrencyToken"] = ["validation.concurrency"] }
      : null;

  private static Dictionary<string, string[]>? EnsureCurrentConcurrencyToken(
    DomainCatalogSetting item,
    string? concurrencyToken) =>
    string.IsNullOrWhiteSpace(concurrencyToken) ||
    !string.Equals(concurrencyToken.Trim(), item.ConcurrencyToken.Value, StringComparison.Ordinal)
      ? new Dictionary<string, string[]> { ["concurrencyToken"] = ["validation.concurrency"] }
      : null;

  private static ApplicationOperationResult<T> Invalid<T>(IEnumerable<ValidationFailure> failures) =>
    ApplicationOperationResult<T>.Invalid(failures);

  private static Dictionary<string, string> ToBrandingAuditData(OrganizationSettings settings)
  {
    var data = new Dictionary<string, string>(StringComparer.Ordinal);
    if (!string.IsNullOrWhiteSpace(settings.BrandDisplayName))
    {
      data["displayName"] = settings.BrandDisplayName;
    }

    if (!string.IsNullOrWhiteSpace(settings.PrimaryColor))
    {
      data["primaryColor"] = settings.PrimaryColor;
    }

    if (!string.IsNullOrWhiteSpace(settings.AccentColor))
    {
      data["accentColor"] = settings.AccentColor;
    }

    if (!string.IsNullOrWhiteSpace(settings.LogoStorageKey))
    {
      data["logo"] = settings.LogoStorageKey;
    }

    return data;
  }

  private static bool IsValidAbsoluteUrl(string? value)
  {
    if (string.IsNullOrWhiteSpace(value))
    {
      return true;
    }

    return Uri.TryCreate(value.Trim(), UriKind.Absolute, out var uri) &&
      (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp);
  }

  private static string? NormalizeColor(string? value)
  {
    if (string.IsNullOrWhiteSpace(value))
    {
      return null;
    }

    var trimmed = value.Trim();
    if (!HexColorRegex().IsMatch(trimmed))
    {
      return null;
    }

    if (trimmed.Length == 4)
    {
      trimmed = string.Create(
        CultureInfo.InvariantCulture,
        $"#{trimmed[1]}{trimmed[1]}{trimmed[2]}{trimmed[2]}{trimmed[3]}{trimmed[3]}");
    }

#pragma warning disable CA1308
    return trimmed.ToLowerInvariant();
#pragma warning restore CA1308
  }

  private static bool MeetsContrastRatio(string foreground, string background) =>
    ContrastRatio(foreground, background) >= 4.5d;

  private static double ContrastRatio(string foreground, string background)
  {
    var foregroundLuminance = RelativeLuminance(foreground);
    var backgroundLuminance = RelativeLuminance(background);
    var lighter = Math.Max(foregroundLuminance, backgroundLuminance);
    var darker = Math.Min(foregroundLuminance, backgroundLuminance);
    return (lighter + 0.05d) / (darker + 0.05d);
  }

  private static double RelativeLuminance(string color)
  {
    var red = LinearizedColorComponent(color[1..3]);
    var green = LinearizedColorComponent(color[3..5]);
    var blue = LinearizedColorComponent(color[5..7]);

    return 0.2126d * red + 0.7152d * green + 0.0722d * blue;
  }

  private static bool MatchesLogoContentType(byte[] bytes, string contentType)
  {
    if (string.Equals(contentType, "image/png", StringComparison.OrdinalIgnoreCase))
    {
      return bytes.Length >= 8 &&
        bytes[0] == 0x89 &&
        bytes[1] == 0x50 &&
        bytes[2] == 0x4E &&
        bytes[3] == 0x47 &&
        bytes[4] == 0x0D &&
        bytes[5] == 0x0A &&
        bytes[6] == 0x1A &&
        bytes[7] == 0x0A;
    }

    if (string.Equals(contentType, "image/jpeg", StringComparison.OrdinalIgnoreCase))
    {
      return bytes.Length >= 3 && bytes[0] == 0xFF && bytes[1] == 0xD8 && bytes[2] == 0xFF;
    }

    if (string.Equals(contentType, "image/webp", StringComparison.OrdinalIgnoreCase))
    {
      return bytes.Length >= 12 &&
        bytes[0] == 0x52 &&
        bytes[1] == 0x49 &&
        bytes[2] == 0x46 &&
        bytes[3] == 0x46 &&
        bytes[8] == 0x57 &&
        bytes[9] == 0x45 &&
        bytes[10] == 0x42 &&
        bytes[11] == 0x50;
    }

    return false;
  }

  private static double LinearizedColorComponent(string component)
  {
    var value = int.Parse(component, NumberStyles.HexNumber, CultureInfo.InvariantCulture) / 255d;
    return value <= 0.03928d ? value / 12.92d : Math.Pow((value + 0.055d) / 1.055d, 2.4d);
  }

  [GeneratedRegex("^#(?:[0-9a-fA-F]{3}|[0-9a-fA-F]{6})$", RegexOptions.CultureInvariant)]
  private static partial Regex HexColorRegex();
}
