using Alsappan.Application.Common.Audit;
using Alsappan.Application.Common.Auth;
using Alsappan.Application.Common.Authorization;
using Alsappan.Application.Common.Contracts;
using Alsappan.Application.Common.Files;
using Alsappan.Application.Common.Results;
using Alsappan.Application.Common.Tenancy;
using Alsappan.Application.Settings;
using Alsappan.Application.Settings.Repositories;
using Alsappan.Domain.Common.Identifiers;
using Alsappan.Domain.Identity;
using Alsappan.Domain.Settings;

namespace Alsappan.Application.Tests.Settings;

public sealed class SettingsServiceTests
{
  [Fact]
  public async Task GetAsyncRequiresSettingsReadPermission()
  {
    var fixture = SettingsFixture.Create(permissions: []);

    var result = await fixture.Service.GetAsync();

    Assert.Equal(ApplicationOperationFailure.Forbidden, result.Failure);
  }

  [Fact]
  public async Task GetAsyncCreatesTenantDefaultsAndCatalogs()
  {
    var fixture = SettingsFixture.Create([PermissionCodes.Read(PermissionModules.Settings)]);

    var result = await fixture.Service.GetAsync("pt-BR");

    Assert.True(result.Succeeded);
    Assert.Equal("pt-BR", result.Value!.Localization.DefaultLocale);
    Assert.Contains("en-US", result.Value.Localization.EnabledLocales);
    Assert.True(result.Value.ResidentPortal.IsEnabled);
    Assert.Contains(result.Value.Catalogs, catalog => catalog.CatalogType == SettingsCatalog.PropertyTypes);
    Assert.Contains(result.Value.Catalogs, catalog => catalog.CatalogType == SettingsCatalog.OccurrenceTypes);
    Assert.All(fixture.Repository.CatalogItems, item => Assert.Equal(fixture.OrganizationId, item.OrganizationId));
  }

  [Fact]
  public async Task UpdateOrganizationProfilePersistsSettingsAndWritesAudit()
  {
    var fixture = SettingsFixture.Create([PermissionCodes.Write(PermissionModules.Settings)]);
    var settings = await fixture.Repository.GetOrCreateSettingsAsync(
      fixture.OrganizationId,
      DateTimeOffset.UtcNow,
      fixture.UserId);

    var result = await fixture.Service.UpdateOrganizationProfileAsync(
      new OrganizationProfileUpdateRequestDto(
        "nova-org",
        "Nova Organizacao",
        "Nova Organizacao LTDA",
        "BRL",
        "America/Sao_Paulo",
        "suporte@example.com",
        "(11) 99999-0000",
        "https://example.com",
        settings.ConcurrencyToken.Value));

    Assert.True(result.Succeeded);
    Assert.Equal("nova-org", fixture.Repository.Organization!.Slug);
    Assert.Equal("Nova Organizacao LTDA", result.Value!.DisplayName);
    Assert.Equal("settings.organization.updated", Assert.Single(fixture.AuditWriter.Entries).Action);
  }

  [Fact]
  public async Task UserLocalePreferenceUsesOrganizationFallbackAndCanPersistEnabledLocale()
  {
    var fixture = SettingsFixture.Create([PermissionCodes.Read(PermissionModules.Settings)]);

    var fallback = await fixture.Service.GetUserLocalePreferenceAsync("en-US");
    var updated = await fixture.Service.UpdateUserLocalePreferenceAsync(
      new UserLocalePreferenceUpdateRequestDto("en-US"),
      "en-US");

    Assert.True(fallback.Succeeded);
    Assert.False(fallback.Value!.IsPersisted);
    Assert.Equal("pt-BR", fallback.Value.EffectiveLocale);
    Assert.True(updated.Succeeded);
    Assert.True(updated.Value!.IsPersisted);
    Assert.Equal("en-US", updated.Value.EffectiveLocale);
  }

