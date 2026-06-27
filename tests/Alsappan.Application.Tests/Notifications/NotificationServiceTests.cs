using Alsappan.Application.Common.Auth;
using Alsappan.Application.Common.Authorization;
using Alsappan.Application.Common.Contracts;
using Alsappan.Application.Common.Results;
using Alsappan.Application.Common.Tenancy;
using Alsappan.Application.Notifications;
using Alsappan.Application.Notifications.Repositories;
using Alsappan.Domain.Common.Identifiers;

namespace Alsappan.Application.Tests.Notifications;

public sealed class NotificationServiceTests
{
  private static readonly Guid NotificationId = new("55555555-5555-5555-5555-555555555555");

  [Fact]
  public async Task ListAsyncLocalizesCanonicalEventsAndBuildsDeepLinks()
  {
    var organizationId = OrganizationId.New();
    var service = CreateService(
      organizationId,
      [CreateSnapshot("payments", "payment.overdue", "payment", "payment-a", "Aluguel junho")],
      [PermissionCodes.Read(PermissionModules.Notifications)]);

    var result = await service.ListAsync(new NotificationListRequestDto(Locale: "en-US"));

    Assert.True(result.Succeeded);
    var item = Assert.Single(result.Value!.Items);
    Assert.Equal("Payment overdue", item.Title);
    Assert.Equal("Aluguel junho has an overdue payment.", item.Message);
    Assert.Equal("Payments", item.Category.Label);
    Assert.Equal("/pagamentos?paymentId=payment-a", item.DeepLink);
  }

  [Fact]
  public async Task NotificationOperationsRequireReadPermission()
  {
    var service = CreateService(
      OrganizationId.New(),
      [CreateSnapshot("properties", "property.updated", "property", "property-a", "Casa")],
      []);

    var list = await service.ListAsync(new NotificationListRequestDto());
    var unread = await service.GetUnreadCountAsync();
    var markRead = await service.MarkReadAsync(NotificationId);

    Assert.Equal(ApplicationOperationFailure.Forbidden, list.Failure);
    Assert.Equal(ApplicationOperationFailure.Forbidden, unread.Failure);
    Assert.Equal(ApplicationOperationFailure.Forbidden, markRead.Failure);
  }

  [Fact]
  public async Task MarkAllReadReturnsUpdatedCount()
  {
    var repository = new FakeNotificationRepository(
      [CreateSnapshot("properties", "property.updated", "property", "property-a", "Casa")]);
    var service = CreateService(
      OrganizationId.New(),
      [],
      [PermissionCodes.Read(PermissionModules.Notifications)],
      repository);

    var result = await service.MarkAllReadAsync();

    Assert.True(result.Succeeded);
    Assert.Equal(1, result.Value!.UpdatedCount);
    Assert.Equal(1, repository.MarkAllReadCalls);
  }

  [Fact]
  public async Task PreferencesPersistDefaultsAndValidateCatalogValues()
  {
    var repository = new FakeNotificationRepository([]);
    var service = CreateService(
      OrganizationId.New(),
      [],
      [PermissionCodes.Read(PermissionModules.Notifications)],
      repository);

    var preferences = await service.GetPreferencesAsync("pt-BR");
    var saved = await service.UpdatePreferencesAsync(
      new NotificationPreferenceUpdateRequestDto(
        [
          new NotificationPreferenceUpdateItemDto("payments", "in-app", false),
          new NotificationPreferenceUpdateItemDto("system", "in-app", false)
        ]),
      "pt-BR");
    var invalid = await service.UpdatePreferencesAsync(
      new NotificationPreferenceUpdateRequestDto(
        [new NotificationPreferenceUpdateItemDto("unknown", "in-app", true)]));

    Assert.True(preferences.Succeeded);
    Assert.False(preferences.Value!.IsPersisted);
    Assert.Contains(preferences.Value.Preferences, item => item.Category == "occurrences" && item.Channel == "in-app" && item.IsEnabled);
    Assert.Contains(preferences.Value.Preferences, item => item.Category == "occurrences" && item.Channel == "email" && !item.IsEnabled);
    Assert.True(saved.Succeeded);
    Assert.True(saved.Value!.IsPersisted);
    Assert.Contains(saved.Value.Preferences, item => item.Category == "payments" && item.Channel == "in-app" && !item.IsEnabled);
    Assert.Contains(saved.Value.Preferences, item => item.Category == "system" && item.Channel == "in-app" && item.IsEnabled);
    Assert.Equal(ApplicationOperationFailure.Validation, invalid.Failure);
  }

  private static NotificationService CreateService(
    OrganizationId organizationId,
    IReadOnlyList<NotificationRecordSnapshot> notifications,
    IEnumerable<string> permissions,
    FakeNotificationRepository? repository = null) =>
    new(
      repository ?? new FakeNotificationRepository(notifications),
      new FixedPermissionService(permissions),
      new FixedActiveOrganizationContextResolver(organizationId),
      TimeProvider.System);

