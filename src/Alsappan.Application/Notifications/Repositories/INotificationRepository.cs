using Alsappan.Application.Common.Contracts;
using Alsappan.Domain.Common.Identifiers;

namespace Alsappan.Application.Notifications.Repositories;

public interface INotificationRepository
{
  Task<PagedResultDto<NotificationRecordSnapshot>> ListAsync(
    NotificationListRequestDto request,
    OrganizationId organizationId,
    UserId userId,
    CancellationToken cancellationToken = default);

  Task<int> CountUnreadAsync(
    OrganizationId organizationId,
    UserId userId,
    CancellationToken cancellationToken = default);

  Task<NotificationRecordSnapshot?> MarkReadAsync(
    Guid id,
    OrganizationId organizationId,
    UserId userId,
    DateTimeOffset readAt,
    CancellationToken cancellationToken = default);

  Task<int> MarkAllReadAsync(
    OrganizationId organizationId,
    UserId userId,
    DateTimeOffset readAt,
    CancellationToken cancellationToken = default);

  Task<bool> ArchiveAsync(
    Guid id,
    OrganizationId organizationId,
    UserId userId,
    DateTimeOffset archivedAt,
    CancellationToken cancellationToken = default);

  Task<IReadOnlyList<NotificationPreferenceSnapshot>> ListPreferencesAsync(
    OrganizationId organizationId,
    UserId userId,
    CancellationToken cancellationToken = default);

  Task<IReadOnlyList<NotificationPreferenceSnapshot>> SavePreferencesAsync(
    OrganizationId organizationId,
    UserId userId,
    IReadOnlyList<NotificationPreferenceWriteModel> preferences,
    DateTimeOffset savedAt,
    CancellationToken cancellationToken = default);
}

public sealed record NotificationRecordSnapshot(
  Guid Id,
  Guid EventId,
  Guid? RecipientUserId,
  string Category,
  string EventName,
  string Channel,
  string DeliveryStatus,
  IReadOnlyDictionary<string, string> Payload,
  string SubjectEntityType,
  string SubjectEntityId,
  string? SubjectDisplayName,
  DateTimeOffset OccurredAt,
  DateTimeOffset CreatedAt,
  bool IsRead,
  DateTimeOffset? ReadAt,
  bool IsArchived,
  DateTimeOffset? ArchivedAt,
  string? CorrelationId);

public sealed record NotificationPreferenceWriteModel(
  string Category,
  string Channel,
  bool IsEnabled,
  bool IsMandatory);

public sealed record NotificationPreferenceSnapshot(
  string Category,
  string Channel,
  bool IsEnabled,
  DateTimeOffset CreatedAt,
  DateTimeOffset? UpdatedAt);
