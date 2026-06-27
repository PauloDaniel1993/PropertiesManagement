using Alsappan.Application.Common.Contracts;

namespace Alsappan.Application.Occurrences;

public sealed record OccurrenceListRequestDto(
  int Page = ListFilterDto.DefaultPage,
  int PageSize = ListFilterDto.DefaultPageSize,
  string? Search = null,
  string? Type = null,
  string? Priority = null,
  string? Status = null,
  Guid? AssignedUserId = null,
  Guid? PropertyId = null,
  Guid? ResidentId = null,
  Guid? ContractId = null,
  DateOnly? DateFrom = null,
  DateOnly? DateTo = null,
  bool UnresolvedOnly = false,
  bool IncludeArchived = false,
  string? Sort = null,
  string? Locale = null);

public sealed record OccurrenceEntitySummaryDto(
  Guid Id,
  string Name,
  string? Description = null,
  string? Route = null);

public sealed record OccurrenceUserSummaryDto(
  Guid Id,
  string DisplayName,
  string? Email = null);

public sealed record OccurrenceDocumentDto(
  Guid DocumentId,
  string? Label,
  string Route,
  DateTimeOffset CreatedAt);

public sealed record OccurrenceCommentDto(
  Guid Id,
  string Body,
  bool IsInternal,
  Guid? AuthorUserId,
  string? AuthorDisplayName,
  DateTimeOffset CreatedAt);

public sealed record OccurrenceStatusHistoryDto(
  Guid Id,
  StatusLabelDto? PreviousStatus,
  StatusLabelDto NewStatus,
  string? Notes,
  Guid? ActorUserId,
  string? ActorDisplayName,
  DateTimeOffset CreatedAt);

public sealed record OccurrencePriorityHistoryDto(
  Guid Id,
  StatusLabelDto? PreviousPriority,
  StatusLabelDto NewPriority,
  string? Notes,
  Guid? ActorUserId,
  string? ActorDisplayName,
  DateTimeOffset CreatedAt);

public sealed record OccurrenceAssignmentHistoryDto(
  Guid Id,
  OccurrenceUserSummaryDto? PreviousAssignedUser,
  OccurrenceUserSummaryDto? NewAssignedUser,
  string? Notes,
  Guid? ActorUserId,
  string? ActorDisplayName,
  DateTimeOffset CreatedAt);

public sealed record OccurrenceListItemDto(
  Guid Id,
  string Title,
  string Description,
  StatusLabelDto Type,
  StatusLabelDto Priority,
  StatusLabelDto Status,
  OccurrenceEntitySummaryDto? Property,
  OccurrenceEntitySummaryDto? Resident,
  OccurrenceEntitySummaryDto? Contract,
  OccurrenceUserSummaryDto? AssignedUser,
  DateOnly? DueDate,
  bool IsUnresolved,
  bool IsArchived,
  DateTimeOffset CreatedAt,
  DateTimeOffset? UpdatedAt,
  string? ConcurrencyToken);

public sealed record OccurrenceDetailDto(
  Guid Id,
  string Title,
  string Description,
  StatusLabelDto Type,
  StatusLabelDto Priority,
  StatusLabelDto Status,
  OccurrenceEntitySummaryDto? Property,
  OccurrenceEntitySummaryDto? Resident,
  OccurrenceEntitySummaryDto? Contract,
  OccurrenceUserSummaryDto? AssignedUser,
  DateOnly? DueDate,
  DateTimeOffset? ResolvedAt,
  Guid? ResolvedByUserId,
  string? ResolutionNotes,
  DateTimeOffset? CancelledAt,
  Guid? CancelledByUserId,
  string? CancellationNotes,
  IReadOnlyList<OccurrenceCommentDto> Comments,
  IReadOnlyList<OccurrenceDocumentDto> Attachments,
  IReadOnlyList<OccurrenceStatusHistoryDto> StatusHistory,
  IReadOnlyList<OccurrencePriorityHistoryDto> PriorityHistory,
  IReadOnlyList<OccurrenceAssignmentHistoryDto> AssignmentHistory,
  string TimelineRoute,
  string AuditRoute,
  DateTimeOffset CreatedAt,
  DateTimeOffset? UpdatedAt,
  DateTimeOffset? ArchivedAt,
  string? ConcurrencyToken);

public sealed record OccurrenceCreateRequestDto(
  string Title,
  string Description,
  string Type,
  string Priority,
  Guid? PropertyId,
  Guid? ResidentId,
  Guid? ContractId,
  Guid? AssignedUserId,
  DateOnly? DueDate);

public sealed record OccurrenceUpdateRequestDto(
  string Title,
  string Description,
  string Type,
  Guid? PropertyId,
  Guid? ResidentId,
  Guid? ContractId,
  DateOnly? DueDate,
  string? ConcurrencyToken = null);

public sealed record OccurrenceAssignmentRequestDto(
  Guid? AssignedUserId,
  string? Notes = null);

public sealed record OccurrencePriorityChangeRequestDto(
  string Priority,
  string? Notes = null);

public sealed record OccurrenceStatusChangeRequestDto(
  string Status,
  string? Notes = null);

public sealed record OccurrenceResolutionRequestDto(
  string ResolutionNotes);

public sealed record OccurrenceLifecycleRequestDto(
  string? Notes = null);

public sealed record OccurrenceCommentRequestDto(
  string Body,
  bool IsInternal = false);

public sealed record OccurrenceAttachmentRequestDto(
  Guid DocumentId,
  string? Label = null);
