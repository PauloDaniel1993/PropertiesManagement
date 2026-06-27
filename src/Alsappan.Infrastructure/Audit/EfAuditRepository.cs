using Alsappan.Application.Audit;
using Alsappan.Application.Audit.Repositories;
using Alsappan.Application.Common.Contracts;
using Alsappan.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Alsappan.Infrastructure.Audit;

public sealed class EfAuditRepository : IAuditRepository
{
  private readonly AlsappanDbContext _dbContext;

  public EfAuditRepository(AlsappanDbContext dbContext)
  {
    _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
  }

  public async Task<PagedResultDto<AuditEntryRecord>> ListAsync(
    AuditListRequestDto request,
    CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(request);
    var filter = request.ToListFilter();
    var query = ApplyFilters(_dbContext.AuditLogEntries.AsNoTracking(), request);
    var totalItems = await query.CountAsync(cancellationToken).ConfigureAwait(false);
    var items = await ApplySorting(query, request.Sort)
      .Skip(filter.Offset)
      .Take(filter.PageSize)
      .Select(entry => new
      {
        entry.Action,
        entry.ActorDisplayName,
        entry.ActorKind,
        ActorUserId = entry.ActorUserId.HasValue ? entry.ActorUserId.Value.Value : (Guid?)null,
        entry.Category,
        entry.ChangedFieldsJson,
        entry.ContextJson,
        entry.CorrelationId,
        entry.Id,
        entry.OccurredAt,
        entry.TargetDisplayName,
        entry.TargetEntityId,
        entry.TargetEntityType,
      })
      .ToListAsync(cancellationToken)
      .ConfigureAwait(false);

    return new PagedResultDto<AuditEntryRecord>(
      items.Select(item => new AuditEntryRecord(
        item.Id,
        item.Action,
        item.Category,
        item.OccurredAt,
        item.ActorKind,
        item.ActorUserId,
        item.ActorDisplayName,
        item.TargetEntityType,
        item.TargetEntityId,
        item.TargetDisplayName,
        DeserializeDictionary(item.ChangedFieldsJson),
        DeserializeDictionary(item.ContextJson),
        item.CorrelationId)).ToArray(),
      filter.Page,
      filter.PageSize,
      totalItems);
  }

  public async Task<AuditEntryRecord?> GetAsync(
    Guid id,
    CancellationToken cancellationToken = default)
  {
    var item = await _dbContext.AuditLogEntries
      .AsNoTracking()
      .Where(entry => entry.Id == id)
      .Select(entry => new
      {
        entry.Action,
        entry.ActorDisplayName,
        entry.ActorKind,
        ActorUserId = entry.ActorUserId.HasValue ? entry.ActorUserId.Value.Value : (Guid?)null,
        entry.Category,
        entry.ChangedFieldsJson,
        entry.ContextJson,
        entry.CorrelationId,
        entry.Id,
        entry.OccurredAt,
        entry.TargetDisplayName,
        entry.TargetEntityId,
        entry.TargetEntityType,
      })
      .SingleOrDefaultAsync(cancellationToken)
      .ConfigureAwait(false);

    return item is null
      ? null
      : new AuditEntryRecord(
        item.Id,
        item.Action,
        item.Category,
        item.OccurredAt,
        item.ActorKind,
        item.ActorUserId,
        item.ActorDisplayName,
        item.TargetEntityType,
        item.TargetEntityId,
        item.TargetDisplayName,
        DeserializeDictionary(item.ChangedFieldsJson),
        DeserializeDictionary(item.ContextJson),
        item.CorrelationId);
  }

#pragma warning disable CA1304, CA1311, CA1862
  private static IQueryable<AuditLogEntry> ApplyFilters(
    IQueryable<AuditLogEntry> query,
    AuditListRequestDto request)
  {
    if (!string.IsNullOrWhiteSpace(request.Action))
    {
      var action = request.Action.Trim();
      query = query.Where(entry => entry.Action == action);
    }

    if (!string.IsNullOrWhiteSpace(request.Actor))
    {
      var actor = request.Actor.Trim().ToLower();
      query = query.Where(entry =>
        (entry.ActorDisplayName ?? string.Empty).ToLower().Contains(actor) ||
        entry.ActorKind.ToLower().Contains(actor));
    }

    if (!string.IsNullOrWhiteSpace(request.EntityType))
    {
      var entityType = request.EntityType.Trim();
      query = query.Where(entry => entry.TargetEntityType == entityType);
    }

    if (!string.IsNullOrWhiteSpace(request.EntityId))
    {
      var entityId = request.EntityId.Trim();
      query = query.Where(entry => entry.TargetEntityId == entityId);
    }

    if (!string.IsNullOrWhiteSpace(request.Category))
    {
      var category = request.Category.Trim();
      query = query.Where(entry => entry.Category == category);
    }

    if (request.From.HasValue)
    {
      var from = request.From.Value.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
      query = query.Where(entry => entry.OccurredAt >= new DateTimeOffset(from));
    }

    if (request.To.HasValue)
    {
      var toExclusive = request.To.Value.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
      query = query.Where(entry => entry.OccurredAt < new DateTimeOffset(toExclusive));
    }

    if (!string.IsNullOrWhiteSpace(request.Search))
    {
      var search = request.Search.Trim().ToLower();
      query = query.Where(entry =>
        entry.Action.ToLower().Contains(search) ||
        entry.Category.ToLower().Contains(search) ||
        entry.TargetEntityType.ToLower().Contains(search) ||
        entry.TargetEntityId.ToLower().Contains(search) ||
        (entry.TargetDisplayName ?? string.Empty).ToLower().Contains(search) ||
        (entry.ActorDisplayName ?? string.Empty).ToLower().Contains(search));
    }

    return query;
  }
#pragma warning restore CA1304, CA1311, CA1862

  private static IQueryable<AuditLogEntry> ApplySorting(
    IQueryable<AuditLogEntry> query,
    string? sort) =>
    sort?.Trim().ToUpperInvariant() switch
    {
      "OLDEST" or "OCCURRED-AT" => query.OrderBy(entry => entry.OccurredAt),
      "ACTION" => query.OrderBy(entry => entry.Action).ThenByDescending(entry => entry.OccurredAt),
      "CATEGORY" => query.OrderBy(entry => entry.Category).ThenByDescending(entry => entry.OccurredAt),
      "ACTOR" => query.OrderBy(entry => entry.ActorDisplayName).ThenByDescending(entry => entry.OccurredAt),
      "TARGET" => query.OrderBy(entry => entry.TargetEntityType).ThenBy(entry => entry.TargetDisplayName),
      _ => query.OrderByDescending(entry => entry.OccurredAt),
    };

  private static Dictionary<string, string> DeserializeDictionary(string json)
  {
    if (string.IsNullOrWhiteSpace(json))
    {
      return new Dictionary<string, string>(StringComparer.Ordinal);
    }

    return InfrastructureJsonSerializer.Deserialize<Dictionary<string, string>>(json);
  }
}
