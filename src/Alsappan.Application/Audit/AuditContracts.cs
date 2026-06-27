using Alsappan.Application.Common.Contracts;

namespace Alsappan.Application.Audit;

public sealed record AuditListRequestDto(
  int Page = ListFilterDto.DefaultPage,
  int PageSize = ListFilterDto.DefaultPageSize,
  string? Search = null,
  string? Action = null,
  string? Actor = null,
  string? EntityType = null,
  string? EntityId = null,
  string? Category = null,
  DateOnly? From = null,
  DateOnly? To = null,
  string? Sort = null,
  string? Locale = null)
{
  public ListFilterDto ToListFilter() =>
    new(Page, PageSize, Search, Sort, includeArchived: false, Locale, From, To);
}

public sealed record AuditEntryDto(
  Guid Id,
  string Action,
  string ActionLabel,
  AuditCategoryLabelDto Category,
  DateTimeOffset OccurredAt,
  string ActorKind,
  Guid? ActorUserId,
  string ActorDisplayName,
  string TargetEntityType,
  string TargetEntityId,
  string TargetDisplayName,
  IReadOnlyDictionary<string, string> ChangedFields,
  IReadOnlyDictionary<string, string> Context,
  string? CorrelationId);

public sealed record AuditCategoryLabelDto(string Code, string Label, string Tone = "neutral");
