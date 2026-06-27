using Alsappan.Application.Common.Contracts;
using Alsappan.Application.Timeline;
using Alsappan.Application.Timeline.Repositories;
using Alsappan.Domain.Common.Events;
using Alsappan.Domain.Common.Identifiers;
using Alsappan.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Alsappan.Infrastructure.Timeline;

public sealed class EfTimelineRepository : ITimelineRepository
{
  private readonly AlsappanDbContext dbContext;

  public EfTimelineRepository(AlsappanDbContext dbContext)
  {
    this.dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
  }

  public async Task<PagedResultDto<TimelineEntryRecord>> ListAsync(
    TimelineListRequestDto request,
    OrganizationId organizationId,
    IReadOnlySet<string>? readableSubjectEntityTypes,
    CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(request);

    var listFilter = CreateListFilter(request.Page, request.PageSize, request.From, request.To, request.Sort);
    if (readableSubjectEntityTypes is { Count: 0 })
    {
      return EmptyPage(listFilter);
    }

    var query = ApplyCommonFilters(
      dbContext.TimelineEntries.IgnoreQueryFilters(),
      organizationId,
      request.EventType,
      request.ActorUserId,
      request.From,
      request.To,
      readableSubjectEntityTypes);

    if (!string.IsNullOrWhiteSpace(request.EntityType))
    {
      var entityType = TimelineCatalog.NormalizeEntityType(request.EntityType);
      query = query.Where(entry => entry.SubjectEntityType == entityType);
    }

    if (HasRelatedEntityFilter(request.RelatedEntityType, request.RelatedEntityId))
    {
      query = ApplyRelatedEntityJsonPrefilter(
        query,
        request.RelatedEntityType,
        request.RelatedEntityId);

      return await MaterializeFilterAndPageAsync(
          query,
          listFilter,
          request.Sort,
          record => MatchesRelatedEntity(record, request.RelatedEntityType, request.RelatedEntityId),
          readableSubjectEntityTypes,
          cancellationToken)
        .ConfigureAwait(false);
    }

    return await PageServerQueryAsync(
        query,
        listFilter,
        request.Sort,
        readableSubjectEntityTypes,
        cancellationToken)
      .ConfigureAwait(false);
  }

  public async Task<PagedResultDto<TimelineEntryRecord>> ListEntityAsync(
    string entityType,
    string entityId,
    TimelineEntityListRequestDto request,
    OrganizationId organizationId,
    IReadOnlySet<string>? readableSubjectEntityTypes,
    CancellationToken cancellationToken = default)
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(entityType);
    ArgumentException.ThrowIfNullOrWhiteSpace(entityId);
    ArgumentNullException.ThrowIfNull(request);

    var listFilter = CreateListFilter(request.Page, request.PageSize, request.From, request.To, request.Sort);
    if (readableSubjectEntityTypes is { Count: 0 })
    {
      return EmptyPage(listFilter);
    }

    var normalizedEntityType = TimelineCatalog.NormalizeEntityType(entityType);
    var normalizedEntityId = entityId.Trim();
    var query = ApplyCommonFilters(
      dbContext.TimelineEntries.IgnoreQueryFilters(),
      organizationId,
      request.EventType,
      request.ActorUserId,
      request.From,
      request.To,
      readableSubjectEntityTypes);
    query = ApplyEntityJsonPrefilter(query, normalizedEntityType, normalizedEntityId);

