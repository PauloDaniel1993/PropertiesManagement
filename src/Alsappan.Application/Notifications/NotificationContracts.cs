using Alsappan.Application.Common.Contracts;

namespace Alsappan.Application.Notifications;

public sealed record NotificationListRequestDto(
  int Page = ListFilterDto.DefaultPage,
  int PageSize = ListFilterDto.DefaultPageSize,
  string? Search = null,
  string? Category = null,
  bool? IsRead = null,
  string? Channel = null,
  bool IncludeArchived = false,
  string? Sort = null,
  string? Locale = null)
{
  public ListFilterDto ToListFilter() =>
    new(Page, PageSize, Search, Sort, IncludeArchived, Locale);
}

public sealed record NotificationListItemDto(
  Guid Id,
  Guid EventId,
  Guid? RecipientUserId,
  StatusLabelDto Category,
  string EventName,
  string EventTypeLabel,
  string Title,
  string Message,
  string? DeepLink,
  StatusLabelDto Channel,
  StatusLabelDto DeliveryStatus,
  StatusLabelDto ReadState,
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

public sealed record NotificationUnreadCountDto(int Count);

public sealed record NotificationMarkAllReadResultDto(int UpdatedCount);

public sealed record NotificationPreferenceDto(
  string Category,
  string CategoryLabel,
  string Channel,
  string ChannelLabel,
  bool IsEnabled,
  bool IsMandatory);

public sealed record NotificationPreferencesDto(
  IReadOnlyList<NotificationPreferenceDto> Preferences,
  bool IsPersisted,
  string Notice);

public sealed record NotificationPreferenceUpdateRequestDto(
  IReadOnlyList<NotificationPreferenceUpdateItemDto> Preferences);

public sealed record NotificationPreferenceUpdateItemDto(
  string Category,
  string Channel,
  bool IsEnabled);
