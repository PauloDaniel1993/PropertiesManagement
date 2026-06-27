using Alsappan.Application.Common.Contracts;

namespace Alsappan.Application.Timeline;

public sealed record TimelineListRequestDto(
  int Page = ListFilterDto.DefaultPage,
  int PageSize = ListFilterDto.DefaultPageSize,
  string? EntityType = null,
  string? EventType = null,
  Guid? ActorUserId = null,
  DateOnly? From = null,
  DateOnly? To = null,
  string? RelatedEntityType = null,
  string? RelatedEntityId = null,
  string? Sort = null,
  string? Locale = null);

public sealed record TimelineEntityListRequestDto(
  int Page = ListFilterDto.DefaultPage,
  int PageSize = ListFilterDto.DefaultPageSize,
  string? EventType = null,
  Guid? ActorUserId = null,
  DateOnly? From = null,
  DateOnly? To = null,
  string? RelatedEntityType = null,
  string? RelatedEntityId = null,
  string? Sort = null,
  string? Locale = null);

public sealed record TimelineActorDto(
  string Kind,
  string KindLabel,
  Guid? UserId,
  string? DisplayName);

public sealed record TimelineEntityReferenceDto(
  string EntityType,
  string EntityTypeLabel,
  string EntityId,
  string? DisplayName,
  string? Route);

public sealed record TimelineDisplayDto(
  string EventLabel,
  string Summary);

public sealed record TimelineEntryDto(
  Guid Id,
  Guid EventId,
  string ModuleName,
  string EventType,
  string EventTypeLabel,
  DateTimeOffset OccurredAt,
  TimelineActorDto Actor,
  TimelineEntityReferenceDto Subject,
  IReadOnlyList<TimelineEntityReferenceDto> RelatedEntities,
  IReadOnlyDictionary<string, string> Data,
  TimelineDisplayDto Display,
  string? CorrelationId,
  string? Route);