    return await MaterializeFilterAndPageAsync(
        query,
        listFilter,
        request.Sort,
        record => MatchesEntity(record, normalizedEntityType, normalizedEntityId) &&
          MatchesRelatedEntity(record, request.RelatedEntityType, request.RelatedEntityId),
        readableSubjectEntityTypes,
        cancellationToken)
      .ConfigureAwait(false);
  }

  private static IQueryable<TimelineEntry> ApplyCommonFilters(
    IQueryable<TimelineEntry> query,
    OrganizationId organizationId,
    string? eventType,
    Guid? actorUserId,
    DateOnly? from,
    DateOnly? to,
    IReadOnlySet<string>? readableSubjectEntityTypes)
  {
    query = query.Where(entry => entry.OrganizationId == organizationId);

    if (!string.IsNullOrWhiteSpace(eventType))
    {
      var normalizedEventType = eventType.Trim();
      query = query.Where(entry => entry.EventName == normalizedEventType);
    }

    if (actorUserId.HasValue)
    {
      var userId = new UserId(actorUserId.Value);
      query = query.Where(entry => entry.ActorUserId == userId);
    }

    if (from.HasValue)
    {
      var start = ToUtcDateTimeOffset(from.Value);
      query = query.Where(entry => entry.OccurredAt >= start);
    }

    if (to.HasValue)
    {
      var endExclusive = ToUtcDateTimeOffset(to.Value.AddDays(1));
      query = query.Where(entry => entry.OccurredAt < endExclusive);
    }

    if (readableSubjectEntityTypes is not null)
    {
      var readableTypes = readableSubjectEntityTypes.ToArray();
      query = query.Where(entry => readableTypes.Contains(entry.SubjectEntityType));
    }

    return query;
  }

  private static async Task<PagedResultDto<TimelineEntryRecord>> PageServerQueryAsync(
    IQueryable<TimelineEntry> query,
    ListFilterDto listFilter,
    string? sort,
    IReadOnlySet<string>? readableSubjectEntityTypes,
    CancellationToken cancellationToken)
  {
    var total = await query.CountAsync(cancellationToken).ConfigureAwait(false);
    var entries = await ApplySort(query, sort)
      .Skip(listFilter.Offset)
      .Take(listFilter.PageSize)
      .ToListAsync(cancellationToken)
      .ConfigureAwait(false);

    return new PagedResultDto<TimelineEntryRecord>(
      entries.Select(entry => ToRecord(entry, readableSubjectEntityTypes)).ToArray(),
      listFilter.Page,
      listFilter.PageSize,
      total);
  }

  private static async Task<PagedResultDto<TimelineEntryRecord>> MaterializeFilterAndPageAsync(
    IQueryable<TimelineEntry> query,
    ListFilterDto listFilter,
    string? sort,
    Func<TimelineEntryRecord, bool> predicate,
    IReadOnlySet<string>? readableSubjectEntityTypes,
    CancellationToken cancellationToken)
  {
    var entries = await ApplySort(query, sort).ToListAsync(cancellationToken)
      .ConfigureAwait(false);
    var filtered = entries
      .Select(entry => ToRecord(entry, readableSubjectEntityTypes))
      .Where(predicate)
      .ToArray();
    var items = filtered
      .Skip(listFilter.Offset)
      .Take(listFilter.PageSize)
      .ToArray();

    return new PagedResultDto<TimelineEntryRecord>(
      items,
      listFilter.Page,
      listFilter.PageSize,
      filtered.Length);
  }

  private static IQueryable<TimelineEntry> ApplySort(IQueryable<TimelineEntry> query, string? sort)
  {
    var normalizedSort = string.IsNullOrWhiteSpace(sort) ? "-occurred-at" : sort.Trim();

    return normalizedSort switch
    {
      "occurred-at" or "occurredAt" => query
        .OrderBy(entry => entry.OccurredAt)
        .ThenBy(entry => entry.Id),
      _ => query
        .OrderByDescending(entry => entry.OccurredAt)
        .ThenByDescending(entry => entry.Id)
    };
  }

  private IQueryable<TimelineEntry> ApplyEntityJsonPrefilter(
    IQueryable<TimelineEntry> query,
    string entityType,
    string entityId)
  {
    if (!SupportsPostgresJsonContains())
    {
      return query;
    }

    var relatedJsonFilter = BuildRelatedJsonFilter(entityType) ??
      throw new InvalidOperationException("Entity timeline prefilter requires an entity type.");

    return query.Where(entry =>
      (entry.SubjectEntityType == entityType && EF.Functions.ILike(entry.SubjectEntityId, entityId)) ||
      EF.Functions.JsonContains(entry.RelatedEntitiesJson, relatedJsonFilter));
  }

  private IQueryable<TimelineEntry> ApplyRelatedEntityJsonPrefilter(
    IQueryable<TimelineEntry> query,
    string? relatedEntityType,
    string? relatedEntityId)
  {
    if (!SupportsPostgresJsonContains())
    {
      return query;
    }

    var relatedJsonFilter = BuildRelatedJsonFilter(relatedEntityType);

    return relatedJsonFilter is null
      ? query
      : query.Where(entry => EF.Functions.JsonContains(entry.RelatedEntitiesJson, relatedJsonFilter));
  }

  private static TimelineEntryRecord ToRecord(
    TimelineEntry entry,
    IReadOnlySet<string>? readableSubjectEntityTypes) =>
    new(
      entry.Id,
      entry.EventId,
      entry.ModuleName,
      entry.EventName,
      entry.OccurredAt,
      new TimelineActorRecord(entry.ActorKind, entry.ActorUserId, entry.ActorDisplayName),
      new EntityReference(entry.SubjectEntityType, entry.SubjectEntityId, entry.SubjectDisplayName),
      DeserializeRelatedEntities(entry.RelatedEntitiesJson)
        .Where(related => IsReadableEntityType(related.EntityType, readableSubjectEntityTypes))
        .ToArray(),
      DeserializeData(entry.DataJson),
      entry.CorrelationId);

  private static bool MatchesEntity(
    TimelineEntryRecord record,
    string entityType,
    string entityId) =>
    IsEntity(record.Subject, entityType, entityId) ||
    record.RelatedEntities.Any(related => IsEntity(related, entityType, entityId));

  private static bool MatchesRelatedEntity(
    TimelineEntryRecord record,
    string? relatedEntityType,
    string? relatedEntityId)
  {
    if (!HasRelatedEntityFilter(relatedEntityType, relatedEntityId))
    {
      return true;
    }

    var normalizedType = string.IsNullOrWhiteSpace(relatedEntityType)
      ? null
      : TimelineCatalog.NormalizeEntityType(relatedEntityType);
    var normalizedId = Optional(relatedEntityId);

    return record.RelatedEntities.Any(related =>
      (normalizedType is null || TimelineCatalog.NormalizeEntityType(related.EntityType) == normalizedType) &&
      (normalizedId is null || string.Equals(related.EntityId, normalizedId, StringComparison.OrdinalIgnoreCase)));
  }

  private static bool IsEntity(EntityReference reference, string entityType, string entityId) =>
    TimelineCatalog.NormalizeEntityType(reference.EntityType) == entityType &&
    string.Equals(reference.EntityId, entityId, StringComparison.OrdinalIgnoreCase);

  private static bool IsReadableEntityType(
    string entityType,
    IReadOnlySet<string>? readableSubjectEntityTypes) =>
    readableSubjectEntityTypes is null ||
    readableSubjectEntityTypes.Contains(TimelineCatalog.NormalizeEntityType(entityType));

  private static bool HasRelatedEntityFilter(string? relatedEntityType, string? relatedEntityId) =>
    !string.IsNullOrWhiteSpace(relatedEntityType) || !string.IsNullOrWhiteSpace(relatedEntityId);

  private bool SupportsPostgresJsonContains() =>
    dbContext.Database.ProviderName?.Contains("Npgsql", StringComparison.OrdinalIgnoreCase) == true;

  private static string? BuildRelatedJsonFilter(string? entityType)
  {
    var filter = new Dictionary<string, string>(StringComparer.Ordinal);

    if (!string.IsNullOrWhiteSpace(entityType))
    {
      filter["entityType"] = TimelineCatalog.NormalizeEntityType(entityType);
    }

    return filter.Count == 0
      ? null
      : InfrastructureJsonSerializer.Serialize(new[] { filter });
  }

  private static EntityReference[] DeserializeRelatedEntities(string json) =>
    InfrastructureJsonSerializer.Deserialize<EntityReference[]>(json);

  private static Dictionary<string, string> DeserializeData(string json) =>
    InfrastructureJsonSerializer.Deserialize<Dictionary<string, string>>(json);

  private static ListFilterDto CreateListFilter(
    int page,
    int pageSize,
    DateOnly? from,
    DateOnly? to,
    string? sort) =>
    new(page, pageSize, sort: sort, from: from, to: to);

  private static PagedResultDto<TimelineEntryRecord> EmptyPage(ListFilterDto listFilter) =>
    new([], listFilter.Page, listFilter.PageSize, 0);

  private static DateTimeOffset ToUtcDateTimeOffset(DateOnly date) =>
    new(date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc));

  private static string? Optional(string? value) =>
    string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
