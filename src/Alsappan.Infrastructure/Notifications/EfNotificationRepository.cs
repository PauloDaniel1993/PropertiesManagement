using Alsappan.Application.Common.Authorization;
using Alsappan.Application.Common.Contracts;
using Alsappan.Application.Notifications;
using Alsappan.Application.Notifications.Repositories;
using Alsappan.Domain.Common.Identifiers;
using Alsappan.Domain.Identity;
using Alsappan.Infrastructure.Authorization;
using Alsappan.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Alsappan.Infrastructure.Notifications;

public sealed class EfNotificationRepository : INotificationRepository
{
  private readonly AlsappanDbContext dbContext;
  private readonly IRolePermissionCatalog rolePermissionCatalog;

  public EfNotificationRepository(
    AlsappanDbContext dbContext,
    IRolePermissionCatalog? rolePermissionCatalog = null)
  {
    this.dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    this.rolePermissionCatalog = rolePermissionCatalog ?? new DefaultRolePermissionCatalog();
  }

  public async Task<PagedResultDto<NotificationRecordSnapshot>> ListAsync(
    NotificationListRequestDto request,
    OrganizationId organizationId,
    UserId userId,
    CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(request);

    var filter = request.ToListFilter();
    var disabledPreferences = await ListDisabledPreferencesAsync(organizationId, userId, cancellationToken)
      .ConfigureAwait(false);
    var visibleCategories = await ListVisibleCategoriesAsync(organizationId, userId, cancellationToken)
      .ConfigureAwait(false);
    var query = ApplyFilters(
      ApplyPreferenceVisibility(
        ApplyPermissionVisibility(
          BuildUserScopedQuery(organizationId, userId, request.IncludeArchived),
          visibleCategories),
        disabledPreferences),
      request);
    var totalItems = await query.CountAsync(cancellationToken).ConfigureAwait(false);
    var rows = await ApplySort(query.AsNoTracking(), request.Sort)
      .Skip(filter.Offset)
      .Take(filter.PageSize)
      .ToListAsync(cancellationToken)
      .ConfigureAwait(false);

    return new PagedResultDto<NotificationRecordSnapshot>(
      rows.Select(ToSnapshot).ToArray(),
      filter.Page,
      filter.PageSize,
      totalItems);
  }

  public Task<int> CountUnreadAsync(
    OrganizationId organizationId,
    UserId userId,
    CancellationToken cancellationToken = default) =>
    CountUnreadCoreAsync(organizationId, userId, cancellationToken);