  private static NotificationRecordSnapshot CreateSnapshot(
    string category,
    string eventName,
    string subjectEntityType,
    string subjectEntityId,
    string subjectDisplayName) =>
    new(
      NotificationId,
      Guid.NewGuid(),
      null,
      category,
      eventName,
      "in-app",
      "pending",
      new Dictionary<string, string> { ["status"] = "overdue" },
      subjectEntityType,
      subjectEntityId,
      subjectDisplayName,
      DateTimeOffset.UtcNow.AddMinutes(-5),
      DateTimeOffset.UtcNow,
      false,
      null,
      false,
      null,
      "request-1");

  private sealed class FixedPermissionService : IPermissionService
  {
    private readonly HashSet<string> permissions;

    public FixedPermissionService(IEnumerable<string> permissions)
    {
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
      var organizationId = OrganizationId.New();
      var granted = permissions.Contains(PermissionCodes.Wildcard) ||
        permissions.Contains(requirement.PermissionCode);
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

    public FixedActiveOrganizationContextResolver(OrganizationId organizationId)
    {
      var membership = new OrganizationMembership(
        organizationId,
        permissionCodes: [PermissionCodes.Read(PermissionModules.Notifications)]);
      var user = new AuthenticatedUser(UserId.New(), "admin@alsappan.local", "Paulo", [membership]);
      context = new ActiveOrganizationContext(user, membership);
    }

    public ValueTask<ActiveOrganizationResolutionResult> ResolveAsync(
      CancellationToken cancellationToken = default) =>
      ValueTask.FromResult(ActiveOrganizationResolutionResult.Success(context));
  }

  private sealed class FakeNotificationRepository : INotificationRepository
  {
    private readonly List<NotificationRecordSnapshot> notifications;
    private readonly List<NotificationPreferenceWriteModel> preferences = [];

    public FakeNotificationRepository(IReadOnlyList<NotificationRecordSnapshot> notifications)
    {
      this.notifications = notifications.ToList();
    }

    public int MarkAllReadCalls { get; private set; }

    public Task<PagedResultDto<NotificationRecordSnapshot>> ListAsync(
      NotificationListRequestDto request,
      OrganizationId organizationId,
      UserId userId,
      CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();
      return Task.FromResult(
        new PagedResultDto<NotificationRecordSnapshot>(
          notifications,
          request.Page,
          request.PageSize,
          notifications.Count));
    }

    public Task<int> CountUnreadAsync(
      OrganizationId organizationId,
      UserId userId,
      CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();
      return Task.FromResult(notifications.Count(notification => !notification.IsRead));
    }

    public Task<NotificationRecordSnapshot?> MarkReadAsync(
      Guid id,
      OrganizationId organizationId,
      UserId userId,
      DateTimeOffset readAt,
      CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();
      var notification = notifications.FirstOrDefault(item => item.Id == id);
      return Task.FromResult(notification is null ? null : notification with { IsRead = true, ReadAt = readAt });
    }

    public Task<int> MarkAllReadAsync(
      OrganizationId organizationId,
      UserId userId,
      DateTimeOffset readAt,
      CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();
      MarkAllReadCalls++;
      return Task.FromResult(notifications.Count(notification => !notification.IsRead));
    }

    public Task<bool> ArchiveAsync(
      Guid id,
      OrganizationId organizationId,
      UserId userId,
      DateTimeOffset archivedAt,
      CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();
      return Task.FromResult(notifications.Any(notification => notification.Id == id));
    }

    public Task<IReadOnlyList<NotificationPreferenceSnapshot>> ListPreferencesAsync(
      OrganizationId organizationId,
      UserId userId,
      CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();
      return Task.FromResult<IReadOnlyList<NotificationPreferenceSnapshot>>(
        preferences
          .Select(preference => new NotificationPreferenceSnapshot(
            preference.Category,
            preference.Channel,
            preference.IsEnabled,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow))
          .ToArray());
    }

    public Task<IReadOnlyList<NotificationPreferenceSnapshot>> SavePreferencesAsync(
      OrganizationId organizationId,
      UserId userId,
      IReadOnlyList<NotificationPreferenceWriteModel> nextPreferences,
      DateTimeOffset savedAt,
      CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();
      preferences.Clear();
      preferences.AddRange(nextPreferences);

      return Task.FromResult<IReadOnlyList<NotificationPreferenceSnapshot>>(
        nextPreferences
          .Select(preference => new NotificationPreferenceSnapshot(
            preference.Category,
            preference.Channel,
            preference.IsEnabled,
            savedAt,
            savedAt))
          .ToArray());
    }
  }
}