  [Fact]
  public async Task CatalogUpdatesRequireManagePermissionAndRejectDuplicateCodes()
  {
    var readFixture = SettingsFixture.Create([PermissionCodes.Read(PermissionModules.Settings)]);
    var forbidden = await readFixture.Service.UpdateCatalogAsync(
      SettingsCatalog.PropertyTypes,
      new DomainCatalogUpdateRequestDto(
      [
        new(
          "loft",
          new Dictionary<string, string> { ["pt-BR"] = "Loft", ["en-US"] = "Loft" })
      ]));
    Assert.Equal(ApplicationOperationFailure.Forbidden, forbidden.Failure);

    var manageFixture = SettingsFixture.Create([PermissionCodes.Manage(PermissionModules.Settings)]);
    var duplicate = await manageFixture.Service.UpdateCatalogAsync(
      SettingsCatalog.PropertyTypes,
      new DomainCatalogUpdateRequestDto(
      [
        new(
          "loft",
          new Dictionary<string, string> { ["pt-BR"] = "Loft", ["en-US"] = "Loft" }),
        new(
          "LOFT",
          new Dictionary<string, string> { ["pt-BR"] = "Loft 2", ["en-US"] = "Loft 2" })
      ]));
    Assert.Equal(ApplicationOperationFailure.Validation, duplicate.Failure);

    var result = await manageFixture.Service.UpdateCatalogAsync(
      SettingsCatalog.PropertyTypes,
      new DomainCatalogUpdateRequestDto(
      [
        new(
          "loft",
          new Dictionary<string, string> { ["pt-BR"] = "Loft", ["en-US"] = "Loft" },
          SortOrder: 5)
      ]));

    Assert.True(result.Succeeded);
    Assert.Contains(result.Value!.Items, item => item.Code == "loft" && !item.IsSystem);
  }

  [Fact]
  public async Task SettingsSectionsPersistAndAuditChanges()
  {
    var fixture = SettingsFixture.Create(
    [
      PermissionCodes.Write(PermissionModules.Settings),
      PermissionCodes.Manage(PermissionModules.Settings)
    ]);
    var settings = await fixture.Repository.GetOrCreateSettingsAsync(
      fixture.OrganizationId,
      DateTimeOffset.UtcNow,
      fixture.UserId);

    var tenant = await fixture.Service.UpdateTenantBehaviorAsync(
      new TenantBehaviorUpdateRequestDto(false, true, true, settings.ConcurrencyToken.Value));
    var residentPortal = await fixture.Service.UpdateResidentPortalAsync(
      new ResidentPortalUpdateRequestDto(true, false, true, false, tenant.Value!.ConcurrencyToken));
    var security = await fixture.Service.UpdateSecurityAsync(
      new SecuritySettingsUpdateRequestDto(
        120,
        new PasswordRuleSettingsDto(12, true, true, true, true),
        "required",
        residentPortal.Value!.ConcurrencyToken));
    var notifications = await fixture.Service.UpdateNotificationsAsync(
      new NotificationSettingsUpdateRequestDto(
        ["payments", "system"],
        ["in-app", "email"],
        true,
        true,
        false,
        security.Value!.ConcurrencyToken));

    Assert.True(tenant.Succeeded);
    Assert.False(tenant.Value!.AllowOrganizationSwitching);
    Assert.True(residentPortal.Succeeded);
    Assert.True(residentPortal.Value!.AllowDocumentUpload);
    Assert.True(security.Succeeded);
    Assert.Equal(120, security.Value!.SessionTimeoutMinutes);
    Assert.True(notifications.Succeeded);
    Assert.Contains("email", notifications.Value!.EnabledChannels);
    Assert.Contains(fixture.AuditWriter.Entries, entry => entry.Action == "settings.security.updated");
  }

