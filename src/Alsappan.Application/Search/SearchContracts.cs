using Alsappan.Application.Common.Contracts;

namespace Alsappan.Application.Search;

public sealed record GlobalSearchRequestDto(
  string Query,
  int Limit = 5,
  string? Locale = null);

public sealed record GlobalSearchResponseDto(
  string Query,
  int TotalItems,
  IReadOnlyList<GlobalSearchGroupDto> Groups);

public sealed record GlobalSearchGroupDto(
  string EntityType,
  string EntityTypeLabel,
  string Route,
  IReadOnlyList<GlobalSearchResultDto> Results);

public sealed record GlobalSearchResultDto(
  string EntityType,
  string EntityTypeLabel,
  Guid Id,
  string Label,
  string? Summary,
  string MatchedField,
  string Route);

public sealed record GlobalSearchEntityOptionDto(
  string EntityType,
  string EntityTypeLabel,
  string ReadPermission);

public sealed record GlobalSearchResultContractDto(
  IReadOnlyList<GlobalSearchEntityOptionDto> EntityTypes,
  IReadOnlyList<SelectOptionDto> MatchedFields);
