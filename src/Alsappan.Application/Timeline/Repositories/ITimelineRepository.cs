using Alsappan.Application.Common.Contracts;
using Alsappan.Domain.Common.Events;
using Alsappan.Domain.Common.Identifiers;

namespace Alsappan.Application.Timeline.Repositories;

public interface ITimelineRepository
{
  Task<PagedResultDto<TimelineEntryRecord>> ListAsync(
    TimelineListRequestDto request,
    OrganizationId organizationId,
    IReadOnlySet<string>? readableSubjectEntityTypes,
    CancellationToken cancellationToken = default);

  Task<PagedResultDto<TimelineEntryRecord>> ListEntityAsync(
    string entityType,
    string entityId,
    TimelineEntityListRequestDto request,
    OrganizationId organizationId,
    IReadOnlySet<string>? readableSubjectEntityTypes,
    CancellationToken cancellationToken = default);
}

public sealed record TimelineActorRecord(
  string Kind,
  UserId? UserId,
  string? DisplayName);

public sealed record TimelineEntryRecord(
  Guid Id,
  Guid EventId,
  string ModuleName,
  string EventType,
  DateTimeOffset OccurredAt,
  TimelineActorRecord Actor,
  EntityReference Subject,
  IReadOnlyList<EntityReference> RelatedEntities,
  IReadOnlyDictionary<string, string> Data,
  string? CorrelationId);