  [Fact]
  public async Task BrandingValidationRejectsUnsafeColorsAndLogoMetadata()
  {
    var fixture = SettingsFixture.Create([PermissionCodes.Write(PermissionModules.Settings)]);
    var settings = await fixture.Repository.GetOrCreateSettingsAsync(
      fixture.OrganizationId,
      DateTimeOffset.UtcNow,
      fixture.UserId);

    var lowContrast = await fixture.Service.UpdateBrandingAsync(
      new OrganizationBrandingUpdateRequestDto(
        "Marca",
        null,
        null,
        "#ffffff",
        "#fefefe",
        "#1877f2",
        "#ffffff",
        "suporte@example.com",
        null,
        "https://example.com",
        settings.ConcurrencyToken.Value));
    var badLogo = await fixture.Service.UploadLogoAsync(
      new BrandLogoUploadRequestDto(
        "logo.exe",
        "application/octet-stream",
        4,
        200,
        80,
        "Logo",
        Convert.ToBase64String([1, 2, 3, 4]),
        settings.ConcurrencyToken.Value));

    Assert.Equal(ApplicationOperationFailure.Validation, lowContrast.Failure);
    Assert.Contains("PrimaryForegroundColor", lowContrast.Errors!.Keys);
    Assert.Equal(ApplicationOperationFailure.Validation, badLogo.Failure);
    Assert.Contains(nameof(BrandLogoUploadRequestDto.ContentType), badLogo.Errors!.Keys);
  }

  [Fact]
  public async Task NotificationValidationRejectsBlankTokens()
  {
    var fixture = SettingsFixture.Create([PermissionCodes.Manage(PermissionModules.Settings)]);
    var settings = await fixture.Repository.GetOrCreateSettingsAsync(
      fixture.OrganizationId,
      DateTimeOffset.UtcNow,
      fixture.UserId);

    var result = await fixture.Service.UpdateNotificationsAsync(
      new NotificationSettingsUpdateRequestDto(
        ["payments", " "],
        ["in-app"],
        true,
        false,
        false,
        settings.ConcurrencyToken.Value));

    Assert.Equal(ApplicationOperationFailure.Validation, result.Failure);
    Assert.Contains(nameof(NotificationSettingsUpdateRequestDto.EnabledCategories), result.Errors!.Keys);
  }

  [Fact]
  public async Task LogoUploadStoresFileAndRemovalDeletesPreviousAsset()
  {
    var fixture = SettingsFixture.Create([PermissionCodes.Manage(PermissionModules.Settings)]);
    var settings = await fixture.Repository.GetOrCreateSettingsAsync(
      fixture.OrganizationId,
      DateTimeOffset.UtcNow,
      fixture.UserId);

    var upload = await fixture.Service.UploadLogoAsync(
      new BrandLogoUploadRequestDto(
        "logo.png",
        "image/png",
        8,
        200,
        80,
        "Logo da organizacao",
        Convert.ToBase64String([0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]),
        settings.ConcurrencyToken.Value));
    var remove = await fixture.Service.RemoveLogoAsync();

    Assert.True(upload.Succeeded);
    Assert.NotNull(upload.Value!.Logo);
    Assert.True(remove.Succeeded);
    Assert.Null(remove.Value!.Logo);
    Assert.Equal(1, fixture.FileStorageProvider.SaveCalls);
    Assert.Equal(upload.Value.Logo!.StorageKey, Assert.Single(fixture.FileStorageProvider.DeletedKeys));
  }

  private sealed class SettingsFixture
  {
    private SettingsFixture(
      OrganizationId organizationId,
      UserId userId,
      FakeSettingsRepository repository,
      RecordingAuditWriter auditWriter,
      RecordingFileStorageProvider fileStorageProvider,
      SettingsService service)
    {
      OrganizationId = organizationId;
      UserId = userId;
      Repository = repository;
      AuditWriter = auditWriter;
      FileStorageProvider = fileStorageProvider;
      Service = service;
    }

    public OrganizationId OrganizationId { get; }

    public UserId UserId { get; }

    public FakeSettingsRepository Repository { get; }

    public RecordingAuditWriter AuditWriter { get; }

    public RecordingFileStorageProvider FileStorageProvider { get; }

    public SettingsService Service { get; }

