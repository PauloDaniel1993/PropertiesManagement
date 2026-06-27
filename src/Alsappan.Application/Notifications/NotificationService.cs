using Alsappan.Application.Common.Authorization;
using Alsappan.Application.Common.Contracts;
using Alsappan.Application.Common.Results;
using Alsappan.Application.Common.Tenancy;
using Alsappan.Application.Common.Validation;
using Alsappan.Application.Notifications.Repositories;

namespace Alsappan.Application.Notifications;

public sealed class NotificationService : INotificationService
{
  private readonly INotificationRepository notificationRepository;
  private readonly IPermissionService permissionService;
  private readonly IActiveOrganizationContextResolver activeOrganizationContextResolver;
  private readonly TimeProvider timeProvider;

  public NotificationService(
    INotificationRepository notificationRepository,
    IPermissionService permissionService,
    IActiveOrganizationContextResolver activeOrganizationContextResolver,
    TimeProvider? timeProvider = null)
  {
    this.notificationRepository = notificationRepository ??
      throw new ArgumentNullException(nameof(notificationRepository));
    this.permissionService = permissionService ?? throw new ArgumentNullException(nameof(permissionService));
    this.activeOrganizationContextResolver = activeOrganizationContextResolver ??
      throw new ArgumentNullException(nameof(activeOrganizationContextResolver));
    this.timeProvider = timeProvider ?? TimeProvider.System;
  }

  public async Task<ApplicationOperationResult<PagedResultDto<NotificationListItemDto>>> ListAsync(
    NotificationListRequestDto request,
    CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(request);

    var validation = ValidateListRequest(request).ToList();
    if (validation.Count > 0)
    {
      return ApplicationOperationResult<PagedResultDto<NotificationListItemDto>>.Invalid(validation);
    }

    var context = await AuthorizeContextAsync(cancellationToken).ConfigureAwait(false);
    if (context is null)
    {
      return ApplicationOperationResult<PagedResultDto<NotificationListItemDto>>.Failed(
        ApplicationOperationFailure.Forbidden);
    }

    var page = await notificationRepository.ListAsync(
        request,
        context.OrganizationId,
        context.UserId,
        cancellationToken)
      .ConfigureAwait(false);

    return ApplicationOperationResult<PagedResultDto<NotificationListItemDto>>.Success(
      new PagedResultDto<NotificationListItemDto>(
        page.Items.Select(item => NotificationCatalog.ToDto(item, request.Locale)).ToArray(),
        page.Page,
        page.PageSize,
        page.TotalItems));
  }

  public async Task<ApplicationOperationResult<NotificationUnreadCountDto>> GetUnreadCountAsync(
    CancellationToken cancellationToken = default)
  {
    var context = await AuthorizeContextAsync(cancellationToken).ConfigureAwait(false);
    if (context is null)
    {
      return ApplicationOperationResult<NotificationUnreadCountDto>.Failed(ApplicationOperationFailure.Forbidden);
    }

    var count = await notificationRepository.CountUnreadAsync(
        context.OrganizationId,
        context.UserId,
        cancellationToken)
      .ConfigureAwait(false);

    return ApplicationOperationResult<NotificationUnreadCountDto>.Success(new NotificationUnreadCountDto(count));
  }

  public async Task<ApplicationOperationResult<NotificationListItemDto>> MarkReadAsync(
    Guid id,
    string? locale = null,
    CancellationToken cancellationToken = default)
  {
    var validation = ValidateId(id).ToList();
    if (validation.Count > 0)
    {
      return ApplicationOperationResult<NotificationListItemDto>.Invalid(validation);
    }

    var context = await AuthorizeContextAsync(cancellationToken).ConfigureAwait(false);
    if (context is null)
    {
      return ApplicationOperationResult<NotificationListItemDto>.Failed(ApplicationOperationFailure.Forbidden);
    }

    var notification = await notificationRepository.MarkReadAsync(
        id,
        context.OrganizationId,
        context.UserId,
        timeProvider.GetUtcNow(),
        cancellationToken)
      .ConfigureAwait(false);

    return notification is null
      ? ApplicationOperationResult<NotificationListItemDto>.Failed(ApplicationOperationFailure.NotFound)
      : ApplicationOperationResult<NotificationListItemDto>.Success(
        NotificationCatalog.ToDto(notification, locale));
  }