  public async Task<NotificationRecordSnapshot?> MarkReadAsync(
    Guid id,
    OrganizationId organizationId,
    UserId userId,
    DateTimeOffset readAt,
    CancellationToken cancellationToken = default)
  {
    var disabledPreferences = await ListDisabledPreferencesAsync(organizationId, userId, cancellationToken)
      .ConfigureAwait(false);
    var visibleCategories = await ListVisibleCategoriesAsync(organizationId, userId, cancellationToken)
      .ConfigureAwait(false);
    var notification = await ApplyPreferenceVisibility(
        ApplyPermissionVisibility(
          BuildUserScopedQuery(organizationId, userId, includeArchived: false),
          visibleCategories),
        disabledPreferences)
      .FirstOrDefaultAsync(item => item.Id == id, cancellationToken)
      .ConfigureAwait(false);

    if (notification is null)
    {
      return null;
    }

    if (!notification.IsRead)
    {
      notification.MarkRead(readAt);
      await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    return ToSnapshot(notification);
  }

  public async Task<int> MarkAllReadAsync(
    OrganizationId organizationId,
    UserId userId,
    DateTimeOffset readAt,
    CancellationToken cancellationToken = default)
  {
    var disabledPreferences = await ListDisabledPreferencesAsync(organizationId, userId, cancellationToken)
      .ConfigureAwait(false);
    var visibleCategories = await ListVisibleCategoriesAsync(organizationId, userId, cancellationToken)
      .ConfigureAwait(false);
    var notifications = await ApplyPreferenceVisibility(
        ApplyPermissionVisibility(
          BuildUserScopedQuery(organizationId, userId, includeArchived: false),
          visibleCategories),
        disabledPreferences)
      .Where(notification => !notification.IsRead)
      .ToListAsync(cancellationToken)
      .ConfigureAwait(false);

    foreach (var notification in notifications)
    {
      notification.MarkRead(readAt);
    }

    if (notifications.Count > 0)
    {
      await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    return notifications.Count;
  }

  public async Task<bool> ArchiveAsync(
    Guid id,
    OrganizationId organizationId,
    UserId userId,
    DateTimeOffset archivedAt,
    CancellationToken cancellationToken = default)
  {
    var disabledPreferences = await ListDisabledPreferencesAsync(organizationId, userId, cancellationToken)
      .ConfigureAwait(false);
    var visibleCategories = await ListVisibleCategoriesAsync(organizationId, userId, cancellationToken)
      .ConfigureAwait(false);
    var notification = await ApplyPreferenceVisibility(
        ApplyPermissionVisibility(
          BuildUserScopedQuery(organizationId, userId, includeArchived: false),
          visibleCategories),
        disabledPreferences)
      .FirstOrDefaultAsync(item => item.Id == id, cancellationToken)
      .ConfigureAwait(false);

    if (notification is null)
    {
      return false;
    }

    notification.Archive(archivedAt, userId);
    await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

    return true;
  }

  public async Task<IReadOnlyList<NotificationPreferenceSnapshot>> ListPreferencesAsync(
    OrganizationId organizationId,
    UserId userId,
    CancellationToken cancellationToken = default) =>
    await dbContext.NotificationPreferenceRecords
      .IgnoreQueryFilters()
      .AsNoTracking()
      .Where(preference => preference.OrganizationId == organizationId && preference.UserId == userId)
      .OrderBy(preference => preference.Category)
      .ThenBy(preference => preference.Channel)
      .Select(preference => new NotificationPreferenceSnapshot(
        preference.Category,
        preference.Channel,
        preference.IsEnabled,
        preference.CreatedAt,
        preference.UpdatedAt))
      .ToListAsync(cancellationToken)
      .ConfigureAwait(false);

  public async Task<IReadOnlyList<NotificationPreferenceSnapshot>> SavePreferencesAsync(
    OrganizationId organizationId,
    UserId userId,
    IReadOnlyList<NotificationPreferenceWriteModel> preferences,
    DateTimeOffset savedAt,
    CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(preferences);

    var normalizedPreferences = preferences
      .Select(preference => new NotificationPreferenceWriteModel(
        NotificationCatalog.NormalizeToken(preference.Category),
        NotificationCatalog.NormalizeToken(preference.Channel),
        preference.IsEnabled,
        preference.IsMandatory))
      .ToArray();
    var existingPreferences = await dbContext.NotificationPreferenceRecords
      .IgnoreQueryFilters()
      .Where(preference => preference.OrganizationId == organizationId && preference.UserId == userId)
      .ToListAsync(cancellationToken)
      .ConfigureAwait(false);
    var existingByKey = existingPreferences.ToDictionary(
      preference => $"{preference.Category}:{preference.Channel}",
      StringComparer.OrdinalIgnoreCase);

    foreach (var preference in normalizedPreferences)
    {
      var key = $"{preference.Category}:{preference.Channel}";
      if (existingByKey.TryGetValue(key, out var existing))
      {
        existing.Apply(preference.IsEnabled, preference.IsMandatory, savedAt);
        continue;
      }

      dbContext.NotificationPreferenceRecords.Add(
        NotificationPreferenceRecord.Create(
          organizationId,
          userId,
          preference.Category,
          preference.Channel,
          preference.IsEnabled,
          preference.IsMandatory,
          savedAt));
    }

    await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

    return await ListPreferencesAsync(organizationId, userId, cancellationToken)
      .ConfigureAwait(false);
  }

  private async Task<int> CountUnreadCoreAsync(
    OrganizationId organizationId,
    UserId userId,
    CancellationToken cancellationToken)
  {
    var disabledPreferences = await ListDisabledPreferencesAsync(organizationId, userId, cancellationToken)
      .ConfigureAwait(false);
    var visibleCategories = await ListVisibleCategoriesAsync(organizationId, userId, cancellationToken)
      .ConfigureAwait(false);

    return await ApplyPreferenceVisibility(
        ApplyPermissionVisibility(
          BuildUserScopedQuery(organizationId, userId, includeArchived: false),
          visibleCategories),
        disabledPreferences)
      .CountAsync(notification => !notification.IsRead, cancellationToken)
      .ConfigureAwait(false);
  }

  private IQueryable<NotificationRecord> BuildUserScopedQuery(
    OrganizationId organizationId,
    UserId userId,
    bool includeArchived)
  {
    var query = dbContext.NotificationRecords
      .IgnoreQueryFilters()
      .Where(notification =>
        notification.OrganizationId == organizationId &&
        notification.RecipientUserId.HasValue &&
        notification.RecipientUserId.Value == userId);

    if (!includeArchived)
    {
      query = query.Where(notification => notification.DeletedAt == null);
    }

    return query;
  }

#pragma warning disable CA1304, CA1311, CA1862
  private static IQueryable<NotificationRecord> ApplyPermissionVisibility(
    IQueryable<NotificationRecord> query,
    IReadOnlySet<string> visibleCategories)
  {
    if (visibleCategories.Count == 0)
    {
      return query.Where(notification => false);
    }

    return query.Where(notification => visibleCategories.Contains(notification.Category.ToLower()));
  }

  private static IQueryable<NotificationRecord> ApplyPreferenceVisibility(
    IQueryable<NotificationRecord> query,
    IReadOnlyList<DisabledNotificationPreference> disabledPreferences)
  {
    foreach (var preference in disabledPreferences)
    {
      var category = preference.Category;
      var channel = preference.Channel;
      query = query.Where(notification =>
        notification.Category.ToLower() != category ||
        notification.Channel.ToLower() != channel);
    }

    return query;
  }

  private static IQueryable<NotificationRecord> ApplyFilters(
    IQueryable<NotificationRecord> query,
    NotificationListRequestDto request)
  {
    if (!string.IsNullOrWhiteSpace(request.Category))
    {
      var category = NotificationCatalog.NormalizeToken(request.Category);
      query = query.Where(notification => notification.Category.ToLower() == category);
    }

    if (!string.IsNullOrWhiteSpace(request.Channel))
    {
      var channel = NotificationCatalog.NormalizeToken(request.Channel);
      query = query.Where(notification => notification.Channel.ToLower() == channel);
    }

    if (request.IsRead.HasValue)
    {
      query = query.Where(notification => notification.IsRead == request.IsRead.Value);
    }

    if (!string.IsNullOrWhiteSpace(request.Search))
    {
      var search = request.Search.Trim().ToLower();
      query = query.Where(notification =>
        notification.Category.ToLower().Contains(search) ||
        notification.EventName.ToLower().Contains(search) ||
        notification.SubjectEntityType.ToLower().Contains(search) ||
        notification.SubjectEntityId.ToLower().Contains(search) ||
        (notification.SubjectDisplayName ?? string.Empty).ToLower().Contains(search));
    }

    return query;
  }
#pragma warning restore CA1304, CA1311, CA1862

  private static IQueryable<NotificationRecord> ApplySort(
    IQueryable<NotificationRecord> query,
    string? sort) =>
    NotificationCatalog.NormalizeToken(sort) switch
    {
      "oldest" or "occurred-at" => query.OrderBy(notification => notification.OccurredAt),
      "category" => query.OrderBy(notification => notification.Category).ThenByDescending(notification => notification.OccurredAt),
      "channel" => query.OrderBy(notification => notification.Channel).ThenByDescending(notification => notification.OccurredAt),
      "read-state" or "read" => query.OrderBy(notification => notification.IsRead).ThenByDescending(notification => notification.OccurredAt),
      "created-at" => query.OrderByDescending(notification => notification.CreatedAt),
      _ => query.OrderByDescending(notification => notification.OccurredAt).ThenByDescending(notification => notification.CreatedAt)
    };

  private static NotificationRecordSnapshot ToSnapshot(NotificationRecord notification) =>
    new(
      notification.Id,
      notification.EventId,
      notification.RecipientUserId.HasValue ? notification.RecipientUserId.Value.Value : null,
      notification.Category,
      notification.EventName,
      notification.Channel,
      notification.DeliveryStatus,
      DeserializePayload(notification.PayloadJson),
      notification.SubjectEntityType,
      notification.SubjectEntityId,
      notification.SubjectDisplayName,
      notification.OccurredAt,
      notification.CreatedAt,
      notification.IsRead,
      notification.ReadAt,
      notification.IsDeleted,
      notification.DeletedAt,
      notification.CorrelationId);

  private static Dictionary<string, string> DeserializePayload(string json)
  {
    if (string.IsNullOrWhiteSpace(json))
    {
      return new Dictionary<string, string>(StringComparer.Ordinal);
    }

    return InfrastructureJsonSerializer.Deserialize<Dictionary<string, string>>(json);
  }

  private async Task<IReadOnlyList<DisabledNotificationPreference>> ListDisabledPreferencesAsync(
    OrganizationId organizationId,
    UserId userId,
    CancellationToken cancellationToken) =>
    await dbContext.NotificationPreferenceRecords
      .IgnoreQueryFilters()
      .AsNoTracking()
      .Where(preference =>
        preference.OrganizationId == organizationId &&
        preference.UserId == userId &&
        !preference.IsEnabled)
      .Select(preference => new DisabledNotificationPreference(preference.Category, preference.Channel))
      .ToListAsync(cancellationToken)
      .ConfigureAwait(false);

  private async Task<IReadOnlySet<string>> ListVisibleCategoriesAsync(
    OrganizationId organizationId,
    UserId userId,
    CancellationToken cancellationToken)
  {
    var membership = await dbContext.IdentityMemberships
      .IgnoreQueryFilters()
      .AsNoTracking()
      .FirstOrDefaultAsync(
        candidate =>
          candidate.OrganizationId == organizationId &&
          candidate.UserId == userId &&
          candidate.DeletedAt == null &&
          candidate.Status == IdentityMembershipStatus.Active,
        cancellationToken)
      .ConfigureAwait(false);
    if (membership is null)
    {
      return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    }

    var permissions = new HashSet<string>(membership.PermissionCodes, StringComparer.OrdinalIgnoreCase);
    permissions.UnionWith(rolePermissionCatalog.GetPermissionsForRoles(membership.RoleCodes));

    return NotificationCatalog.CategoryCodes
      .Where(category =>
      {
        var sourcePermission = NotificationCatalog.GetReadPermissionForCategory(category);
        return sourcePermission is not null &&
          HasPermission(permissions, PermissionCodes.Read(PermissionModules.Notifications)) &&
          HasPermission(permissions, sourcePermission);
      })
      .ToHashSet(StringComparer.OrdinalIgnoreCase);
  }

  private static bool HasPermission(HashSet<string> permissions, string permissionCode) =>
    permissions.Contains(PermissionCodes.Wildcard) ||
    permissions.Contains(PermissionCodes.Normalize(permissionCode));

  private sealed record DisabledNotificationPreference(string Category, string Channel);
}