    public static SettingsFixture Create(IEnumerable<string> permissions)
    {
      var organizationId = OrganizationId.New();
      var userId = UserId.New();
      var organization = IdentityOrganization.Create(
        organizationId,
        "alsappan",
        "Alsappan",
        DateTimeOffset.UtcNow,
        userId,
        "Alsappan",
        IdentityDefaults.DefaultLocale,
        IdentityDefaults.DefaultCurrency);
      var repository = new FakeSettingsRepository(organization);
      var auditWriter = new RecordingAuditWriter();
      var fileStorageProvider = new RecordingFileStorageProvider();
      var service = new SettingsService(
        repository,
        new FixedPermissionService(permissions, organizationId),
        new FixedActiveOrganizationContextResolver(organizationId, userId, permissions),
        auditWriter,
        fileStorageProvider);

      return new SettingsFixture(organizationId, userId, repository, auditWriter, fileStorageProvider, service);
    }
  }

  private sealed class FakeSettingsRepository : ISettingsRepository
  {
    private readonly List<DomainCatalogSetting> catalogItems = [];
    private readonly List<UserLocalePreference> localePreferences = [];
    private OrganizationSettings? settings;

    public FakeSettingsRepository(IdentityOrganization organization)
    {
      Organization = organization;
    }

    public IdentityOrganization? Organization { get; private set; }

    public IReadOnlyList<DomainCatalogSetting> CatalogItems => catalogItems;

    public Task<IdentityOrganization?> FindOrganizationAsync(
      OrganizationId organizationId,
      CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();
      return Task.FromResult(Organization?.Id == organizationId ? Organization : null);
    }

    public Task<OrganizationSettings> GetOrCreateSettingsAsync(
      OrganizationId organizationId,
      DateTimeOffset createdAt,
      UserId? createdByUserId = null,
      CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();
      settings ??= OrganizationSettings.Create(EntityId.New(), organizationId, createdAt, createdByUserId);
      return Task.FromResult(settings);
    }

    public Task<IReadOnlyList<DomainCatalogSetting>> ListCatalogItemsAsync(
      OrganizationId organizationId,
      string? catalogType = null,
      CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();
      var normalizedCatalogType = string.IsNullOrWhiteSpace(catalogType)
        ? null
        : SettingsCatalog.NormalizeCatalogType(catalogType);
      IReadOnlyList<DomainCatalogSetting> items = catalogItems
        .Where(item =>
          item.OrganizationId == organizationId &&
          (normalizedCatalogType is null || item.CatalogType == normalizedCatalogType))
        .OrderBy(item => item.CatalogType, StringComparer.Ordinal)
        .ThenBy(item => item.SortOrder)
        .ToArray();
      return Task.FromResult(items);
    }

    public Task EnsureCatalogDefaultsAsync(
      OrganizationId organizationId,
      DateTimeOffset createdAt,
      UserId? createdByUserId = null,
      string? catalogType = null,
      CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();
      var normalizedCatalogType = string.IsNullOrWhiteSpace(catalogType)
        ? null
        : SettingsCatalog.NormalizeCatalogType(catalogType);
      foreach (var item in SettingsCatalog.DefaultCatalogItems
        .Where(item => normalizedCatalogType is null || item.CatalogType == normalizedCatalogType))
      {
        if (catalogItems.Any(existing =>
          existing.OrganizationId == organizationId &&
          existing.CatalogType == item.CatalogType &&
          existing.Code == item.Code))
        {
          continue;
        }

        catalogItems.Add(DomainCatalogSetting.Create(
          EntityId.New(),
          organizationId,
          item.CatalogType,
          item.Code,
          item.LabelPtBr,
          item.LabelEnUs,
          item.SortOrder,
          isEnabled: true,
          isSystem: true,
          createdAt,
          createdByUserId));
      }

      return Task.CompletedTask;
    }

