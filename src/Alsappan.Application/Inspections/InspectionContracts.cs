using Alsappan.Application.Common.Contracts;

namespace Alsappan.Application.Inspections;

public sealed record InspectionListRequestDto(
  int Page = ListFilterDto.DefaultPage,
  int PageSize = ListFilterDto.DefaultPageSize,
  string? Search = null,
  string? Type = null,
  string? Status = null,
  Guid? PropertyId = null,
  Guid? ContractId = null,
  Guid? ResidentId = null,
  Guid? AssignedUserId = null,
  DateTimeOffset? ScheduledFrom = null,
  DateTimeOffset? ScheduledTo = null,
  bool PendingOnly = false,
  bool IncludeArchived = false,
  string? Sort = null,
  string? Locale = null);

public sealed record InspectionEntitySummaryDto(
  Guid Id,
  string Name,
  string? Description = null,
  string? Route = null);

public sealed record InspectionProgressDto(
  int TotalItems,
  int CompletedItems,
  decimal Percentage);

public sealed record InspectionChecklistItemDto(
  Guid Id,
  string AreaName,
  string ItemName,
  bool IsRequired,
  StatusLabelDto ConditionRating,
  string? Observations,
  int SortOrder,
  bool IsComplete,
  DateTimeOffset CreatedAt,
  DateTimeOffset? UpdatedAt);

public sealed record InspectionDocumentDto(
  Guid DocumentId,
  Guid? ChecklistItemId,
  StatusLabelDto Kind,
  string? Label,
  string Route);

public sealed record InspectionSignatureSlotDto(
  Guid Id,
  string SignerRole,
  string? SignerName,
  bool IsRequired,
  bool IsSigned,
  DateTimeOffset? SignedAt,
  Guid? SignatureDocumentId,
  string? Notes);

public sealed record InspectionListItemDto(
  Guid Id,
  string Title,
  StatusLabelDto Type,
  StatusLabelDto Status,
  InspectionEntitySummaryDto Property,
  InspectionEntitySummaryDto? Contract,
  InspectionEntitySummaryDto? Resident,
  InspectionEntitySummaryDto Assignee,
  DateTimeOffset ScheduledAt,
  DateTimeOffset? StartedAt,
  DateTimeOffset? CompletedAt,
  InspectionProgressDto Progress,
  bool IsPending,
  bool IsArchived,
  DateTimeOffset CreatedAt,
  DateTimeOffset? UpdatedAt,
  string? ConcurrencyToken);

public sealed record InspectionDetailDto(
  Guid Id,
  string Title,
  StatusLabelDto Type,
  StatusLabelDto Status,
  InspectionEntitySummaryDto Property,
  InspectionEntitySummaryDto? Contract,
  InspectionEntitySummaryDto? Resident,
  InspectionEntitySummaryDto Assignee,
  DateTimeOffset ScheduledAt,
  DateTimeOffset? StartedAt,
  DateTimeOffset? CompletedAt,
  DateTimeOffset? CancelledAt,
  string? CompletionNotes,
  string? CancellationReason,
  string? Notes,
  InspectionProgressDto Progress,
  IReadOnlyList<InspectionChecklistItemDto> ChecklistItems,
  IReadOnlyList<InspectionDocumentDto> PhotoDocuments,
  IReadOnlyList<InspectionDocumentDto> LinkedDocuments,
  IReadOnlyList<InspectionSignatureSlotDto> SignatureSlots,
  string TimelineRoute,
  string AuditRoute,
  DateTimeOffset CreatedAt,
  DateTimeOffset? UpdatedAt,
  DateTimeOffset? ArchivedAt,
  string? ConcurrencyToken);

public sealed record InspectionSignatureSlotRequestDto(
  string SignerRole,
  string? SignerName,
  bool IsRequired = false);

public sealed record InspectionScheduleRequestDto(
  string Type,
  Guid? PropertyId,
  Guid? ContractId,
  Guid? ResidentId,
  DateTimeOffset ScheduledAt,
  Guid? AssignedUserId,
  string? Title,
  string? Notes,
  IReadOnlyList<InspectionSignatureSlotRequestDto>? SignatureSlots = null);

public sealed record InspectionUpdateRequestDto(
  string Type,
  Guid? PropertyId,
  Guid? ContractId,
  Guid? ResidentId,
  DateTimeOffset ScheduledAt,
  Guid? AssignedUserId,
  string? Title,
  string? Notes,
  IReadOnlyList<InspectionSignatureSlotRequestDto>? SignatureSlots = null,
  string? ConcurrencyToken = null);

public sealed record InspectionChecklistItemRequestDto(
  string AreaName,
  string ItemName,
  bool IsRequired,
  string ConditionRating,
  string? Observations,
  int SortOrder = 0);

public sealed record InspectionDocumentLinkRequestDto(
  Guid? DocumentId,
  Guid? ChecklistItemId,
  string Kind,
  string? Label);

public sealed record InspectionLifecycleRequestDto(string? Notes = null);