  public async Task<ApplicationOperationResult<NotificationMarkAllReadResultDto>> MarkAllReadAsync(
    CancellationToken cancellationToken = default)
  {
    var context = await AuthorizeContextAsync(cancellationToken).ConfigureAwait(false);
    if (context is null)
    {
      return ApplicationOperationResult<NotificationMarkAllReadResultDto>.Failed(
        ApplicationOperationFailure.Forbidden);
    }

    var updatedCount = await notificationRepository.MarkAllReadAsync(
        context.OrganizationId,
        context.UserId,
        timeProvider.GetUtcNow(),
        cancellationToken)
      .ConfigureAwait(false);

    return ApplicationOperationResult<NotificationMarkAllReadResultDto>.Success(
      new NotificationMarkAllReadResultDto(updatedCount));
  }

  public async Task<ApplicationOperationResult> ArchiveAsync(
    Guid id,
    CancellationToken cancellationToken = default)
  {
    var validation = ValidateId(id).ToList();
    if (validation.Count > 0)
    {
      return ApplicationOperationResult.Invalid(validation);
    }

    var context = await AuthorizeContextAsync(cancellationToken).ConfigureAwait(false);
    if (context is null)
    {
      return ApplicationOperationResult.Failed(ApplicationOperationFailure.Forbidden);
    }

    var archived = await notificationRepository.ArchiveAsync(
        id,
        context.OrganizationId,
        context.UserId,
        timeProvider.GetUtcNow(),
        cancellationToken)
      .ConfigureAwait(false);

    return archived
      ? ApplicationOperationResult.Success()
      : ApplicationOperationResult.Failed(ApplicationOperationFailure.NotFound);
  }

  public Task<IReadOnlyList<SelectOptionDto>> GetCategoryOptionsAsync(
    string? locale = null,
    CancellationToken cancellationToken = default)
  {
    cancellationToken.ThrowIfCancellationRequested();
    return Task.FromResult(NotificationCatalog.GetCategoryOptions(locale));
  }

  public Task<IReadOnlyList<SelectOptionDto>> GetChannelOptionsAsync(
    string? locale = null,
    CancellationToken cancellationToken = default)
  {
    cancellationToken.ThrowIfCancellationRequested();
    return Task.FromResult(NotificationCatalog.GetChannelOptions(locale));
  }

  public async Task<ApplicationOperationResult<NotificationPreferencesDto>> GetPreferencesAsync(
    string? locale = null,
    CancellationToken cancellationToken = default)
  {
    var context = await AuthorizeContextAsync(cancellationToken).ConfigureAwait(false);
    if (context is null)
    {
      return ApplicationOperationResult<NotificationPreferencesDto>.Failed(ApplicationOperationFailure.Forbidden);
    }

    var savedPreferences = await notificationRepository.ListPreferencesAsync(
        context.OrganizationId,
        context.UserId,
        cancellationToken)
      .ConfigureAwait(false);

    return ApplicationOperationResult<NotificationPreferencesDto>.Success(
      BuildPreferenceResponse(locale, savedPreferences));
  }

  public async Task<ApplicationOperationResult<NotificationPreferencesDto>> UpdatePreferencesAsync(
    NotificationPreferenceUpdateRequestDto request,
    string? locale = null,
    CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(request);

    var validation = ValidatePreferences(request).ToList();
    if (validation.Count > 0)
    {
      return ApplicationOperationResult<NotificationPreferencesDto>.Invalid(validation);
    }

    var context = await AuthorizeContextAsync(cancellationToken).ConfigureAwait(false);
    if (context is null)
    {
      return ApplicationOperationResult<NotificationPreferencesDto>.Failed(ApplicationOperationFailure.Forbidden);
    }

    var defaultPreferences = NotificationCatalog.GetDefaultPreferences(locale);
    var requested = request.Preferences.ToDictionary(
      item => $"{NotificationCatalog.NormalizeToken(item.Category)}:{NotificationCatalog.NormalizeToken(item.Channel)}",
      item => item.IsEnabled,
      StringComparer.OrdinalIgnoreCase);
    var preferencesToSave = defaultPreferences
      .Select(preference =>
      {
        var key = $"{preference.Category}:{preference.Channel}";
        var isEnabled = requested.TryGetValue(key, out var requestedEnabled)
          ? requestedEnabled
          : preference.IsEnabled;

        return new NotificationPreferenceWriteModel(
          preference.Category,
          preference.Channel,
          preference.IsMandatory || isEnabled,
          preference.IsMandatory);
      })
      .ToArray();

    var savedPreferences = await notificationRepository.SavePreferencesAsync(
        context.OrganizationId,
        context.UserId,
        preferencesToSave,
        timeProvider.GetUtcNow(),
        cancellationToken)
      .ConfigureAwait(false);

    return ApplicationOperationResult<NotificationPreferencesDto>.Success(
      BuildPreferenceResponse(locale, savedPreferences));
  }