    public Task<UserLocalePreference?> FindUserLocalePreferenceAsync(
      OrganizationId organizationId,
      UserId userId,
      CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();
      return Task.FromResult(localePreferences.FirstOrDefault(preference =>
        preference.OrganizationId == organizationId &&
        preference.UserId == userId));
    }

    public void AddUserLocalePreference(UserLocalePreference preference)
    {
      localePreferences.Add(preference);
    }

    public void AddCatalogItem(DomainCatalogSetting item)
    {
      catalogItems.Add(item);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();
      return Task.CompletedTask;
    }
  }

  private sealed class FixedPermissionService : IPermissionService
  {
    private readonly OrganizationId organizationId;
    private readonly HashSet<string> permissions;

    public FixedPermissionService(IEnumerable<string> permissions, OrganizationId organizationId)
    {
      this.organizationId = organizationId;
      this.permissions = new HashSet<string>(permissions, StringComparer.OrdinalIgnoreCase);
    }

    public ValueTask<PermissionEvaluationResult> AuthorizeAsync(
      string permissionCode,
      CancellationToken cancellationToken = default) =>
      AuthorizeAsync(new PermissionRequirement(permissionCode), cancellationToken);

    public ValueTask<PermissionEvaluationResult> AuthorizeAsync(
      PermissionRequirement requirement,
      CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();
      var granted = permissions.Contains(PermissionCodes.Wildcard) ||
        permissions.Contains(PermissionCodes.Normalize(requirement.PermissionCode));

      return ValueTask.FromResult(
        granted
          ? PermissionEvaluationResult.Granted(requirement, organizationId)
          : PermissionEvaluationResult.Denied(
            requirement,
            PermissionEvaluationFailure.PermissionDenied,
            organizationId));
    }

    public ValueTask<IReadOnlySet<string>> GetEffectivePermissionsAsync(
      CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();
      return ValueTask.FromResult<IReadOnlySet<string>>(permissions);
    }
  }

  private sealed class FixedActiveOrganizationContextResolver : IActiveOrganizationContextResolver
  {
    private readonly ActiveOrganizationContext context;

    public FixedActiveOrganizationContextResolver(
      OrganizationId organizationId,
      UserId userId,
      IEnumerable<string> permissions)
    {
      var membership = new OrganizationMembership(
        organizationId,
        permissionCodes: permissions,
        isActive: true);
      var user = new AuthenticatedUser(
        userId,
        "admin@alsappan.local",
        "Ana Admin",
        [membership]);
      context = new ActiveOrganizationContext(user, membership);
    }

    public ValueTask<ActiveOrganizationResolutionResult> ResolveAsync(
      CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();
      return ValueTask.FromResult(ActiveOrganizationResolutionResult.Success(context));
    }
  }

  private sealed class RecordingAuditWriter : IAuditWriter
  {
    public List<AuditEntryDraft> Entries { get; } = [];

    public Task WriteAsync(AuditEntryDraft entry, CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();
      Entries.Add(entry);
      return Task.CompletedTask;
    }
  }

  private sealed class RecordingFileStorageProvider : IFileStorageProvider
  {
    public int SaveCalls { get; private set; }

    public List<string> DeletedKeys { get; } = [];

    public Task<StoredFileDescriptor> SaveAsync(
      FileStorageRequest request,
      CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();
      SaveCalls++;
      return Task.FromResult(new StoredFileDescriptor(
        request.OrganizationId,
        $"organizations/{request.OrganizationId}/logos/{Guid.NewGuid():N}-{request.FileName}",
        request.FileName,
        request.ContentType,
        request.Content.Length,
        DateTimeOffset.UtcNow,
        request.Metadata));
    }

    public Task<Stream> OpenReadAsync(
      OrganizationId organizationId,
      string storageKey,
      CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();
      Stream stream = new MemoryStream([]);
      return Task.FromResult(stream);
    }

    public Task DeleteAsync(
      OrganizationId organizationId,
      string storageKey,
      CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();
      DeletedKeys.Add(storageKey);
      return Task.CompletedTask;
    }
  }
}
