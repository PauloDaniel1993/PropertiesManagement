using Alsappan.Application.Common.Contracts;
using Alsappan.Application.Common.Results;

namespace Alsappan.Application.Notifications;

public interface INotificationService
{
  Task<ApplicationOperationResult<PagedResultDto<NotificationListItemDto>>> ListAsync(
    NotificationListRequestDto request,
    CancellationToken cancellationToken = default);

  Task<ApplicationOperationResult<NotificationUnreadCountDto>> GetUnreadCountAsync(
    CancellationToken cancellationToken = default);

  Task<ApplicationOperationResult<NotificationListItemDto>> MarkReadAsync(
    Guid id,
    string? locale = null,
    CancellationToken cancellationToken = default);

  Task<ApplicationOperationResult<NotificationMarkAllReadResultDto>> MarkAllReadAsync(
    CancellationToken cancellationToken = default);

  Task<ApplicationOperationResult> ArchiveAsync(
    Guid id,
    CancellationToken cancellationToken = default);

  Task<IReadOnlyList<SelectOptionDto>> GetCategoryOptionsAsync(
    string? locale = null,
    CancellationToken cancellationToken = default);

  Task<IReadOnlyList<SelectOptionDto>> GetChannelOptionsAsync(
    string? locale = null,
    CancellationToken cancellationToken = default);

  Task<ApplicationOperationResult<NotificationPreferencesDto>> GetPreferencesAsync(
    string? locale = null,
    CancellationToken cancellationToken = default);

  Task<ApplicationOperationResult<NotificationPreferencesDto>> UpdatePreferencesAsync(
    NotificationPreferenceUpdateRequestDto request,
    string? locale = null,
    CancellationToken cancellationToken = default);
}