  private async Task<ActiveOrganizationContext?> AuthorizeContextAsync(CancellationToken cancellationToken)
  {
    var permission = await permissionService.AuthorizeAsync(
        PermissionCodes.Read(PermissionModules.Notifications),
        cancellationToken)
      .ConfigureAwait(false);
    if (!permission.IsGranted)
    {
      return null;
    }

    var context = await activeOrganizationContextResolver.ResolveAsync(cancellationToken)
      .ConfigureAwait(false);
    return context.Succeeded ? context.Context : null;
  }

  private static NotificationPreferencesDto BuildPreferenceResponse(
    string? locale,
    IReadOnlyList<NotificationPreferenceSnapshot>? savedPreferences = null)
  {
    var savedByKey = savedPreferences?.ToDictionary(
        preference => $"{NotificationCatalog.NormalizeToken(preference.Category)}:{NotificationCatalog.NormalizeToken(preference.Channel)}",
        preference => preference.IsEnabled,
        StringComparer.OrdinalIgnoreCase) ??
      new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
    var merged = NotificationCatalog.GetDefaultPreferences(locale)
      .Select(preference =>
      {
        var key = $"{preference.Category}:{preference.Channel}";
        return preference with
        {
          IsEnabled = preference.IsMandatory ||
            (savedByKey.TryGetValue(key, out var savedIsEnabled)
              ? savedIsEnabled
              : preference.IsEnabled)
        };
      })
      .ToArray();
    var isPersisted = savedByKey.Count > 0;

    return new NotificationPreferencesDto(
      merged,
      isPersisted,
      IsPortuguese(locale)
        ? isPersisted
          ? "Preferencias personalizadas salvas para este usuario."
          : "Preferencias padrao aplicadas para este usuario."
        : isPersisted
          ? "Custom preferences are saved for this user."
          : "Default preferences are applied for this user.");
  }

  private static IEnumerable<ValidationFailure> ValidateListRequest(NotificationListRequestDto request)
  {
    if (!NotificationCatalog.IsKnownCategory(request.Category))
    {
      yield return new ValidationFailure(nameof(request.Category), "validation.category");
    }

    if (!NotificationCatalog.IsKnownChannel(request.Channel))
    {
      yield return new ValidationFailure(nameof(request.Channel), "validation.channel");
    }
  }

  private static IEnumerable<ValidationFailure> ValidatePreferences(NotificationPreferenceUpdateRequestDto request)
  {
    if (request.Preferences is null)
    {
      yield return new ValidationFailure(nameof(request.Preferences), ValidationMessageKeys.Required);
      yield break;
    }

    for (var index = 0; index < request.Preferences.Count; index++)
    {
      var preference = request.Preferences[index];
      if (!NotificationCatalog.IsKnownCategory(preference.Category))
      {
        yield return new ValidationFailure($"Preferences[{index}].Category", "validation.category");
      }

      if (!NotificationCatalog.IsKnownChannel(preference.Channel))
      {
        yield return new ValidationFailure($"Preferences[{index}].Channel", "validation.channel");
      }
    }
  }

  private static IEnumerable<ValidationFailure> ValidateId(Guid id)
  {
    if (id == Guid.Empty)
    {
      yield return new ValidationFailure(nameof(id), ValidationMessageKeys.InvalidId);
    }
  }

  private static bool IsPortuguese(string? locale) =>
    string.IsNullOrWhiteSpace(locale) ||
      locale.StartsWith("pt", StringComparison.OrdinalIgnoreCase);
}
