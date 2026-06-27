using Alsappan.Domain.Common.Identifiers;

namespace Alsappan.Application.Search.Repositories;

public sealed record GlobalSearchQuery(
  string Query,
  int LimitPerEntity,
  IReadOnlySet<string>? ReadableEntityTypes);

public sealed record GlobalSearchResultRecord(
  string EntityType,
  Guid Id,
  string Label,
  string? Summary,
  string MatchedFieldKey,
  string Route,
  DateTimeOffset SortDate);

public interface IGlobalSearchRepository
{
  Task<IReadOnlyList<GlobalSearchResultRecord>> SearchAsync(
    OrganizationId organizationId,
    GlobalSearchQuery query,
    CancellationToken cancellationToken = default);
}
